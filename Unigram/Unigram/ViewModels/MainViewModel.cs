﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Api.Aggregator;
using Telegram.Api.Helpers;
using Telegram.Api.Services;
using Telegram.Api.Services.Cache;
using Telegram.Api.TL;
using Telegram.Api.TL.Phone.Methods;
using Telegram.Api.TL.Contacts.Methods;
using Unigram.Collections;
using Unigram.Common;
using Unigram.Converters;
using Unigram.Core.Notifications;
using Unigram.Core.Services;
using Unigram.Views;
using Windows.ApplicationModel.Background;
using Windows.Globalization.DateTimeFormatting;
using Windows.Networking.PushNotifications;
using Windows.Security.Authentication.Web;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Unigram.Controls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math;
using Unigram.Core;
using Unigram.Common.Dialogs;
using Telegram.Api.TL.Phone;

namespace Unigram.ViewModels
{
    public class MainViewModel : UnigramViewModelBase, IHandle<TLUpdatePhoneCall>, IHandle<TLUpdateUserTyping>, IHandle<TLUpdateChatUserTyping>
    {
        private readonly IPushService _pushService;

        private readonly Dictionary<int, InputTypingManager> _typingManagers;
        private readonly Dictionary<int, InputTypingManager> _chatTypingManagers;

        public MainViewModel(IMTProtoService protoService, ICacheService cacheService, ITelegramEventAggregator aggregator, IPushService pushService, IContactsService contactsService, DialogsViewModel dialogs)
            : base(protoService, cacheService, aggregator)
        {
            _pushService = pushService;
            _typingManagers = new Dictionary<int, InputTypingManager>();
            _chatTypingManagers = new Dictionary<int, InputTypingManager>();

            //Dialogs = new DialogCollection(protoService, cacheService);
            SearchDialogs = new ObservableCollection<TLDialog>();
            Dialogs = dialogs;
            Contacts = new ContactsViewModel(protoService, cacheService, aggregator, contactsService);
            Calls = new CallsViewModel(protoService, cacheService, aggregator);

            aggregator.Subscribe(this);
        }

        #region Typing

        public void Handle(TLUpdateUserTyping update)
        {
            var user = CacheService.GetUser(update.UserId) as TLUser;
            if (user != null && !user.IsSelf)
            {
                _typingManagers.TryGetValue(update.UserId, out InputTypingManager typingManager);
                if (typingManager == null)
                {
                    typingManager = new InputTypingManager(users =>
                    {
                        user.TypingSubtitle = DialogViewModel.GetTypingString(user.ToPeer(), users, CacheService.GetUser, null);
                        user.IsTyping = true;
                    },
                    () =>
                    {
                        user.TypingSubtitle = null;
                        user.IsTyping = false;
                    });

                    _typingManagers[update.UserId] = typingManager;
                }

                var action = update.Action;
                if (action is TLSendMessageCancelAction)
                {
                    typingManager.RemoveTypingUser(update.UserId);
                    return;
                }

                typingManager.AddTypingUser(update.UserId, action);
            }
        }

        public void Handle(TLUpdateChatUserTyping update)
        {
            var chatBase = CacheService.GetChat(update.ChatId) as TLChatBase;
            if (chatBase != null)
            {
                _typingManagers.TryGetValue(update.ChatId, out InputTypingManager typingManager);
                if (typingManager == null)
                {
                    typingManager = new InputTypingManager(users =>
                    {
                        chatBase.TypingSubtitle = DialogViewModel.GetTypingString(chatBase.ToPeer(), users, CacheService.GetUser, null);
                        chatBase.IsTyping = true;
                    },
                    () =>
                    {
                        chatBase.TypingSubtitle = null;
                        chatBase.IsTyping = false;
                    });

                    _typingManagers[update.ChatId] = typingManager;
                }

                var action = update.Action;
                if (action is TLSendMessageCancelAction)
                {
                    typingManager.RemoveTypingUser(update.UserId);
                    return;
                }

                typingManager.AddTypingUser(update.UserId, action);
            }
        }

        #endregion

        public TLVector<TLTopPeerCategoryPeers> TopPeers { get; private set; }

        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Task.Run(() => _pushService.RegisterAsync());

            Execute.BeginOnUIThread(() => Calls.OnNavigatedToAsync(parameter, mode, state));
            //Execute.BeginOnUIThread(() => Dialogs.LoadFirstSlice());
            //Execute.BeginOnUIThread(() => Contacts.getTLContacts());
            //Execute.BeginOnUIThread(() => Contacts.GetSelfAsync());

            //ProtoService.GetTopPeersAsync(TLContactsGetTopPeers.Flag.BotsInline, 0, 0, 0, result =>
            //{
            //    var topPeers = result as TLContactsTopPeers;
            //    if (topPeers != null)
            //    {
            //        TopPeers = topPeers.Categories;
            //    }
            //});

            return Task.CompletedTask;
        }

        private byte[] secretP;
        private byte[] a_or_b;

