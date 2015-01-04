﻿using Kazyx.ImageStream;
using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.Control;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Settings;
using Kazyx.Uwpmm.Utility;
using NtImageProcessor;
using NtNfcLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Networking.Proximity;
using Windows.Phone.UI.Input;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Kazyx.Uwpmm.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private LiveviewScreenViewData screenViewData;
        private HistogramCreator HistogramCreator;

        public MainPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);

            NavigatedByInAppBackTransition = e.NavigationMode == NavigationMode.Back;

            NetworkObserver.INSTANCE.CameraDiscovered += NetworkObserver_Discovered;
            NetworkObserver.INSTANCE.CdsDiscovered += NetworkObserver_CdsDiscovered;
            NetworkObserver.INSTANCE.Clear();
            NetworkObserver.INSTANCE.Search();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            NetworkObserver.INSTANCE.CameraDiscovered -= NetworkObserver_Discovered;
            NetworkObserver.INSTANCE.CdsDiscovered -= NetworkObserver_CdsDiscovered;

            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private bool NavigatedByInAppBackTransition = false;

        CommandBarManager _CommandBarManager = new CommandBarManager();
        Geolocator _Geolocator = new Geolocator();
        Geoposition CachedPosition = null;

        bool ControlPanelDisplayed = false;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _CommandBarManager.SetEvent(AppBarItem.ControlPanel, (s, args) =>
            {
                if (ControlPanelDisplayed) { StartToHideControlPanel(); }
                else { StartToShowControlPanel(); }
            });
            _CommandBarManager.SetEvent(AppBarItem.CancelTouchAF, async (s, args) =>
                {
                    if (target == null || target.Api == null) { return; }
                    await target.Api.Camera.CancelTouchAFAsync();
                });
            _CommandBarManager.SetEvent(AppBarItem.AppSetting, (s, args) =>
            {
                OpenAppSettingPanel();
            });
            _CommandBarManager.SetEvent(AppBarItem.Ok, (s, args) =>
            {
                CloseAppSettingPanel();
            });
            _CommandBarManager.SetEvent(AppBarItem.AboutPage, (s, args) =>
            {
                Frame.Navigate(typeof(AboutPage));
            });
            _CommandBarManager.SetEvent(AppBarItem.LoggerPage, (s, args) =>
            {
                Frame.Navigate(typeof(LogViewerPage));
            });
            _CommandBarManager.SetEvent(AppBarItem.PlaybackPage, (s, args) =>
            {
                Frame.Navigate(typeof(PlaybackPage));
            });
            _CommandBarManager.SetEvent(AppBarItem.WifiSetting, async (s, args) =>
            {
                await Launcher.LaunchUriAsync(new Uri("ms-settings-wifi:"));
            });
            _CommandBarManager.SetEvent(AppBarItem.Donation, (s, args) =>
            {
                Frame.Navigate(typeof(HiddenPage));
            });

            PivotRoot.SelectionChanged += PivotRoot_SelectionChanged;

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            HardwareButtons.CameraHalfPressed += async (_sender, arg) =>
            {
                if (target == null || target.Api == null || !target.Api.Capability.IsAvailable("actHalfPressShutter")) { return; }
                await target.Api.Camera.ActHalfPressShutterAsync();
            };
            HardwareButtons.CameraReleased += async (_sender, arg) =>
            {
                if (target == null || target.Api == null || !target.Api.Capability.IsAvailable("cancelHalfPressShutter")) { return; }
                await target.Api.Camera.CancelHalfPressShutterAsync();
            };
            HardwareButtons.CameraPressed += (_sender, arg) =>
            {
                ShutterButtonPressed();
            };

            _FocusFrameSurface.OnTouchFocusOperated += async (obj, args) =>
            {
                DebugUtil.Log("Touch AF operated: " + args.X + " " + args.Y);
                if (target == null || target.Api == null || !target.Api.Capability.IsAvailable("setTouchAFPosition")) { return; }
                await target.Api.Camera.SetAFPositionAsync(args.X, args.Y);
            };

            InitializeUI();
            InitializeProximityDevice();

            PictureDownloader.Instance.Fetched += async (folder, file, tagResult) =>
            {
                var thumb = await file.GetThumbnailAsync(ThumbnailMode.ListView, 100);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var image = new BitmapImage();
                    image.SetSource(thumb);
                    var path = file.Path.Split('\\');
                    var name = '\\' + path[path.Length - 2] + '\\' + path[path.Length - 1];
                    var text = SystemUtil.GetStringResource("Message_ImageDL_Succeed");
                    switch (tagResult)
                    {
                        case GeotaggingResult.OK:
                            text = SystemUtil.GetStringResource("Message_ImageDL_Succeed_withGeotag");
                            break;
                        case GeotaggingResult.GeotagAlreadyExists:
                            text = SystemUtil.GetStringResource("ErrorMessage_ImageDL_DuplicatedGeotag");
                            break;
                        case GeotaggingResult.UnExpectedError:
                            text = SystemUtil.GetStringResource("ErrorMessage_ImageDL_Geotagging");
                            break;
                    }
                    Toast.PushToast(new Control.ToastContent() { Text = text + name, Icon = image });
                    thumb.Dispose();
                });
            };

            PictureDownloader.Instance.Failed += (err, tagResult) =>
            {
                ShowError(SystemUtil.GetStringResource("ErrorMessage_ImageDL_Other") + err + " " + tagResult);
            };
        }

        private void EnableGeolocator()
        {
            _Geolocator.DesiredAccuracy = PositionAccuracy.Default;
            _Geolocator.ReportInterval = 3000;

            _Geolocator.PositionChanged += _Geolocator_PositionChanged;
            _Geolocator.StatusChanged += _Geolocator_StatusChanged;
            screenViewData.GeopositionEnabled = true;
        }

        private void DisableGeolocator()
        {
            _Geolocator.StatusChanged -= _Geolocator_StatusChanged;
            _Geolocator.PositionChanged -= _Geolocator_PositionChanged;
            CachedPosition = null;
            screenViewData.GeopositionEnabled = false;
        }

        void _Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            screenViewData.GeopositionStatus = args.Status;
            if (args.Status != PositionStatus.Ready)
            {
                this.CachedPosition = null;
            }
        }

        private void _Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            CachedPosition = args.Position;
            DebugUtil.Log("position: " + args.Position.Coordinate.Point.Position.Longitude + " " + args.Position.Coordinate.Point.Position.Latitude);
        }

        void InitializeUI()
        {
            HistogramControl.Init(Histogram.ColorType.White, 1500);

            HistogramCreator = null;
            HistogramCreator = new HistogramCreator(HistogramCreator.HistogramResolution.Resolution_128);
            HistogramCreator.OnHistogramCreated += async (r, g, b) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    HistogramControl.SetHistogramValue(r, g, b);
                });
            };
            HistogramControl.DataContext = ApplicationSettings.GetInstance();
            ShutterButtonWrapper.DataContext = ApplicationSettings.GetInstance();
            InitializeAppSettingPanel();
            FramingGuideSurface.DataContext = ApplicationSettings.GetInstance();
        }

        private void CreateEntranceAppBar()
        {
            this.BottomAppBar = _CommandBarManager.Clear()//
                .Icon(AppBarItem.WifiSetting)//
                .NoIcon(AppBarItem.AboutPage)//
                .NoIcon(AppBarItem.PlaybackPage)//
                .NoIcon(AppBarItem.LoggerPage)//
                .Icon(AppBarItem.Donation)//
                .CreateNew(0.6);
        }

        private void CreateCameraControlAppBar()
        {
            this.BottomAppBar = _CommandBarManager.Clear()//
                .Icon(AppBarItem.AppSetting)//
                .Icon(AppBarItem.ControlPanel)//
                .Icon(AppBarItem.PlaybackPage)//
                .CreateNew(0.6);
        }

        async void PivotRoot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((sender as Pivot).SelectedIndex)
            {
                case 0:
                    LiveViewPageUnloaded();
                    CreateEntranceAppBar();
                    if (target != null)
                    {
                        await SequentialOperation.CloseLiveviewStream(target.Api, liveview);
                        target.Observer.Stop();
                    }
                    target = null;
                    break;
                case 1:
                    if (target != null)
                    {
                        CreateCameraControlAppBar();
                        LiveViewPageLoaded();
                    }
                    else
                    {
                        // TODO search devices explicitly.
                    }
                    break;
            }
        }

        private async void LiveViewPageLoaded()
        {
            screenViewData = new LiveviewScreenViewData(target);
            Liveview.DataContext = screenViewData;
            ShutterButton.DataContext = screenViewData;
            BatteryStatusDisplay.DataContext = target.Status.BatteryInfo;
            _FocusFrameSurface.ClearFrames();

            if (ApplicationSettings.GetInstance().GeotagEnabled) { EnableGeolocator(); }
            else { DisableGeolocator(); }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                PivotRoot.IsLocked = true;
            });
        }

        private void LiveViewPageUnloaded()
        {
            LayoutRoot.DataContext = null;
        }

        private void Entrance_Loaded(object sender, RoutedEventArgs e)
        {
            CreateEntranceAppBar();
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (PivotRoot.SelectedIndex == 0)
            {
                return;
            }

            if (ControlPanelDisplayed)
            {
                StartToHideControlPanel();
                e.Handled = true;
                return;
            }

            GoToEntranceScreen();
            e.Handled = true;
        }

        private void StartToShowControlPanel()
        {
            SlideTransform.X = 200;
            ShowControlPanelStoryBoard.Begin();
            SlideInControlPanel.Begin();
        }

        private void StartToHideControlPanel()
        {
            HideControlPanelStoryBoard.Begin();
            SlideOutControlPanel.Begin();
        }

        private void ShowControlPanelStoryBoard_Completed(object sender, object e)
        {
            ControlPanelDisplayed = true;
            SlideTransform.X = 0;
        }

        private void HideControlPanelStoryBoard_Completed(object sender, object e)
        {
            ControlPanelDisplayed = false;
        }

        private TargetDevice target;
        private StreamProcessor liveview = new StreamProcessor();
        private ImageDataSource liveview_data = new ImageDataSource();
        private ImageDataSource postview_data = new ImageDataSource();

        private async void NetworkObserver_CdsDiscovered(object sender, CdServiceEventArgs e)
        {
            var type = await e.CdService.LocalAddress.IPInformation.NetworkAdapter.GetConnectedProfileAsync();
            if (!NavigatedByInAppBackTransition && type.IsWlanConnectionProfile)
            {
                var ssid = type.WlanConnectionProfileDetails.GetConnectedSsid();
                if (ssid != null && ssid.StartsWith("Direct-", StringComparison.OrdinalIgnoreCase))
                {
                    DebugUtil.Log("It seems to be connected to camera directly. Navigate to PlaybackPage automatically.");
                    await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        Frame.Navigate(typeof(PlaybackPage), PlaybackPage.AUTO_JUMP_TO_DLNA_FLAG);
                    });
                    return;
                }
            }
            // TODO
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ShowToast("[TMP] CDS discovered: " + e.CdService.FriendlyName);
            });
        }

        async void NetworkObserver_Discovered(object sender, CameraDeviceEventArgs e)
        {
            var target = e.CameraDevice;
            try
            {
                await SequentialOperation.SetUp(target, liveview);
            }
            catch (Exception ex)
            {
                DebugUtil.Log("Failed setup: " + ex.Message);
                ShowError(SystemUtil.GetStringResource("ErrorMessage_fatal"));
                return;
            }

            this.target = target;

            target.Status.PropertyChanged += Status_PropertyChanged;
            // TODO remove when the target is gone to out of control.

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                GoToLiveviewScreen();
                var panels = SettingPanelBuilder.CreateNew(target);
                var pn = panels.GetPanelsToShow();
                foreach (var panel in pn)
                {
                    ControlPanel.Children.Add(panel);
                }

                SetupFocusFrame(ApplicationSettings.GetInstance().RequestFocusFrameInfo);

                ShootingParamSliders.DataContext = new ShootingParamViewData() { Status = target.Status, Liveview = screenViewData };
                FnumberSlider.SliderOperated += async (s, arg) =>
                {
                    DebugUtil.Log("Fnumber operated: " + arg.Selected);
                    try { await target.Api.Camera.SetFNumberAsync(arg.Selected); }
                    catch (RemoteApiException) { }
                };
                SSSlider.SliderOperated += async (s, arg) =>
                {
                    DebugUtil.Log("SS operated: " + arg.Selected);
                    try { await target.Api.Camera.SetShutterSpeedAsync(arg.Selected); }
                    catch (RemoteApiException) { }
                };
                ISOSlider.SliderOperated += async (s, arg) =>
                {
                    DebugUtil.Log("ISO operated: " + arg.Selected);
                    try { await target.Api.Camera.SetISOSpeedAsync(arg.Selected); }
                    catch (RemoteApiException) { }
                };
                EvSlider.SliderOperated += async (s, arg) =>
                {
                    DebugUtil.Log("Ev operated: " + arg.Selected);
                    try { await target.Api.Camera.SetEvIndexAsync(arg.Selected); }
                    catch (RemoteApiException) { }
                };
                ProgramShiftSlider.SliderOperated += async (s, arg) =>
                {
                    DebugUtil.Log("Program shift operated: " + arg.OperatedStep);
                    try { await target.Api.Camera.SetProgramShiftAsync(arg.OperatedStep); }
                    catch (RemoteApiException) { }
                };
            });
        }

        private async void SetupFocusFrame(bool RequestFocusFrameEnabled)
        {
            if (RequestFocusFrameEnabled && target.Api.Capability.IsAvailable("setLiveviewFrameInfo"))
            {
                await target.Api.Camera.SetLiveviewFrameInfo(new FrameInfoSetting() { TransferFrameInfo = true });
            }

            if (RequestFocusFrameEnabled && !target.Api.Capability.IsSupported("setLiveviewFrameInfo") && target.Api.Capability.IsAvailable("setTouchAFPosition"))
            {
                // For devices which does not support to transfer focus frame info, draw focus frame itself.
                _FocusFrameSurface.SelfDrawTouchAFFrame = true;
            }
            else { _FocusFrameSurface.SelfDrawTouchAFFrame = false; }
        }

        void Status_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var status = sender as CameraStatus;
            switch (e.PropertyName)
            {
                case "FocusStatus":
                    DebugUtil.Log("Focus status changed: " + status.FocusStatus);
                    if (status.FocusStatus == Kazyx.RemoteApi.Camera.FocusState.Focused)
                    {
                        ShowCancelTouchAFButton();
                        _FocusFrameSurface.Focused = true;
                    }
                    else
                    {
                        HideCancelTouchAFButton();
                        _FocusFrameSurface.Focused = false;
                    }
                    break;
                case "TouchFocusStatus":
                    DebugUtil.Log("TouchFocusStatus changed: " + status.TouchFocusStatus.Focused);
                    if (status.TouchFocusStatus.Focused)
                    {
                        ShowCancelTouchAFButton();
                        _FocusFrameSurface.Focused = true;
                    }
                    else
                    {
                        HideCancelTouchAFButton();
                        _FocusFrameSurface.Focused = false;
                    }
                    break;
                case "PictureUrls":
                    foreach (var url in status.PictureUrls)
                    {
                        PictureDownloader.Instance.Enqueue(new Uri(url, UriKind.Absolute), CachedPosition);
                    }
                    break;
                case "BatteryInfo":
                    BatteryStatusDisplay.BatteryInfo = status.BatteryInfo;
                    break;
                case "AvailableApis":
                    SetupFocusFrame(ApplicationSettings.GetInstance().RequestFocusFrameInfo);
                    break;
                default:
                    break;
            }
        }

        private void HideCancelTouchAFButton()
        {
            this.BottomAppBar = _CommandBarManager.Disable(AppBarItemType.WithIcon, AppBarItem.CancelTouchAF).CreateNew(0.6);
        }

        void ShowCancelTouchAFButton()
        {
            this.BottomAppBar = _CommandBarManager.Icon(AppBarItem.CancelTouchAF).CreateNew(0.6);
        }

        private void GoToLiveviewScreen()
        {
            PivotRoot.SelectedIndex = 1;
        }

        private void GoToEntranceScreen()
        {
            PivotRoot.IsLocked = false;
            PivotRoot.SelectedIndex = 0;
        }

        private bool IsRendering = false;

        async void liveview_JpegRetrieved(object sender, JpegEventArgs e)
        {
            if (IsRendering) { return; }

            IsRendering = true;
            await LiveviewUtil.SetAsBitmap(e.Packet.ImageData, liveview_data, HistogramCreator, Dispatcher);
            IsRendering = false;
        }

        async void liveview_FocusFrameRetrieved(object sender, FocusFrameEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _FocusFrameSurface.SetFocusFrames(e.Packet.FocusFrames);
            });
        }

        void liveview_Closed(object sender, EventArgs e)
        {
            DebugUtil.Log("Liveview connection closed");
        }

        private void LiveviewImage_Loaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            image.DataContext = liveview_data;
            liveview.JpegRetrieved += liveview_JpegRetrieved;
            liveview.FocusFrameRetrieved += liveview_FocusFrameRetrieved;
            liveview.Closed += liveview_Closed;
        }

        private void LiveviewImage_Unloaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            image.DataContext = null;
            liveview.JpegRetrieved -= liveview_JpegRetrieved;
            liveview.FocusFrameRetrieved -= liveview_FocusFrameRetrieved;
            liveview.Closed -= liveview_Closed;
        }

        private async void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.ActionStop); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomOutButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.Action1Shot); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomOutButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.ActionStart); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStop); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomInButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.Action1Shot); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomInButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStart); }
            catch (RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private void ShutterButton_Click(object sender, RoutedEventArgs e)
        {
            ShutterButtonPressed();
        }

        private void ShutterButton_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {

        }

        PeriodicalShootingTask PeriodicalShootingTask;

        async void ShutterButtonPressed()
        {
            if (target == null || target.Status.ShootMode == null) { return; }
            if (target.Status.ShootMode.Current == ShootModeParam.Still)
            {
                if (PeriodicalShootingTask != null && PeriodicalShootingTask.IsRunning)
                {
                    PeriodicalShootingTask.Stop();
                }
                else if (ApplicationSettings.GetInstance().IsIntervalShootingEnabled)
                {
                    PeriodicalShootingTask = new PeriodicalShootingTask(new List<TargetDevice>() { target }, ApplicationSettings.GetInstance().IntervalTime);
                    PeriodicalShootingTask.Tick += async (result) =>
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            switch (result)
                            {
                                case PeriodicalShootingTask.ShootingResult.Skipped:
                                    ShowToast(SystemUtil.GetStringResource("PeriodicalShooting_Skipped"));
                                    break;
                                case PeriodicalShootingTask.ShootingResult.Succeed:
                                    ShowToast(SystemUtil.GetStringResource("Message_ImageCapture_Succeed"));
                                    break;
                            };
                        });
                    };
                    PeriodicalShootingTask.Stopped += async (reason) =>
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            switch (reason)
                            {
                                case PeriodicalShootingTask.StopReason.ShootingFailed:
                                    ShowError(SystemUtil.GetStringResource("ErrorMessage_Interval"));
                                    break;
                                case PeriodicalShootingTask.StopReason.SkipLimitExceeded:
                                    ShowError(SystemUtil.GetStringResource("PeriodicalShooting_SkipLimitExceed"));
                                    break;
                                case PeriodicalShootingTask.StopReason.RequestedByUser:
                                    ShowToast(SystemUtil.GetStringResource("PeriodicalShooting_StoppedByUser"));
                                    break;
                            };
                        });
                    };
                    PeriodicalShootingTask.StatusUpdated += async (status) =>
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            DebugUtil.Log("Status updated: " + status.Count);
                            if (status.IsRunning)
                            {
                                PeriodicalShootingStatus.Visibility = Visibility.Visible;
                                PeriodicalShootingStatusText.Text = SystemUtil.GetStringResource("PeriodicalShooting_Status")
                                    .Replace("__INTERVAL__", status.Interval.ToString())
                                    .Replace("__PHOTO_NUM__", status.Count.ToString());
                            }
                            else { PeriodicalShootingStatus.Visibility = Visibility.Collapsed; }
                        });
                    };
                    PeriodicalShootingTask.Start();
                }
                else
                {
                    try
                    {
                        await SequentialOperation.TakePicture(target.Api, CachedPosition);
                        if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled)
                        {
                            ShowToast(SystemUtil.GetStringResource("Message_ImageCapture_Succeed"));
                        }
                    }
                    catch (RemoteApiException ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        ShowError(SystemUtil.GetStringResource("ErrorMessage_shootingFailure"));
                    }
                }
            }
            else if (target.Status.ShootMode.Current == ShootModeParam.Movie)
            {
                if (target.Status.Status == EventParam.Idle)
                {
                    try { await target.Api.Camera.StartMovieRecAsync(); }
                    catch (RemoteApiException ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        ShowError(SystemUtil.GetStringResource("ErrorMessage_shootingFailure"));
                    }
                }
                else if (target.Status.Status == EventParam.MvRecording)
                {
                    try { await target.Api.Camera.StopMovieRecAsync(); }
                    catch (RemoteApiException ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        ShowError(SystemUtil.GetStringResource("ErrorMessage_fatal"));
                    }
                }
            }
            else if (target.Status.ShootMode.Current == ShootModeParam.Audio)
            {
                if (target.Status.Status == EventParam.Idle)
                {
                    try { await target.Api.Camera.StartAudioRecAsync(); }
                    catch (RemoteApiException ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        ShowError(SystemUtil.GetStringResource("ErrorMessage_shootingFailure"));
                    }
                }
                else if (target.Status.Status == EventParam.AuRecording)
                {
                    try { await target.Api.Camera.StopAudioRecAsync(); }
                    catch (RemoteApiException ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        ShowError(SystemUtil.GetStringResource("ErrorMessage_fatal"));
                    }
                }
            }
            else if (target.Status.ShootMode.Current == ShootModeParam.Interval)
            {
                if (target.Status.Status == EventParam.Idle)
                {
                    try { await target.Api.Camera.StartIntervalStillRecAsync(); }
                    catch (RemoteApiException ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        ShowError(SystemUtil.GetStringResource("ErrorMessage_shootingFailure"));
                    }
                }
                else if (target.Status.Status == EventParam.ItvRecording)
                {
                    try { await target.Api.Camera.StopIntervalStillRecAsync(); }
                    catch (RemoteApiException ex)
                    {
                        DebugUtil.Log(ex.StackTrace);
                        ShowError(SystemUtil.GetStringResource("ErrorMessage_fatal"));
                    }
                }
            }
        }

        private async void ShowError(string error)
        {
            var dialog = new MessageDialog(error);
            await dialog.ShowAsync();
        }

        private void LiveviewImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var rh = (sender as Image).RenderSize.Height;
            var rw = (sender as Image).RenderSize.Width;
            this._FocusFrameSurface.Height = rh;
            this._FocusFrameSurface.Width = rw;
            this.FramingGuideSurface.Height = rh;
            this.FramingGuideSurface.Width = rw;
        }

        private void ShowToast(string s)
        {
            Toast.PushToast(new Control.ToastContent() { Text = s });
        }

        ProximityDevice ProximityDevice;

        private void InitializeProximityDevice()
        {
            try
            {
                ProximityDevice = ProximityDevice.GetDefault();
            }
            catch (FileNotFoundException)
            {
                ProximityDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. ");
                return;
            }
            catch (COMException)
            {
                ProximityDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. ");
                return;
            }

            if (ProximityDevice == null)
            {
                DebugUtil.Log("It seems this is not NFC available device");
                return;
            }

            try
            {
                ProximityDevice.SubscribeForMessage("NDEF", ProximityMessageReceivedHandler);
            }
            catch (Exception e)
            {
                ProximityDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. " + e.Message + e.StackTrace);
                return;
            }

            NfcAvailable.Visibility = Visibility.Visible;
        }

        private async void ProximityMessageReceivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            var parser = new NdefParser(message);
            var ndefRecords = new List<NdefRecord>();

            var err = "";

            try { ndefRecords = parser.Parse(); }
            catch (NoSonyNdefRecordException) { err = SystemUtil.GetStringResource("ErrorMessage_CantFindSonyRecord"); }
            catch (NoNdefRecordException) { err = SystemUtil.GetStringResource("ErrorMessage_ParseNFC"); }
            catch (NdefParseException) { err = SystemUtil.GetStringResource("ErrorMessage_ParseNFC"); }
            catch (Exception) { err = SystemUtil.GetStringResource("ErrorMessage_fatal"); }

            if (err != "")
            {
                DebugUtil.Log("Failed to read NFC: " + err);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { ShowError(err); });
            }

            if (ndefRecords.Count > 0)
            {
                foreach (NdefRecord r in ndefRecords)
                {
                    if (r.SSID.Length > 0 && r.Password.Length > 0)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            // TODO: find any easy way to connect the camera
                            // Clipboard.SetText(r.Password);
                            var sb = new StringBuilder();
                            sb.Append(SystemUtil.GetStringResource("Message_NFC_succeed"));
                            sb.Append(System.Environment.NewLine);
                            sb.Append(System.Environment.NewLine);
                            sb.Append("SSID: ");
                            sb.Append(r.SSID);
                            sb.Append(System.Environment.NewLine);
                            sb.Append("Password: ");
                            sb.Append(r.Password);
                            sb.Append(System.Environment.NewLine);
                            sb.Append(System.Environment.NewLine);
                            var dialog = new MessageDialog(sb.ToString());
                            await dialog.ShowAsync();
                        });
                        break;
                    }
                }
            }
        }

        private AppSettingData<bool> geoSetting;
        private AppSettingData<int> gridColorSetting;
        private AppSettingData<int> fibonacciOriginSetting;
        private AppSettingData<bool> FocusFrameSetting;

        private void OpenAppSettingPanel()
        {
            // TODO: cancel touch AF and close other panels.
            AppSettingPanel.Visibility = Visibility.Visible;
            ShowSettingAnimation.Begin();
            this.BottomAppBar = _CommandBarManager.Clear().Icon(AppBarItem.Ok).CreateNew(0.6);
        }

        private void CloseAppSettingPanel()
        {
            HideSettingAnimation.Begin();
            CreateCameraControlAppBar();
        }

        private void InitializeAppSettingPanel()
        {
            var image_settings = new SettingSection(SystemUtil.GetStringResource("SettingSection_Image"));

            AppSettings.Children.Add(image_settings);

            image_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("PostviewTransferSetting"), SystemUtil.GetStringResource("Guide_ReceiveCapturedImage"),
                () => { return ApplicationSettings.GetInstance().IsPostviewTransferEnabled; },
                enabled => { ApplicationSettings.GetInstance().IsPostviewTransferEnabled = enabled; })));

            geoSetting = new AppSettingData<bool>(SystemUtil.GetStringResource("AddGeotag"), SystemUtil.GetStringResource("AddGeotag_guide"),
                () => { return ApplicationSettings.GetInstance().GeotagEnabled; },
                enabled =>
                {
                    ApplicationSettings.GetInstance().GeotagEnabled = enabled;
                    if (enabled) { EnableGeolocator(); }
                    else { DisableGeolocator(); }
                });
            image_settings.Add(new CheckBoxSetting(geoSetting));

            var display_settings = new SettingSection(SystemUtil.GetStringResource("SettingSection_Display"));

            AppSettings.Children.Add(display_settings);

            display_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("DisplayTakeImageButtonSetting"), SystemUtil.GetStringResource("Guide_DisplayTakeImageButtonSetting"),
                () => { return ApplicationSettings.GetInstance().IsShootButtonDisplayed; },
                enabled => { ApplicationSettings.GetInstance().IsShootButtonDisplayed = enabled; })));

            display_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(SystemUtil.GetStringResource("DisplayHistogram"), SystemUtil.GetStringResource("Guide_Histogram"),
                () => { return ApplicationSettings.GetInstance().IsHistogramDisplayed; },
                enabled => { ApplicationSettings.GetInstance().IsHistogramDisplayed = enabled; })));

            FocusFrameSetting = new AppSettingData<bool>(SystemUtil.GetStringResource("FocusFrameDisplay"), SystemUtil.GetStringResource("Guide_FocusFrameDisplay"),
                () => { return ApplicationSettings.GetInstance().RequestFocusFrameInfo; },
                enabled =>
                {
                    ApplicationSettings.GetInstance().RequestFocusFrameInfo = enabled;
                    SetupFocusFrame(enabled);
                    if (!enabled) { _FocusFrameSurface.ClearFrames(); }
                });
            display_settings.Add(new CheckBoxSetting(FocusFrameSetting));

            display_settings.Add(new ComboBoxSetting(
                new AppSettingData<int>(SystemUtil.GetStringResource("FramingGrids"), SystemUtil.GetStringResource("Guide_FramingGrids"),
                    () => { return ApplicationSettings.GetInstance().GridTypeIndex; },
                    setting =>
                    {
                        if (setting < 0) { return; }
                        ApplicationSettings.GetInstance().GridTypeIndex = setting;
                        DisplayGridColorSetting(ApplicationSettings.GetInstance().GridTypeSettings[setting] != FramingGridTypes.Off);
                        DisplayFibonacciOriginSetting(ApplicationSettings.GetInstance().GridTypeSettings[setting] == FramingGridTypes.Fibonacci);
                    },
                    SettingValueConverter.FromFramingGrid(ApplicationSettings.GetInstance().GridTypeSettings.ToArray())
                    )));

            gridColorSetting = new AppSettingData<int>(SystemUtil.GetStringResource("FramingGridColor"), null,
                    () => { return ApplicationSettings.GetInstance().GridColorIndex; },
                    setting =>
                    {
                        if (setting < 0) { return; }
                        ApplicationSettings.GetInstance().GridColorIndex = setting;
                    },
                    SettingValueConverter.FromFramingGridColor(ApplicationSettings.GetInstance().GridColorSettings.ToArray()));
            display_settings.Add(new ComboBoxSetting(gridColorSetting));

            fibonacciOriginSetting = new AppSettingData<int>(SystemUtil.GetStringResource("FibonacciSpiralOrigin"), null,
                () => { return ApplicationSettings.GetInstance().FibonacciOriginIndex; },
                setting =>
                {
                    if (setting < 0) { return; }
                    ApplicationSettings.GetInstance().FibonacciOriginIndex = setting;
                },
                SettingValueConverter.FromFibonacciLineOrigin(ApplicationSettings.GetInstance().FibonacciLineOriginSettings.ToArray()));
            display_settings.Add(new ComboBoxSetting(fibonacciOriginSetting));

            HideSettingAnimation.Completed += HideSettingAnimation_Completed;
        }

        private void DisplayGridColorSetting(bool displayed)
        {
            if (gridColorSetting != null)
            {
                gridColorSetting.IsActive = displayed;
            }
        }

        private void DisplayFibonacciOriginSetting(bool displayed)
        {
            if (fibonacciOriginSetting != null)
            {
                fibonacciOriginSetting.IsActive = displayed;
            }
        }

        private void HideSettingAnimation_Completed(object sender, object e)
        {
            AppSettingPanel.Visibility = Visibility.Collapsed;
        }

        public void StartOpenSliderAnimation(double from, double to)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            var sb = new Storyboard() { Duration = duration };
            var da = new DoubleAnimation() { Duration = duration };

            sb.Children.Add(da);

            var rt = new RotateTransform();

            Storyboard.SetTarget(da, rt);
            Storyboard.SetTargetProperty(da, "Angle");
            da.From = from;
            da.To = to;

            OpenSliderImage.RenderTransform = rt;
            OpenSliderImage.RenderTransformOrigin = new Point(0.5, 0.5);
            sb.Begin();
        }

        private void Grid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            OpenCloseSliders();
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OpenCloseSliders();
        }

        private void OpenCloseSliders()
        {
            if (ShootingParamSliders.Visibility == Visibility.Visible)
            {
                ShootingParamSliders.Visibility = Visibility.Collapsed;
                StartOpenSliderAnimation(180, 0);
            }
            else
            {
                ShootingParamSliders.Visibility = Visibility.Visible;
                StartOpenSliderAnimation(0, 180);
            }
        }
    }
}
