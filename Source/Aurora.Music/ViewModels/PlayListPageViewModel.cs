﻿// Copyright (c) Aurora Studio. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Aurora.Music.Core;
using Aurora.Music.Core.Models;
using Aurora.Music.Core.Storage;
using Aurora.Music.Pages;
using Aurora.Shared.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

namespace Aurora.Music.ViewModels
{
    sealed class PlayListPageViewModel : ViewModelBase
    {

        private ObservableCollection<GroupedItem<SongViewModel>> songsList;
        public ObservableCollection<GroupedItem<SongViewModel>> SongsList
        {
            get { return songsList; }
            set { SetProperty(ref songsList, value); }
        }

        private List<Uri> heroImage;
        public List<Uri> HeroImage
        {
            get { return heroImage; }
            set { SetProperty(ref heroImage, value); }
        }

        private string desc;
        public string Description
        {
            get { return desc; }
            set { SetProperty(ref desc, value); }
        }

        private string songsCount;
        public string SongsCount
        {
            get { return songsCount; }
            set { SetProperty(ref songsCount, value); }
        }

        private string title;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public PlayListPageViewModel()
        {
            SongsList = new ObservableCollection<GroupedItem<SongViewModel>>();
        }

        public DelegateCommand PlayAll
        {
            get
            {
                return new DelegateCommand(async () =>
                {
                    await MainPageViewModel.Current.InstantPlay(await SQLOperator.Current().GetSongsAsync(Model.SongsID));
                });
            }
        }

        public DelegateCommand Delete
        {
            get
            {
                return new DelegateCommand(async () =>
                {
                    await SQLOperator.Current().RemovePlayListAsync(Model.ID);
                    LibraryPage.Current.RemovePlayList(Model);
                });
            }
        }

        public PlayList Model { get; private set; }

        public async Task GetSongsAsync(PlayList model)
        {
            Model = await SQLOperator.Current().GetPlayListAsync(model.ID);
            if (Model == null)
            {
                return;
            }

            var songs = await SQLOperator.Current().GetSongsAsync(Model.SongsID);

            var grouped = GroupedItem<SongViewModel>.CreateGroupsByAlpha(songs.ConvertAll(x => new SongViewModel(x)));

            await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                SongsList.Clear();
                foreach (var item in grouped)
                {
                    item.Aggregate((x, y) =>
                    {
                        y.Index = x.Index + 1;
                        return y;
                    });
                    SongsList.Add(item);
                }
                SongsCount = SmartFormat.Smart.Format(Consts.Localizer.GetString("SmartSongs"), songs.Count);
                Description = Model.Description;
                Title = Model.Title;
                HeroImage = Array.ConvertAll(Model.HeroArtworks ?? new string[] { }, x => new Uri(x)).ToList();
                foreach (var item in SongsList)
                {
                    foreach (var song in item)
                    {
                        song.RefreshFav();
                    }
                }
            });
        }

        internal async Task DeleteSong(SongViewModel songViewModel)
        {
            Model.SongsID = Model.SongsID.Where(a => a != songViewModel.ID).ToArray();
            await Model.SaveAsync();
        }

        internal async void ChangeSort(int selectedIndex)
        {
            SongsList.Clear();
            var songs = await SQLOperator.Current().GetSongsAsync(Model.SongsID);
            IEnumerable<GroupedItem<SongViewModel>> grouped;

            switch (selectedIndex)
            {
                case 0:
                    grouped = GroupedItem<SongViewModel>.CreateGroupsByAlpha(songs.ConvertAll(x => new SongViewModel(x)));
                    break;
                case 1:
                    grouped = GroupedItem<SongViewModel>.CreateGroups(songs.ConvertAll(x => new SongViewModel(x)), x => x.FormattedAlbum);
                    break;
                case 2:
                    grouped = GroupedItem<SongViewModel>.CreateGroups(songs.ConvertAll(x => new SongViewModel(x)), x => x.GetFormattedArtists());
                    break;
                default:
                    grouped = GroupedItem<SongViewModel>.CreateGroups(songs.ConvertAll(x => new SongViewModel(x)), x => x.Song.Year, true);
                    break;
            }
            foreach (var item in grouped)
            {
                item.Aggregate((x, y) =>
                {
                    y.Index = x.Index + 1;
                    return y;
                });
                SongsList.Add(item);
            }
            foreach (var item in SongsList)
            {
                foreach (var song in item)
                {
                    song.RefreshFav();
                }
            }
        }

        internal async Task PlayAt(SongViewModel songViewModel)
        {
            var list = await SQLOperator.Current().GetSongsAsync(Model.SongsID);
            await MainPageViewModel.Current.InstantPlay(list, list.FindIndex(x => x.ID == songViewModel.ID));
        }

        internal async Task EditDescription(string text)
        {
            Model.Description = text;
            await Model.SaveAsync();
            Description = text;
        }
    }
}
