﻿using Kazyx.Uwpmm.DataModel;
using NtImageProcessor;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.Utility
{
    public class LiveviewUtil
    {
        public static async Task SetAsBitmap(byte[] data, ImageDataSource target, HistogramCreator Histogram, CoreDispatcher Dispatcher = null)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(data.AsBuffer());
                stream.Seek(0);
                if (Dispatcher == null)
                {
                    Dispatcher = SystemUtil.GetCurrentDispatcher();
                }
                if (Dispatcher == null)
                {
                    return;
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    var image = new BitmapImage();
                    image.SetSource(stream);
                    target.Image = image;
                });

                if (ApplicationSettings.GetInstance().IsHistogramDisplayed && !Histogram.IsRunning)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        Histogram.IsRunning = true;
                        stream.Seek(0);
                        var image = new BitmapImage();
                        image.SetSource(stream);
                        var writableImage = new WriteableBitmap(image.PixelWidth, image.PixelHeight);
                        stream.Seek(0);
                        writableImage.SetSource(stream);
                        Histogram.CreateHistogram(writableImage);
                    });
                }
                else
                {
                    DebugUtil.Log("Histogram creating. skip.");
                }
            }
        }
    }
}
