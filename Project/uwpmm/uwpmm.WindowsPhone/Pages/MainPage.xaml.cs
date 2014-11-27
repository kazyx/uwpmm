﻿using Kazyx.DeviceDiscovery;
using Kazyx.ImageStream;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Settings;
using Kazyx.Uwpmm.Utility;
using NtImageProcessor;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        CommandBarManager _CommandBarManager = new CommandBarManager();

        bool ControlPanelDisplayed = false;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var discovery = new SsdpDiscovery();
            discovery.SonyCameraDeviceDiscovered += discovery_ScalarDeviceDiscovered;
            discovery.SearchSonyCameraDevices();

            _CommandBarManager.SetEvent(AppBarItem.ControlPanel, (s, args) =>
            {
                if (ControlPanelDisplayed) { StartToHideControlPanel(); }
                else { StartToShowControlPanel(); }
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

            PivotRoot.SelectionChanged += PivotRoot_SelectionChanged;

            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Windows.Phone.UI.Input.HardwareButtons.CameraHalfPressed += async (_sender, arg) =>
            {
                if (target == null || target.Api == null || !target.Api.Capability.IsAvailable("actHalfPressShutter")) { return; }
                await target.Api.Camera.ActHalfPressShutterAsync();
            };
            Windows.Phone.UI.Input.HardwareButtons.CameraReleased += async (_sender, arg) =>
            {
                if (target == null || target.Api == null || !target.Api.Capability.IsAvailable("cancelHalfPressShutter")) { return; }
                await target.Api.Camera.CancelHalfPressShutterAsync();
            };
            Windows.Phone.UI.Input.HardwareButtons.CameraPressed += (_sender, arg) =>
            {
                ShutterButtonPressed();
            };

            InitializeUI();
        }

        void InitializeUI()
        {
            HistogramControl.Init(Control.Histogram.ColorType.White, 1500);

            HistogramCreator = null;
            HistogramCreator = new HistogramCreator(HistogramCreator.HistogramResolution.Resolution_128);
            HistogramCreator.OnHistogramCreated += async (r, g, b) => 
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    HistogramControl.SetHistogramValue(r, g, b);
                });
            };
        }

        private void CreateEntranceAppBar()
        {
            this.BottomAppBar = _CommandBarManager.Clear().NoIcon(AppBarItem.AboutPage).NoIcon(AppBarItem.PlaybackPage).NoIcon(AppBarItem.LoggerPage)
                .CreateNew(0.6);
        }

        async void PivotRoot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((sender as Pivot).SelectedIndex)
            {
                case 0:
                    CreateEntranceAppBar();
                    if (target != null)
                    {
                        await SequentialOperation.CloseLiveviewStream(target.Api, liveview);
                        target.Observer.Stop();
                    }
                    break;
                case 1:
                    this.BottomAppBar = _CommandBarManager.Clear().Icon(AppBarItem.ControlPanel).CreateNew(0.6);
                    break;
            }
        }

        private void Entrance_Loaded(object sender, RoutedEventArgs e)
        {
            CreateEntranceAppBar();
        }

        void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
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
        private SsdpDiscovery discovery = new SsdpDiscovery();
        private StreamProcessor liveview = new StreamProcessor();
        private ImageDataSource liveview_data = new ImageDataSource();
        private ImageDataSource postview_data = new ImageDataSource();

        async void discovery_ScalarDeviceDiscovered(object sender, SonyCameraDeviceEventArgs e)
        {
            var api = new DeviceApiHolder(e.SonyCameraDevice);
            api.SupportedApisUpdated += api_SupportedApisUpdated;
            api.AvailiableApisUpdated += api_AvailiableApisUpdated;

            TargetDevice target = null;
            try
            {
                target = await SequentialOperation.SetUp(e.SonyCameraDevice.UDN, api, liveview);
            }
            catch (Exception ex)
            {
                DebugUtil.Log("Failed setup: " + ex.Message);
                return;
            }

            this.target = target;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                GoToLiveviewScreen();
                var panels = SettingPanelBuilder.CreateNew(target);
                var pn = panels.GetPanelsToShow();
                foreach (var panel in pn)
                {
                    ControlPanel.Children.Add(panel);
                }
                screenViewData = new LiveviewScreenViewData(target);
                Bottom.DataContext = screenViewData;
                _FocusFrameSurface.ClearFrames();
            });
        }

        async void api_AvailiableApisUpdated(object sender, AvailableApiEventArgs e)
        {
            if (target == null) { return; }
            if (e.AvailableApis.Contains("setLiveviewFrameInfo"))
            {
                await target.Api.Camera.SetLiveviewFrameInfo(new FrameInfoSetting() { TransferFrameInfo = true });
            }
        }

        private void api_SupportedApisUpdated(object sender, SupportedApiEventArgs e)
        {
        }

        private void GoToLiveviewScreen()
        {
            PivotRoot.SelectedIndex = 1;
        }

        private void GoToEntranceScreen()
        {
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
            catch (RemoteApi.RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }

        }

        private async void ZoomOutButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.Action1Shot); }
            catch (RemoteApi.RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomOutButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionOut, ZoomParam.ActionStart); }
            catch (RemoteApi.RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }

        }

        private async void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStop); }
            catch (RemoteApi.RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }

        }

        private async void ZoomInButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.Action1Shot); }
            catch (RemoteApi.RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private async void ZoomInButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try { await target.Api.Camera.ActZoomAsync(ZoomParam.DirectionIn, ZoomParam.ActionStart); }
            catch (RemoteApi.RemoteApiException ex) { DebugUtil.Log(ex.StackTrace); }
        }

        private void ShutterButton_Click(object sender, RoutedEventArgs e)
        {
            ShutterButtonPressed();
        }

        private void ShutterButton_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {

        }

        async void ShutterButtonPressed()
        {
            if (target == null || target.Status.ShootMode == null) { return; }
            if (target.Status.ShootMode.Current == ShootModeParam.Still)
            {
                try
                {
                    await target.Api.Camera.ActTakePictureAsync();
                }
                catch (RemoteApi.RemoteApiException) { }
            }
        }

        private void LiveviewImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var rh = (sender as Image).RenderSize.Height;
            var rw = (sender as Image).RenderSize.Width;
            this._FocusFrameSurface.Height = rh;
            this._FocusFrameSurface.Width = rw;
        }
    }
}
