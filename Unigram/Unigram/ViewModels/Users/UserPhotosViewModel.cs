﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Api.Aggregator;
using Telegram.Api.Helpers;
using Telegram.Api.Services;
using Telegram.Api.Services.Cache;
using Telegram.Api.Services.FileManager;
using Telegram.Api.Services.FileManager.EventArgs;
using Telegram.Api.TL;
using Telegram.Api.TL.Photos;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Converters;
using Unigram.Core.Common;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unigram.ViewModels.Users
{
    public class UserPhotosViewModel : GalleryViewModelBase
    {
        private readonly DisposableMutex _loadMoreLock = new DisposableMutex();

        public UserPhotosViewModel(TLUser user, IMTProtoService protoService)
            : base(protoService, null, null)
        {
            Items = new MvxObservableCollection<GalleryItem>();
            Initialize(user);
        }

        private async void Initialize(TLUser user)
        {
            User = user;

            var full = InMemoryCacheService.Current.GetFullUser(user.Id);
            if (full == null)
            {
                var response = await ProtoService.GetFullUserAsync(user.ToInputUser());
                if (response.IsSucceeded)
                {
                    full = response.Result;
                }
            }

            if (full != null)
            {
                SelectedItem = new GalleryPhotoItem(full.ProfilePhoto as TLPhoto, user);
                FirstItem = SelectedItem;

                return;
            }

            using (await _loadMoreLock.WaitAsync())
            {
                var response = await ProtoService.GetUserPhotosAsync(User.ToInputUser(), 0, 0, 0);
                if (response.IsSucceeded)
                {
                    if (response.Result is TLPhotosPhotosSlice slice)
                    {
                        TotalItems = slice.Count;
                    }
                    else
                    {
                        TotalItems = response.Result.Photos.Count;
                    }

                    Items.ReplaceWith(response.Result.Photos.OfType<TLPhoto>().Select(x => new GalleryPhotoItem(x, user)));

                    SelectedItem = Items.FirstOrDefault();
                    FirstItem = Items.FirstOrDefault();
                }
            }
        }

        protected override async void LoadNext()
        {
            if (User != null && TotalItems > Items.Count)
            {
                using (await _loadMoreLock.WaitAsync())
                {
                    var response = await ProtoService.GetUserPhotosAsync(User.ToInputUser(), Items.Count, 0, 0);
                    if (response.IsSucceeded)
                    {
                        Items.AddRange(response.Result.Photos.OfType<TLPhoto>().Select(x => new GalleryPhotoItem(x, _user)));
                    }
                }
            }
        }

        public override bool CanDelete => _user != null && _user.IsSelf;

        protected override async void DeleteExecute()
        {
            var confirm = await TLMessageDialog.ShowAsync("Do you want to delete this photo?", "Delete", "OK", "Cancel");
            if (confirm == ContentDialogResult.Primary && _selectedItem is GalleryPhotoItem item)
            {
                //var response = await ProtoService.UpdateProfilePhotoAsync(new TLInputPhotoEmpty());
                var response = await ProtoService.DeletePhotosAsync(new TLVector<TLInputPhotoBase> { new TLInputPhoto { Id = item.Photo.Id, AccessHash = item.Photo.AccessHash } });
                if (response.IsSucceeded)
                {
                    var index = Items.IndexOf(item);
                    if (index < Items.Count - 1)
                    {
                        Items.Remove(item);
                        SelectedItem = Items[index - 1];
                        TotalItems--;
                    }
                    else
                    {
                        NavigationService.GoBack();
                    }
                }
            }
        }

        private TLUser _user;
        public TLUser User
        {
            get
            {
                return _user;
            }
            set
            {
                Set(ref _user, value);
            }
        }
    }

    public class GalleryPhotoItem : GalleryItem
    {
        private readonly TLPhoto _photo;
        private readonly ITLDialogWith _from;
        private readonly string _caption;

        public GalleryPhotoItem(TLPhoto photo, ITLDialogWith from)
        {
            _photo = photo;
            _from = from;
        }

        public GalleryPhotoItem(TLPhoto photo, string caption)
        {
            _photo = photo;
            _caption = caption;
        }

        public TLPhoto Photo => _photo;

        public override ITLTransferable Source => _photo;

        public override string Caption => _caption;

        public override ITLDialogWith From => _from;

        public override int Date => _photo.Date;

        public override bool HasStickers => _photo.IsHasStickers;

        public override TLInputStickeredMediaBase ToInputStickeredMedia()
        {
            return new TLInputStickeredMediaPhoto { Id = _photo.ToInputPhoto() };
        }
    }

    public class GalleryDocumentItem : GalleryItem
    {
        private readonly TLDocument _document;
        private readonly ITLDialogWith _from;
        private readonly string _caption;

        public GalleryDocumentItem(TLDocument document, ITLDialogWith from)
        {
            _document = document;
            _from = from;
        }

        public GalleryDocumentItem(TLDocument document, string caption)
        {
            _document = document;
            _caption = caption;
        }

        public TLDocument Document => _document;

        public override ITLTransferable Source => _document;

        public override string Caption => _caption;

        public override ITLDialogWith From => _from;

        public override int Date => _document.Date;

        public override bool IsVideo => TLMessage.IsVideo(_document) || TLMessage.IsGif(_document) || TLMessage.IsRoundVideo(_document);

        public override bool IsLoop => TLMessage.IsGif(_document);

        public override bool HasStickers => _document.Attributes.Any(x => x is TLDocumentAttributeHasStickers);

        public override TLInputStickeredMediaBase ToInputStickeredMedia()
        {
            return new TLInputStickeredMediaDocument { Id = _document.ToInputDocument() };
        }

        public override Uri GetVideoSource()
        {
            return FileUtils.GetTempFileUri(_document.GetFileName());
        }
    }
}
