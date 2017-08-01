﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Telegram.Api.TL;
using Unigram.ViewModels.Channels;
using Unigram.Views.Users;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unigram.Views.Channels
{
    public sealed partial class ChannelAdminsPage : Page
    {
        public ChannelAdminsViewModel ViewModel => DataContext as ChannelAdminsViewModel;

        public ChannelAdminsPage()
        {
            InitializeComponent();
            DataContext = UnigramContainer.Current.ResolveType<ChannelAdminsViewModel>();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TLChannelParticipantAdmin participant && participant.User != null)
            {
                ViewModel.NavigationService.Navigate(typeof(ChannelAdminRightsPage), TLTuple.Create(ViewModel.Item.ToPeer(), participant));
            }
        }
    }
}
