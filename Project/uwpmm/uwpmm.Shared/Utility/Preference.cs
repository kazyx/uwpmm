﻿using Kazyx.Uwpmm.Control;
using Kazyx.Uwpmm.Playback;
using System;
using Windows.Storage;

namespace Kazyx.Uwpmm.Utility
{
    public class Preference
    {
        private Preference() { }

        private const string sync_postview = "sync_postview";
        private const string interval_time = "interval_time";
        private const string display_take_image_button = "display_take_image_button";
        private const string display_histogram = "display_histogram";
        private const string add_geotag = "add_geotag";
        private const string fraiming_grids = "fraiming_grids";
        private const string framing_grids_color = "framing_grids_color";
        private const string fibonacci_origin = "fibonacci_origin";
        private const string request_focus_frame_info = "request_focus_frame_info";
        private const string prioritize_original_contents = "prioritize_original_contents";
        private const string remote_contents_set = "remote_contents_set";

        private const string init_launched_datetime = "init_datetime";

        public static T GetProperty<T>(string key, T defaultValue)
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(key))
            {
                settings.Values[key] = defaultValue;
            }
            return (T)settings.Values[key];
        }

        public static void SetProperty<T>(string key, T value)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[key] = value;
        }

        public static DateTimeOffset InitialLaunchedDateTime
        {
            get
            {
                var now = DateTimeOffset.Now.ToString();
                var date = GetProperty(init_launched_datetime, now);
                return DateTimeOffset.Parse(date);
            }
        }

        public static bool PostviewSyncEnabled
        {
            get { return GetProperty(sync_postview, true); }
            set { SetProperty(sync_postview, value); }
        }

        public static int IntervalTime
        {
            get { return GetProperty(interval_time, 10); }
            set { SetProperty(interval_time, value); }
        }

        public static bool ShootButtonVisible
        {
            get { return GetProperty(display_take_image_button, true); }
            set { SetProperty(display_take_image_button, value); }
        }

        public static bool HistogramVisible
        {
            get { return GetProperty(display_histogram, true); }
            set { SetProperty(display_histogram, value); }
        }

        public static bool GeoTaggingEnabled
        {
            get { return GetProperty(add_geotag, false); }
            set { SetProperty(add_geotag, value); }
        }

        public static FramingGridTypes FramingGridType
        {
            get { return (FramingGridTypes)GetProperty(fraiming_grids, (int)FramingGridTypes.Off); }
            set { SetProperty(fraiming_grids, (int)value); }
        }

        public static FramingGridColors FramingGridColor
        {
            get { return (FramingGridColors)GetProperty(framing_grids_color, (int)FramingGridColors.White); }
            set { SetProperty(framing_grids_color, (int)value); }
        }

        public static FibonacciLineOrigins FibonacciOrigin
        {
            get { return (FibonacciLineOrigins)GetProperty(fibonacci_origin, (int)FibonacciLineOrigins.UpperLeft); }
            set { SetProperty(fibonacci_origin, (int)value); }
        }

        public static bool FocusFrameEnabled
        {
            get { return GetProperty(request_focus_frame_info, true); }
            set { SetProperty(request_focus_frame_info, value); }
        }

        public static bool OriginalSizeContentsPrioritized
        {
            get { return GetProperty(prioritize_original_contents, false); }
            set { SetProperty(prioritize_original_contents, value); }
        }

        public static ContentsSet RemoteContentsSet
        {
            get { return (ContentsSet)GetProperty(remote_contents_set, (int)ContentsSet.ImagesAndMovies); }
            set { SetProperty(remote_contents_set, (int)value); }
        }
    }
}
