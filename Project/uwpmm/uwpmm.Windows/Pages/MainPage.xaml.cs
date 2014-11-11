﻿using Kazyx.DeviceDiscovery;
using Kazyx.ImageStream;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.Common;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Settings;
using Kazyx.Uwpmm.Utility;
using System;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Kazyx.Uwpmm.Pages
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public MainPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
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
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            var discovery = new SsdpDiscovery();
            discovery.SonyCameraDeviceDiscovered += discovery_ScalarDeviceDiscovered;
            discovery.SearchSonyCameraDevices();
        }

        private TargetDevice target;
        private SsdpDiscovery discovery = new SsdpDiscovery();
        private StreamProcessor liveview = new StreamProcessor();
        private ImageDataSource liveview_data = new ImageDataSource();
        private ImageDataSource postview_data = new ImageDataSource();

        async void discovery_ScalarDeviceDiscovered(object sender, SonyCameraDeviceEventArgs e)
        {
            var api = new DeviceApiHolder(e.SonyCameraDevice);
            TargetDevice target = null;
            try
            {
                target = await SequentialOperation.SetUp(api, liveview);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed setup: " + ex.Message);
                return;
            }

            this.target = target;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var panels = SettingPanelBuilder.CreateNew(target);
                var pn = panels.GetPanelsToShow();
                foreach (var panel in pn)
                {
                    ControlPanel.Children.Add(panel);
                }
            });
        }

        private bool IsRendering = false;

        async void liveview_JpegRetrieved(object sender, JpegEventArgs e)
        {
            if (IsRendering) { return; }

            IsRendering = true;
            await LiveviewUtil.SetAsBitmap(e.Packet.ImageData, liveview_data, Dispatcher);
            IsRendering = false;
        }

        void liveview_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine("Liveview connection closed");
        }

        private void LiveviewImage_Loaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            image.DataContext = liveview_data;
            liveview.JpegRetrieved += liveview_JpegRetrieved;
            liveview.Closed += liveview_Closed;
        }

        private void LiveviewImage_Unloaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            image.DataContext = null;
            liveview.JpegRetrieved -= liveview_JpegRetrieved;
            liveview.Closed -= liveview_Closed;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (target == null) return;
            await SequentialOperation.TakePicture(target.Api, async (file) =>
            {
                var stream = await file.OpenReadAsync();
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var image = new BitmapImage();
                    image.SetSource(stream);
                    PostviewImage.Source = image;
                    stream.Dispose();
                });
            });
        }

        private async void ZoomOut_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ZoomOperation.ZoomOut(target.Api.Camera);
        }

        private async void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            await ZoomOperation.StopZoomOut(target.Api.Camera);
        }

        private async void ZoomOut_Holding(object sender, HoldingRoutedEventArgs e)
        {
            await ZoomOperation.StartZoomOut(target.Api.Camera);
        }

        private async void ZoomOut_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //await ZoomOperation.StartZoomOut(target.Api.Camera);
        }

        private async void ZoomOut_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //await ZoomOperation.StopZoomOut(target.Api.Camera);
        }

        private async void ZoomIn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ZoomOperation.ZoomIn(target.Api.Camera);
        }

        private async void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            await ZoomOperation.StopZoomIn(target.Api.Camera);
        }

        private async void ZoomIn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            await ZoomOperation.StartZoomIn(target.Api.Camera);
        }

        private async void ZoomIn_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //await ZoomOperation.StartZoomIn(target.Api.Camera);
        }

        private async void ZoomIn_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // await ZoomOperation.StopZoomIn(target.Api.Camera);
        }
    }
}