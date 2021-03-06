﻿// Copyright (c) Aurora Studio. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Aurora.Music.Controls;
using Aurora.Music.Core;
using Aurora.Music.Core.Models;
using Aurora.Music.ViewModels;
using Aurora.Shared.Extensions;
using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Aurora.Music.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class PlayListPage : Page, IRequestGoBack
    {
        public PlayListPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        public void RequestGoBack()
        {
            try
            {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(Consts.ArtistPageInAnimation + "_1", Title);
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(Consts.ArtistPageInAnimation + "_2", Details);
            }
            catch (Exception)
            {
            }
            LibraryPage.Current.GoBack();
            UnloadObject(this);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
            AppViewBackButtonVisibility.Visible;

            if (e.Parameter == null)
            {
                return;
            }
            await Context.GetSongsAsync(e.Parameter as PlayList);

            if ((e.Parameter as PlayList).Title == Consts.Localizer.GetString("Favorites"))
            {
                DescriptionBtn.Visibility = Visibility.Collapsed;
                DeleteBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                DescriptionBtn.Visibility = Visibility.Visible;
                DeleteBtn.Visibility = Visibility.Visible;
            }

            SortBox.SelectionChanged -= ComboBox_SelectionChanged;
            SortBox.SelectionChanged += ComboBox_SelectionChanged;
        }

        private async void AlbumList_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Context.PlayAt(e.ClickedItem as SongViewModel);
        }

        private async void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            await Context.PlayAt(sender as SongViewModel);
        }
        private async void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DescriptionBtn.Visibility == Visibility.Collapsed)
            {
                (sender as SongViewModel).Favorite = false;
            }
            else
            {
                await Context.DeleteSong(sender as SongViewModel);
            }
            var groups = Context.SongsList.Where(a => a.Contains((sender as SongViewModel)));
            foreach (var item in groups)
            {
                item.Remove((sender as SongViewModel));
                if (item.Count == 0)
                {
                    Context.SongsList.Remove(item);
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ComboBox;
            Context.ChangeSort(box.SelectedIndex);
        }

        private void AlbumList_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            // Walk up the tree to find the ListViewItem.
            // There may not be one if the click wasn't on an item.
            var requestedElement = (FrameworkElement)args.OriginalSource;
            while ((requestedElement != sender) && !(requestedElement is SelectorItem))
            {
                requestedElement = (FrameworkElement)VisualTreeHelper.GetParent(requestedElement);
            }
            var model = (sender as ListViewBase).ItemFromContainer(requestedElement) as SongViewModel;
            if (requestedElement != sender)
            {
                var albumMenu = MainPage.Current.SongFlyout.Items.First(x => x.Name == "AlbumMenu") as MenuFlyoutItem;
                albumMenu.Text = model.Album;
                albumMenu.Visibility = Visibility.Visible;

                // remove performers in flyout
                var index = MainPage.Current.SongFlyout.Items.IndexOf(albumMenu);
                while (!(MainPage.Current.SongFlyout.Items[index + 1] is MenuFlyoutSeparator))
                {
                    MainPage.Current.SongFlyout.Items.RemoveAt(index + 1);
                }
                // add song's performers to flyout
                if (!model.Song.Performers.IsNullorEmpty())
                {
                    if (model.Song.Performers.Length == 1)
                    {
                        var menuItem = new MenuFlyoutItem()
                        {
                            Text = $"{model.Song.Performers[0]}",
                            Icon = new FontIcon()
                            {
                                Glyph = "\uE136"
                            }
                        };
                        menuItem.Click += MainPage.Current.MenuFlyoutArtist_Click;
                        MainPage.Current.SongFlyout.Items.Insert(index + 1, menuItem);
                    }
                    else
                    {
                        var sub = new MenuFlyoutSubItem()
                        {
                            Text = $"{Consts.Localizer.GetString("PerformersText")}:",
                            Icon = new FontIcon()
                            {
                                Glyph = "\uE136"
                            }
                        };
                        foreach (var item in model.Song.Performers)
                        {
                            var menuItem = new MenuFlyoutItem()
                            {
                                Text = item
                            };
                            menuItem.Click += MainPage.Current.MenuFlyoutArtist_Click;
                            sub.Items.Add(menuItem);
                        }
                        MainPage.Current.SongFlyout.Items.Insert(index + 1, sub);
                    }
                }

                if (args.TryGetPosition(requestedElement, out var point))
                {
                    MainPage.Current.SongFlyout.ShowAt(requestedElement, point);
                }
                else
                {
                    MainPage.Current.SongFlyout.ShowAt(requestedElement);
                }

                args.Handled = true;
            }
        }


        private void AlbumList_ContextCanceled(UIElement sender, RoutedEventArgs args)
        {
            MainPage.Current.SongFlyout.Hide();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DescriptionEditor.Visibility == Visibility.Collapsed)
            {
                DescriptionSymbol.Glyph = "\uE001";
                DescriptionEditor.Visibility = Visibility.Visible;
                DescriptionText.Visibility = Visibility.Collapsed;
            }
            else
            {
                DescriptionSymbol.Glyph = "\uE104";
                DescriptionEditor.Visibility = Visibility.Collapsed;
                DescriptionText.Visibility = Visibility.Visible;
                await Context.EditDescription(DescriptionEditor.Text);
                DescriptionEditor.Text = string.Empty;
            }
        }

        private void SongItem_RequestMultiSelect(object sender, RoutedEventArgs e)
        {
            AlbumList.SelectionMode = ListViewSelectionMode.Multiple;
            AlbumList.IsItemClickEnabled = false;
            foreach (var item in Context.SongsList)
            {
                foreach (var song in item)
                {
                    song.ListMultiSelecting = true;
                }
            }
        }

        public Visibility SelectionModeToTitle(ListViewSelectionMode s)
        {
            if (s == ListViewSelectionMode.Multiple)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public Visibility SelectionModeToOther(ListViewSelectionMode s)
        {
            if (s != ListViewSelectionMode.Multiple)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AlbumList.SelectionMode = ListViewSelectionMode.Single;
            AlbumList.IsItemClickEnabled = true;
            foreach (var item in Context.SongsList)
            {
                foreach (var song in item)
                {
                    song.ListMultiSelecting = false;
                }
            }
        }

        private async void PlayAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await MainPageViewModel.Current.InstantPlay(AlbumList.SelectedItems.Select(a => (a as SongViewModel).Song).ToList());
        }

        private async void PlayNextAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await MainPageViewModel.Current.PlayNext(AlbumList.SelectedItems.Select(a => (a as SongViewModel).Song).ToList());
        }

        private async void AddCollectionAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var s = new AddPlayList(AlbumList.SelectedItems.Select(a => (a as SongViewModel).ID).ToList());
            await s.ShowAsync();
        }

        private void ShareAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var s = AlbumList.SelectedItems.Select(a => (a as SongViewModel)).ToList();
            MainPage.Current.Share(s);
        }
    }
}
