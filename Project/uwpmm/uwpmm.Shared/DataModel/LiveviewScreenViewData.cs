﻿
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Utility;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
namespace Kazyx.Uwpmm.DataModel
{
    public class LiveviewScreenViewData : ObservableBase
    {
        readonly TargetDevice Device;

        public LiveviewScreenViewData(TargetDevice d)
        {
            Device = d;
            Device.Status.PropertyChanged += (sender, e) =>
            {
                NotifyChangedOnUI("ZoomPositionInCurrentBox");
                NotifyChangedOnUI("ZoomBoxIndex");
                NotifyChangedOnUI("ZoomBoxNum");
                NotifyChangedOnUI("ShutterButtonImage");
                NotifyChangedOnUI("ShutterButtonEnabled");
                NotifyChangedOnUI("RecDisplayVisibility");
                NotifyChangedOnUI("ProgressDisplayVisibility");
                NotifyChangedOnUI("ShootModeImage");
                NotifyChangedOnUI("ExposureModeImage");
            };
            Device.Api.AvailiableApisUpdated += (sender, e) =>
            {
                NotifyChangedOnUI("ZoomInterfacesVisibility");
                NotifyChangedOnUI("ShutterButtonEnabled");
                NotifyChangedOnUI("RecDisplayVisibility");
            };
        }

        private static readonly BitmapImage StillImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Camera.png", UriKind.Absolute));
        private static readonly BitmapImage CamImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Camcorder.png", UriKind.Absolute));
        private static readonly BitmapImage AudioImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Music.png", UriKind.Absolute));
        private static readonly BitmapImage StopImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/Stop.png", UriKind.Absolute));
        private static readonly BitmapImage IntervalStillImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/IntervalStillRecButton.png", UriKind.Absolute));
        private static readonly BitmapImage ContShootingImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ContShootingButton.png", UriKind.Absolute));

        private static readonly BitmapImage StillModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_photo.png", UriKind.Absolute));
        private static readonly BitmapImage MovieModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_movie.png", UriKind.Absolute));
        private static readonly BitmapImage IntervalModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_interval.png", UriKind.Absolute));
        private static readonly BitmapImage AudioModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/mode_audio.png", UriKind.Absolute));

        private static readonly BitmapImage AModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_A.png", UriKind.Absolute));
        private static readonly BitmapImage IAModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_iA.png", UriKind.Absolute));
        private static readonly BitmapImage IAPlusModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_iAPlus.png", UriKind.Absolute));
        private static readonly BitmapImage MModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_M.png", UriKind.Absolute));
        private static readonly BitmapImage SModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_S.png", UriKind.Absolute));
        private static readonly BitmapImage PModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_P.png", UriKind.Absolute));
        private static readonly BitmapImage PShiftModeImage = new BitmapImage(new Uri("ms-appx:///Assets/LiveviewScreen/ExposureMode_P_shift.png", UriKind.Absolute));

        public BitmapImage ShutterButtonImage
        {
            get
            {
                if (Device.Status.ShootMode == null)
                {
                    return StillImage;
                }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Still:
                        return StillImage;
                    case ShootModeParam.Movie:
                        return CamImage;
                    case ShootModeParam.Audio:
                        return AudioImage;
                    case ShootModeParam.Interval:
                        return IntervalStillImage;
                    default:
                        return null;
                }
            }
        }

        public BitmapImage ShootModeImage
        {
            get
            {
                if (Device.Status.ShootMode == null) { return null; }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Still:
                        return StillModeImage;
                    case ShootModeParam.Movie:
                        return MovieModeImage;
                    case ShootModeParam.Interval:
                        return IntervalModeImage;
                    case ShootModeParam.Audio:
                        return AudioImage;
                }
                return null;
            }
        }

        public BitmapImage ExposureModeImage
        {
            get
            {
                if (Device.Status.ExposureMode == null) { return null; }
                switch (Device.Status.ExposureMode.Current)
                {
                    case ExposureMode.Intelligent:
                        return IAModeImage;
                    case ExposureMode.Superior:
                        return IAPlusModeImage;
                    case ExposureMode.Program:
                        return PModeImage;
                    case ExposureMode.Aperture:
                        return AModeImage;
                    case ExposureMode.SS:
                        return SModeImage;
                    case ExposureMode.Manual:
                        return MModeImage;
                }
                return null;
            }
        }

        public bool ShutterButtonEnabled
        {
            get
            {
                if (Device.Api.Capability == null)
                {
                    return false;
                }
                switch (Device.Status.ShootMode.Current)
                {
                    case ShootModeParam.Still:
                        if (Device.Status.Status == EventParam.Idle) { return true; }
                        break;
                    case ShootModeParam.Movie:
                        if (Device.Status.Status == EventParam.Idle || Device.Status.Status == EventParam.MvRecording) { return true; }
                        break;
                    case ShootModeParam.Audio:
                        if (Device.Status.Status == EventParam.Idle || Device.Status.Status == EventParam.AuRecording) { return true; }
                        break;
                    case ShootModeParam.Interval:
                        if (Device.Status.Status == EventParam.Idle || Device.Status.Status == EventParam.ItvRecording) { return true; }
                        break;
                }
                return false;
            }
        }

        public Visibility ZoomInterfacesVisibility
        {
            get
            {
                if (Device.Api.Capability.IsAvailable("actZoom")) { return Visibility.Visible; }
                return Visibility.Collapsed;
            }
        }

        public Visibility RecDisplayVisibility
        {
            get
            {
                if (Device.Status == null) { return Visibility.Collapsed; }
                switch (Device.Status.Status)
                {
                    case EventParam.MvRecording:
                    case EventParam.AuRecording:
                    case EventParam.ItvRecording:
                        return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public Visibility ProgressDisplayVisibility
        {
            get
            {
                if (Device.Status == null) { return Visibility.Visible; }
                switch (Device.Status.Status)
                {
                    case EventParam.Idle:
                    case EventParam.MvRecording:
                    case EventParam.AuRecording:
                    case EventParam.ItvRecording:
                        return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
        }

        public int ZoomPositionInCurrentBox
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                DebugUtil.Log("Zoom pos " + Device.Status.ZoomInfo.PositionInCurrentBox);
                return Device.Status.ZoomInfo.PositionInCurrentBox;
            }
        }

        public int ZoomBoxIndex
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                return Device.Status.ZoomInfo.CurrentBoxIndex;
            }
        }

        public int ZoomBoxNum
        {
            get
            {
                if (Device.Status.ZoomInfo == null) { return 0; }
                return Device.Status.ZoomInfo.NumberOfBoxes;
            }
        }
    }
}