        public async void Handle(TLUpdatePhoneCall update)
        {
            await VoIPConnection.Current.SendUpdateAsync(update);
            await Task.Delay(2000);

            //if (update.PhoneCall is TLPhoneCallDiscarded discarded)
            //{
            //    if (discarded.IsNeedRating)
            //    {
            //        Debugger.Break();
            //    }

            //    if (discarded.IsNeedDebug)
            //    {
            //        Debugger.Break();
            //    }
            //}

            return;

            if (update.PhoneCall is TLPhoneCallRequested callRequested)
            {
                var reqReceived = new TLPhoneReceivedCall();
                reqReceived.Peer = new TLInputPhoneCall();
                reqReceived.Peer.Id = callRequested.Id;
                reqReceived.Peer.AccessHash = callRequested.AccessHash;

                ProtoService.SendRequestAsync<bool>("phone.receivedCall", reqReceived, null, null);

                var user = CacheService.GetUser(callRequested.AdminId) as TLUser;

                Execute.BeginOnUIThread(async () =>
                {
                    var dialog = await TLMessageDialog.ShowAsync(user.DisplayName, "CAAAALLL", "OK", "Cancel");
                    if (dialog == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                    {
                        var config = await ProtoService.GetDHConfigAsync(0, 256);
                        if (config.IsSucceeded)
                        {
                            var dh = config.Result;
                            if (!TLUtils.CheckPrime(dh.P, dh.G))
                            {
                                return;
                            }

                            secretP = dh.P;

                            var salt = new byte[256];
                            var secureRandom = new SecureRandom();
                            secureRandom.NextBytes(salt);

                            a_or_b = salt;

                            var g_b = MTProtoService.GetGB(salt, dh.G, dh.P);

                            var request = new TLPhoneAcceptCall
                            {
                                GB = g_b,
                                Peer = new TLInputPhoneCall
                                {
                                    Id = callRequested.Id,
                                    AccessHash = callRequested.AccessHash
                                },
                                Protocol = new TLPhoneCallProtocol
                                {
                                    IsUdpP2p = true,
                                    IsUdpReflector = true,
                                    MinLayer = 65,
                                    MaxLayer = 65,
                                }
                            };

                            var response = await ProtoService.SendRequestAsync<TLPhonePhoneCall>("phone.acceptCall", request);
                            if (response.IsSucceeded)
                            {
                            }
                        }
                    }
                    else
                    {
                        var req = new TLPhoneDiscardCall();
                        req.Peer = new TLInputPhoneCall();
                        req.Peer.Id = callRequested.Id;
                        req.Peer.AccessHash = callRequested.AccessHash;
                        req.Reason = new TLPhoneCallDiscardReasonHangup();

                        ProtoService.SendRequestAsync<TLPhonePhoneCall>("phone.acceptCall", req, null, null);
                    }
                });
            }
            else if (update.PhoneCall is TLPhoneCall call)
            {
                var auth_key = computeAuthKey(call);
                var g_a = call.GAOrB;

                var buffer = TLUtils.Combine(auth_key, g_a);
                var sha256 = Utils.ComputeSHA256(buffer);

                var emoji = EncryptionKeyEmojifier.EmojifyForCall(sha256);

                var user = CacheService.GetUser(call.AdminId) as TLUser;

                Execute.BeginOnUIThread(async () =>
                {
                    var dialog = await TLMessageDialog.ShowAsync(user.DisplayName, string.Join(" ", emoji), "OK");
                });
            }
        }

        private byte[] computeAuthKey(TLPhoneCall call)
        {
            BigInteger g_a = new BigInteger(1, call.GAOrB);
            BigInteger p = new BigInteger(1, secretP);

            g_a = g_a.ModPow(new BigInteger(1, a_or_b), p);

            byte[] authKey = g_a.ToByteArray();
            if (authKey.Length > 256)
            {
                byte[] correctedAuth = new byte[256];
                Buffer.BlockCopy(authKey, authKey.Length - 256, correctedAuth, 0, 256);
                authKey = correctedAuth;
            }
            else if (authKey.Length < 256)
            {
                byte[] correctedAuth = new byte[256];
                Buffer.BlockCopy(authKey, 0, correctedAuth, 256 - authKey.Length, authKey.Length);
                for (int a = 0; a < 256 - authKey.Length; a++)
                {
                    authKey[a] = 0;
                }
                authKey = correctedAuth;
            }
            byte[] authKeyHash = Utils.ComputeSHA1(authKey);
            byte[] authKeyId = new byte[8];
            Buffer.BlockCopy(authKeyHash, authKeyHash.Length - 8, authKeyId, 0, 8);

            return authKey;
        }

        //END OF EXPERIMENTS
        //public DialogCollection Dialogs { get; private set; }

        public ObservableCollection<TLDialog> SearchDialogs { get; private set; }

        public DialogsViewModel Dialogs { get; private set; }

        public ContactsViewModel Contacts { get; private set; }

        public CallsViewModel Calls { get; private set; }
    }
}
