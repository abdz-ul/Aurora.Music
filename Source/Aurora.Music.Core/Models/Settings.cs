﻿// Copyright (c) Aurora Studio. All rights reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Aurora.Shared.Extensions;
using Aurora.Shared.Helpers;
using System;
using System.Collections.Generic;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;

namespace Aurora.Music.Core.Models
{

    [Flags]
    public enum Effects
    {
        None = 0, Reverb = 1, Limiter = 2, Equalizer = 4, ChannelShift = 8, All = Reverb | Limiter | Equalizer | ChannelShift
    }

    public enum Bitrate
    {
        _128, _192, _256, _320
    }

    public class Presets : Dictionary<string, float[]>
    {
        public static readonly Presets Instance = new Presets();

        public Presets()
        {
            //                             30-75-125-250
            //                                         500-1  2  4  8  16
            this["Flat"] = new float[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            this["Pop"] = new float[10] { 6, 4, 2, 0, 2, 3, 0, 0, 0, 0 };
            this["Rock"] = new float[10] { 4, 3, 2, 1, 0, -1, 0, 1, 2, 3 };
            this["Vocal"] = new float[10] { 0, 0, 0, 0, 2, 2, 1, 0, 0, 0 };
            this["Bass"] = new float[10] { 4, 3, 2, 1, 0, 0, 0, 0, 0, 0 };
            this["Air"] = new float[10] { 0, 0, 0, 0, 0, 0, 1, 2, 3, 4 };
        }
    }


    public class Settings
    {
        public bool IsMeteredNetwork()
        {
            var connectionCost = NetworkInformation.GetInternetConnectionProfile().GetConnectionCost();
            return !(connectionCost.NetworkCostType == NetworkCostType.Unknown || connectionCost.NetworkCostType == NetworkCostType.Unrestricted);
        }

        public static event EventHandler SettingsChanged;

        private static object lockable = new object();

        private const string SETTINGS_CONTAINER = "main";

        public bool IncludeMusicLibrary { get; set; } = true;
        public ElementTheme Theme { get; set; } = ElementTheme.Default;

        public bool WelcomeFinished { get; set; } = false;
        public string OutputDeviceID { get; set; }
        public double PlayerVolume { get; set; } = 100d;
        public Effects AudioGraphEffects { get; set; } = Effects.None;
        public string CategoryLastClicked { get; set; }

        public string LyricExtensionID { get; set; } = string.Empty;
        public string OnlineMusicExtensionID { get; set; } = string.Empty;
        public string MetaExtensionID { get; set; } = string.Empty;

        public Bitrate PreferredBitRate { get; set; } = Bitrate._256;
        public bool OnlinePurchase { get; set; } = false;
        public bool DebugModeEnabled { get; set; } = false;

        public string DownloadPathToken { get; set; } = string.Empty;
        public bool RememberFileActivatedAction { get; set; } = false;
        public bool CopyFileWhenActivated { get; set; } = false;

        public DateTime DoubanLogin { get; set; }
        public double DoubanExpireTime { get; set; }
        public string DoubanUserName { get; set; }
        public string DoubanUserID { get; set; }
        public string DoubanToken { get; set; }

        public ulong LastUpdateBuild { get; set; } = 0ul;

        private static Settings current;
        public static Settings Current
        {
            get
            {
                lock (lockable)
                {
                    if (current == null)
                    {
                        current = Load();
                        SettingsChanged += Settings_SettingsChanged;
                    }
                    return current;
                }
            }
        }

        public bool MetaDataEnabled { get; set; } = true;
        public bool DataPlayEnabled { get; set; } = true;
        public bool DataDownloadEnabled { get; set; } = true;
        public bool IsPodcastToast { get; set; } = true;
        public uint FetchInterval { get; set; } = 30;
        public bool ShowPodcastsWhenSearch { get; set; } = true;
        public bool PreventLockscreen { get; set; } = false;

        public float[] Gain { get; set; } = new float[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public uint PreferredSearchCount { get; set; } = 10;

        public float ChannelShift { get; set; } = 0f;
        public bool StereoToMono { get; set; } = false;

        public bool VerifyDoubanLogin()
        {
            var b = DoubanUserID.IsNullorEmpty() || DoubanToken.IsNullorEmpty() || DoubanLogin.AddSeconds(DoubanExpireTime) < DateTime.Now.AddDays(1);
            return !b;
        }

        private static void Settings_SettingsChanged(object sender, EventArgs e)
        {
            lock (lockable)
            {
                current = Load();
            }
        }

        private static Settings Load()
        {
            try
            {
                if (LocalSettingsHelper.GetContainer(SETTINGS_CONTAINER).ReadGroupSettings(out Settings s))
                {
                    return s;
                }
                else
                {
                    return new Settings();
                }
            }
            catch (Exception)
            {
                return new Settings();
            }
        }

        public void Save()
        {
            lock (lockable)
            {
                LocalSettingsHelper.GetContainer(SETTINGS_CONTAINER).WriteGroupSettings(this);
                SettingsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public uint GetPreferredBitRate()
        {
            switch (PreferredBitRate)
            {
                case Bitrate._128:
                    return 128u;
                case Bitrate._192:
                    return 192u;
                case Bitrate._256:
                    return 256u;
                case Bitrate._320:
                    return 320u;
                default:
                    return 256u;
            }
        }

        public void DANGER_DELETE()
        {
            var s = new Settings();
            s.Save();
        }
    }
}
