﻿// Copyright (c) Aurora Studio. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Aurora.Music.Core.Models;
using Aurora.Shared.Extensions;
using Aurora.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TagLib;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Aurora.Music.Core.Storage
{
    public static class FileReader
    {
        public static async Task<IReadOnlyList<StorageFile>> ReadFilesAsync(IReadOnlyList<IStorageItem> p)
        {
            var list = new List<StorageFile>();
            foreach (var item in p)
            {
                if (item is IStorageFile file)
                {
                    foreach (var types in Consts.FileTypes)
                    {
                        if (types == file.FileType)
                        {
                            list.Add(file as StorageFile);
                            break;
                        }
                    }
                }
                else if (item is StorageFolder folder)
                {
                    var options = new Windows.Storage.Search.QueryOptions
                    {
                        FileTypeFilter = { ".flac", ".wav", ".m4a", ".aac", ".mp3", ".wma" },
                        FolderDepth = Windows.Storage.Search.FolderDepth.Deep,
                        IndexerOption = Windows.Storage.Search.IndexerOption.DoNotUseIndexer,
                    };
                    var query = folder.CreateFileQueryWithOptions(options);
                    list.AddRange(await query.GetFilesAsync());
                }
            }
            return list;
        }

        public static event EventHandler<ProgressReport> ProgressUpdated;
        public static event EventHandler Completed;

        public static async Task<List<Song>> GetAllSongAsync()
        {
            var opr = SQLOperator.Current();
            var songs = await opr.GetAllAsync<SONG>();
            return songs.ConvertAll(x => new Song(x));
        }

        public static async Task PlayStaticAdd(int id, int targetType, int addAmount)
        {
            var opr = SQLOperator.Current();
            if (targetType == 0)
            {
                await opr.SongCountAddAsync(id, addAmount);
            }
        }

        /// <summary>
        /// TODO: Only pick files which not in the database, and find deleted files to delete
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private static async Task<IList<StorageFile>> GetFilesAsync(StorageFolder folder)
        {
            // TODO: determine is ondrive on demand
            var files = new List<StorageFile>();
            files.AddRange(await new FileTracker(folder).SearchFolder());
            return files;
        }

        public static async Task<List<Album>> GetAlbumsAsync(string value)
        {
            var opr = SQLOperator.Current();

            // get single song
            // sqlite escaping
            value = SQLOperator.SQLEscaping(value);
            if (value.IsNullorEmpty())
            {
                // anonymous artists, get their songs
                var songs = await opr.GetWithQueryAsync<SONG>("ALBUMARTISTS", value);
                var albumGrouping = from song in songs group song by song.Album;
                return albumGrouping.ToList().ConvertAll(a => new Album(a));
            }

            // get aritst-associated albums
            var albums = await opr.GetWithQueryAsync<ALBUM>("ALBUMARTISTS", value);
            var res = albums.ConvertAll(a => new Album(a));

            var otherSongs = await opr.GetWithQueryAsync<SONG>($"SELECT * FROM SONG WHERE PERFORMERS='{value}' OR ALBUMARTISTS='{value}'");

            // remove duplicated (we suppose that artist's all song is just 1000+, this way can find all song and don't take long time)
            otherSongs.RemoveAll(x => !albums.Where(b => b.Name == x.Album).IsNullorEmpty());
            var otherGrouping = from song in otherSongs group song by song.Album;
            // otherSongs has item
            if (!otherGrouping.IsNullorEmpty())
            {
                res.AddRange(otherGrouping.ToList().ConvertAll(a => new Album(a)));
            }
            return res;
        }

        public static async Task<int> GetArtistsCountAsync()
        {
            var opr = SQLOperator.Current();
            var artists = await opr.GetArtistsAsync();
            return artists.Count;
        }

        public static async Task<List<Album>> GetAllAlbumsAsync()
        {
            var opr = SQLOperator.Current();

            // get aritst-associated albums
            var albums = await opr.GetAllAsync<ALBUM>();
            var res = albums.ConvertAll(a => new Album(a));

            var otherSongs = await opr.GetWithQueryAsync<SONG>($"SELECT * FROM SONG WHERE ALBUM IS NULL");

            // remove duplicated (we suppose that artist's all song is just 1000+, this way can find all song and don't take long time)
            var otherGrouping = from song in otherSongs group song by song.Album;
            // otherSongs has item
            if (!otherGrouping.IsNullorEmpty())
            {
                res.AddRange(otherGrouping.ToList().ConvertAll(a => new Album(a)));
            }
            return res;
        }

        public static async Task<List<GenericMusicItem>> GetFavListAsync()
        {
            return await SQLOperator.Current().GetFavListAsync();
        }

        public static async Task<List<GenericMusicItem>> GetRandomListAsync()
        {
            var opr = SQLOperator.Current();
            var p = Shared.Helpers.Tools.Random.Next(15);
            var songs = await opr.GetRandomListAsync<SONG>(25 - p);
            var albums = await opr.GetRandomListAsync<ALBUM>(p);
            var list = songs.ConvertAll(x => new GenericMusicItem(x));

            foreach (var album in albums)
            {
                var albumSongs = Array.ConvertAll(album.Songs.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries), (a) =>
                {
                    return int.Parse(a);
                });

                list.Add(new GenericMusicItem(album));
            }

            list.Shuffle();
            return list;
        }

        public static async Task<IEnumerable<ListWithKey<GenericMusicItem>>> GetHeroListAsync()
        {
            var opr = SQLOperator.Current();
            var todaySuggestion = await opr.GetTodayListAsync();
            var nowSuggestion = await opr.GetNowListAsync();
            var recent = await opr.GetRecentListAsync();

            var res = new List<ListWithKey<GenericMusicItem>>
            {
                new ListWithKey<GenericMusicItem>(string.Format(Consts.Localizer.GetString("TodaySuggestionText"), DateTime.Today.DayOfWeek.GetDisplayName()), todaySuggestion),
                new ListWithKey<GenericMusicItem>(Consts.Localizer.GetString("RencentlyPlayedText"), recent),
                new ListWithKey<GenericMusicItem>(string.Format(Consts.Localizer.GetString("TodayFavText"), DateTime.Now.GetHourString()), nowSuggestion)
            };
            res.Shuffle();
            return res;
        }

        public async static Task<List<Album>> GetAlbumsAsync()
        {
            var opr = SQLOperator.Current();
            var albums = await opr.GetAllAsync<ALBUM>();
            return albums.ConvertAll(a => new Album(a));
        }


        public static List<StorageFolder> InitFolderList()
        {
            var list = new List<StorageFolder>();
            if (Settings.Current.IncludeMusicLibrary)
            {
                // TODO: music library don't have path
                list.Add(KnownFolders.MusicLibrary);
            }
            list.Add(AsyncHelper.RunSync(async () => await ApplicationData.Current.LocalFolder.CreateFolderAsync("Music", CreationCollisionOption.OpenIfExists)));
            return list;
        }

        public static async Task Read(IList<StorageFolder> folder)
        {
            var list = new List<StorageFile>();
            int i = 1;

            foreach (var item in folder)
            {
                var files = await GetFilesAsync(item);

                var opr = SQLOperator.Current();
                if (KnownFolders.MusicLibrary.Path == item.Path || item.Path.Contains(ApplicationData.Current.LocalFolder.Path))
                {

                }
                else
                {
                    await opr.UpdateFolderAsync(item, files.Count);
                }

                list.AddRange(files);

                ProgressUpdated?.Invoke(null, new ProgressReport() { Description = $"{i} of {folder.Count} folders scanned", Current = i, Total = folder.Count });
                i++;
            }
            await Task.Delay(200);
            ProgressUpdated?.Invoke(null, new ProgressReport() { Description = "Folder scanning completed", Current = i, Total = folder.Count });
            await Task.Delay(200);
            await ReadFileandSave(from a in list group a by a.Path into b select b.First());
        }

        public static async Task ReadFileandSave(IEnumerable<StorageFile> files)
        {
            var opr = SQLOperator.Current();
            var total = files.Count();
            int i = 1;

            var newlist = new List<SONG>();

            foreach (var file in files)
            {
                if (!file.IsAvailable || file.Attributes.HasFlag(FileAttributes.LocallyIncomplete))
                {
                    ProgressUpdated?.Invoke(null, new ProgressReport() { Description = $"{i} of {total} files readed", Current = i, Total = total });

                    i++;
                    continue;
                }

                try
                {
                    using (var tagTemp = File.Create(file.Path))
                    {
                        var song = await Song.Create(tagTemp.Tag, file.Path, await file.Properties.GetMusicPropertiesAsync());
                        var t = await opr.InsertSongAsync(song);
                        if (t != null)
                        {
                            newlist.Add(t);
                        }
                    }
                }
                catch (Exception e)
                {
                    Shared.Helpers.Tools.Logging(e);
                    continue;
                }
                finally
                {
                    ProgressUpdated?.Invoke(null, new ProgressReport() { Description = $"{i} of {total} files read", Current = i, Total = total });

                    i++;
                }
            }

            if (newlist.Count > 0)
            {
                await AddToAlbums(newlist);
            }
            else
            {
                Completed?.Invoke(null, EventArgs.Empty);
            }
        }


        public static async Task UpdateSongAsync(Song model)
        {
            var opr = SQLOperator.Current();
            await opr.UpdateSongAsync(model);
        }

        public static async Task<List<Song>> GetSongsAsync()
        {
            var opr = SQLOperator.Current();
            var songs = await opr.GetAllAsync<SONG>();
            return songs.ConvertAll(a => new Song(a));
        }

        public async static Task AddToAlbums(IEnumerable<Song> songs)
        {
            await Task.Run(async () =>
            {
                var albums = from song in songs group song by song.Album into album select album;
                var opr = SQLOperator.Current();
                var count = albums.Count();

                int i = 1;


                ProgressUpdated?.Invoke(null, new ProgressReport() { Description = $"0 of {count} albums sorted", Current = 0, Total = count });
                foreach (var item in albums)
                {
                    await opr.AddAlbumAsync(item);
                    ProgressUpdated?.Invoke(null, new ProgressReport() { Description = $"{i} of {count} albums sorted", Current = i, Total = count });

                    i++;
                }
                Completed?.Invoke(null, EventArgs.Empty);
            });
        }

        public static async Task<List<GenericMusicItem>> Search(string text)
        {
            var opr = SQLOperator.Current();
            text = SQLOperator.SQLEscaping(text);

            var songs = await opr.SearchAsync<SONG>(text, "TITLE", "PERFORMERS");

            var album = await opr.SearchAsync<ALBUM>(text, "NAME", "AlbumArtists");

            var l = new List<GenericMusicItem>(album.ConvertAll(x => new GenericMusicItem(x)));
            l.AddRange(songs.ConvertAll(x => new GenericMusicItem(x)));
            return l;
        }

        internal static async Task AddToAlbums(IEnumerable<SONG> songs)
        {
            await Task.Run(async () =>
            {
                var albums = from song in songs group song by song.Album into album select album;
                var opr = SQLOperator.Current();
                var count = albums.Count();

                if (count == 0)
                {
                    Completed?.Invoke(null, EventArgs.Empty);
                    return;
                }

                int i = 1;

                await Task.Delay(200);

                ProgressUpdated?.Invoke(null, new ProgressReport() { Description = $"0 of {count} albums sorted", Current = 0, Total = count });
                foreach (var item in albums)
                {
                    await opr.AddAlbumAsync(item);
                    ProgressUpdated?.Invoke(null, new ProgressReport() { Description = $"{i} of {count} albums sorted", Current = i, Total = count });
                    i++;
                }
                Completed?.Invoke(null, EventArgs.Empty);
            });
        }

        public static async Task<IList<Song>> ReadFileandSendBack(List<StorageFile> files)
        {
            List<Song> tempList = new List<Song>();
            var total = files.Count;
            foreach (var file in files)
            {
                using (var tagTemp = File.Create(file))
                {
                    tempList.Add(await Song.Create(tagTemp.Tag, file.Path, await file.Properties.GetMusicPropertiesAsync()));
                }
            }
            var result = from song in tempList orderby song.Track orderby song.Disc group song by song.Album;
            var list = new List<Song>();
            foreach (var item in result)
            {
                list.AddRange(item);
            }
            return list;
        }

        public static async Task<Song> ReadFileAsync(StorageFile file)
        {
            using (var tagTemp = File.Create(file))
            {
                return await Create(tagTemp.Tag, file.Path, await file.Properties.GetMusicPropertiesAsync());
            }
        }

        private static async Task<Song> Create(Tag tag, string path, MusicProperties music)
        {
            var song = new Song
            {
                Duration = music.Duration,
                BitRate = music.Bitrate,
                FilePath = path,
                Rating = (uint)Math.Round(music.Rating / 20.0),
                MusicBrainzArtistId = tag.MusicBrainzArtistId,
                MusicBrainzDiscId = tag.MusicBrainzDiscId,
                MusicBrainzReleaseArtistId = tag.MusicBrainzReleaseArtistId,
                MusicBrainzReleaseCountry = tag.MusicBrainzReleaseCountry,
                MusicBrainzReleaseId = tag.MusicBrainzReleaseId,
                MusicBrainzReleaseStatus = tag.MusicBrainzReleaseStatus,
                MusicBrainzReleaseType = tag.MusicBrainzReleaseType,
                MusicBrainzTrackId = tag.MusicBrainzTrackId,
                MusicIpId = tag.MusicIpId,
                BeatsPerMinute = tag.BeatsPerMinute,
                Album = tag.Album,
                AlbumArtists = tag.AlbumArtists,
                AlbumArtistsSort = tag.AlbumArtistsSort,
                AlbumSort = tag.AlbumSort,
                AmazonId = tag.AmazonId,
                Title = tag.Title,
                TitleSort = tag.TitleSort,
                Track = tag.Track,
                TrackCount = tag.TrackCount,
                ReplayGainTrackGain = tag.ReplayGainTrackGain,
                ReplayGainTrackPeak = tag.ReplayGainTrackPeak,
                ReplayGainAlbumGain = tag.ReplayGainAlbumGain,
                ReplayGainAlbumPeak = tag.ReplayGainAlbumPeak,
                Comment = tag.Comment,
                Disc = tag.Disc,
                Composers = tag.Composers,
                ComposersSort = tag.ComposersSort,
                Conductor = tag.Conductor,
                DiscCount = tag.DiscCount,
                Copyright = tag.Copyright,
                Genres = tag.Genres,
                Grouping = tag.Grouping,
                Lyrics = tag.Lyrics,
                Performers = tag.Performers,
                PerformersSort = tag.PerformersSort,
                Year = tag.Year
            };

            var pictures = tag.Pictures;
            if (!pictures.IsNullorEmpty())
            {
                var fileName = $"{CreateHash64(song.Title).ToString()}.{pictures[0].MimeType.Split('/').LastOrDefault().Replace("jpeg", "jpg")}";
                var s = await ApplicationData.Current.TemporaryFolder.TryGetItemAsync(fileName);
                if (s == null)
                {
                    StorageFile cacheImg = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(cacheImg, pictures[0].Data.Data);
                    song.PicturePath = cacheImg.Path;
                }
                else
                {
                    song.PicturePath = s.Path;
                }
            }
            else
            {
                song.PicturePath = null;
            }
            return song;
        }
        private static ulong CreateHash64(string str)
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(str);

            ulong value = (ulong)utf8.Length;
            for (int n = 0; n < utf8.Length; n++)
            {
                value += (ulong)utf8[n] << ((n * 5) % 56);
            }
            return value;
        }
    }

    public class ProgressReport
    {
        public string Description { get; set; }

        public int Current { get; set; }

        public int Total { get; set; }
    }
}
