﻿using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kazyx.Uwpmm.Utility
{
    public class CommandBarManager
    {
        public CommandBarManager()
        {
            EnabledItems.Add(AppBarItemType.WithIcon, new SortedSet<AppBarItem>());
            EnabledItems.Add(AppBarItemType.OnlyText, new SortedSet<AppBarItem>());
        }

        public CommandBar bar = new CommandBar();

        private static AppBarButton NewButton(AppBarItem item)
        {
            switch (item)
            {
                case AppBarItem.ControlPanel:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_cameraSetting.png", UriKind.Absolute) }, Label = SystemUtil.GetStringResource("AppBar_ControlPanel") };
                case AppBarItem.AppSetting:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/feature.settings.png", UriKind.Absolute) }, Label = SystemUtil.GetStringResource("AppBar_AppSetting") };
                case AppBarItem.CancelTouchAF:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_cancel.png", UriKind.Absolute) }, Label = SystemUtil.GetStringResource("AppBar_CancelTouchAf") };
                case AppBarItem.AboutPage:
                    return new AppBarButton() { Label = SystemUtil.GetStringResource("About") };
                case AppBarItem.LoggerPage:
                    return new AppBarButton() { Label = "Logger" };
                case AppBarItem.PlaybackPage:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_playback.png", UriKind.Absolute) }, Label = SystemUtil.GetStringResource("AppBar_CameraRoll") };
                case AppBarItem.DeleteMultiple:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_delete.png") }, Label = SystemUtil.GetStringResource("AppBar_Delete") };
                case AppBarItem.DownloadMultiple:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_download.png") }, Label = SystemUtil.GetStringResource("AppBar_Download") };
                case AppBarItem.ShowDetailInfo:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_display_info.png") }, Label = SystemUtil.GetStringResource("ShowDetailInfo") };
                case AppBarItem.HideDetailInfo:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_close_display.png") }, Label = SystemUtil.GetStringResource("HideDetailInfo") };
                case AppBarItem.Ok:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_ok.png") }, Label = SystemUtil.GetStringResource("AppBar_Exit") };
                case AppBarItem.WifiSetting:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_wifi.png") }, Label = SystemUtil.GetStringResource("WifiSettingLauncherButtonText") };
                case AppBarItem.Donation:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_Dollar.png") }, Label = SystemUtil.GetStringResource("Donation") };
                case AppBarItem.RotateLeft:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_rotateLeft.png") }, Label = SystemUtil.GetStringResource("RotateLeft") };
                case AppBarItem.RotateRight:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/appbar_rotateRight.png") }, Label = SystemUtil.GetStringResource("RotateRight") };
                case AppBarItem.Refresh:
                    return new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/AppBar/sync.png") }, Label = SystemUtil.GetStringResource("AppBar_Refresh") };
                default:
                    throw new NotImplementedException();
            }
        }

        private readonly Dictionary<AppBarItemType, SortedSet<AppBarItem>> EnabledItems = new Dictionary<AppBarItemType, SortedSet<AppBarItem>>();

        private readonly Dictionary<AppBarItem, RoutedEventHandler> EventHolder = new Dictionary<AppBarItem, RoutedEventHandler>();

        public CommandBarManager SetEvent(AppBarItem item, RoutedEventHandler handler)
        {
            EventHolder.Add(item, handler);
            return this;
        }

        public CommandBarManager ClearEvents()
        {
            EventHolder.Clear();
            return this;
        }

        public CommandBarManager Clear()
        {
            foreach (var items in EnabledItems)
            {
                items.Value.Clear();
            }
            return this;
        }

        public CommandBarManager Icon(AppBarItem item)
        {
            return Enable(AppBarItemType.WithIcon, item);
        }

        public CommandBarManager NoIcon(AppBarItem item)
        {
            return Enable(AppBarItemType.OnlyText, item);
        }

        private CommandBarManager Enable(AppBarItemType type, AppBarItem item)
        {
            if (!EnabledItems[type].Contains(item))
            {
                EnabledItems[type].Add(item);
            }
            return this;
        }

        public CommandBarManager Disable(AppBarItemType type, AppBarItem item)
        {
            if (EnabledItems[type].Contains(item))
            {
                EnabledItems[type].Remove(item);
            }
            return this;
        }

        public CommandBar CreateNew(double opacity)
        {
            bar = new CommandBar()
            {
                Background = Application.Current.Resources["AppBarBackgroundThemeBrush"] as Brush,
                Opacity = opacity,
            };
            foreach (AppBarItem item in EnabledItems[AppBarItemType.WithIcon])
            {
                var button = NewButton(item);
                if (EventHolder.ContainsKey(item))
                {
                    button.Click += EventHolder[item];
                }
                bar.PrimaryCommands.Add(button);
            }
            foreach (AppBarItem item in EnabledItems[AppBarItemType.OnlyText])
            {
                var button = NewButton(item);
                if (EventHolder.ContainsKey(item))
                {
                    button.Click += EventHolder[item];
                }
                bar.SecondaryCommands.Add(button);
            }
            return bar;
        }

        public bool IsEnabled(AppBarItemType type, AppBarItem item)
        {
            return EnabledItems[type].Contains(item);
        }
    }

    public enum AppBarItemType
    {
        WithIcon,
        OnlyText,
    }

    public enum AppBarItem
    {
        PlaybackPage,
        AboutPage,
        LoggerPage,
        CancelTouchAF,
        DownloadMultiple,
        DeleteMultiple,
        Ok,
        WifiSetting,
        Donation,
        RotateRight,
        ShowDetailInfo,
        HideDetailInfo,
        RotateLeft,
        Refresh,
        AppSetting,
        ControlPanel,
    }
}
