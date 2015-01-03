﻿using Kazyx.RemoteApi;
using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.Playback
{
    public class PlaybackModeHelper
    {
        public static async Task<bool> MoveToShootingModeAsync(CameraApiClient camera, CameraStatus status, int timeoutMsec = 10000)
        {
            return await MoveToSpecifiedModeAsync(camera, status, CameraFunction.RemoteShooting, EventParam.Idle, timeoutMsec);
        }

        public static async Task<bool> MoveToContentTransferModeAsync(CameraApiClient camera, CameraStatus status, int timeoutMsec = 10000)
        {
            return await MoveToSpecifiedModeAsync(camera, status, CameraFunction.ContentTransfer, EventParam.ContentsTransfer, timeoutMsec);
        }

        private static async Task<bool> MoveToSpecifiedModeAsync(CameraApiClient camera, CameraStatus status, string nextFunction, string nextState, int timeoutMsec)
        {
            var tcs = new TaskCompletionSource<bool>();
            var ct = new CancellationTokenSource(timeoutMsec); // State change timeout 10 sec.
            ct.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

            PropertyChangedEventHandler status_observer = (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "Status":
                        var current = (sender as CameraStatus).Status;
                        if (nextState == current)
                        {
                            DebugUtil.Log("Camera state changed to " + nextState + " successfully.");
                            tcs.TrySetResult(true);
                            return;
                        }
                        else if (EventParam.NotReady != current)
                        {
                            DebugUtil.Log("Unfortunately camera state changed to " + current);
                            tcs.TrySetResult(false);
                            return;
                        }
                        DebugUtil.Log("It might be in transitioning state...");
                        break;
                    default:
                        break;
                }
            };

            try
            {
                status.PropertyChanged += status_observer;
                await camera.SetCameraFunctionAsync(nextFunction);
                return await tcs.Task;
            }
            catch (RemoteApiException e)
            {
                if (e.code == StatusCode.IllegalState)
                {
                    return true;
                }
            }
            finally
            {
                status.PropertyChanged -= status_observer;
            }

            DebugUtil.Log("Failed to change camera state.");

            try
            {
                DebugUtil.Log("Check current state...");
                if (nextState == await camera.GetCameraFunctionAsync())
                {
                    DebugUtil.Log("Already in specified mode: " + nextState);
                    return true;
                }
            }
            catch (RemoteApiException)
            {
                DebugUtil.Log("Failed to get current state");
                return false;
            }

            DebugUtil.Log("Not in specified state...");
            return false;
        }

        public static async Task<string> PrepareMovieStreamingAsync(AvContentApiClient av, string contentUri)
        {
            var uri = await av.SetStreamingContentAsync(new PlaybackContent
            {
                Uri = contentUri,
                RemotePlayType = RemotePlayMode.SimpleStreaming
            }).ConfigureAwait(false);
            await av.StartStreamingAsync().ConfigureAwait(false);
            return uri.Url;
        }

        public static async Task PauseMovieStreamingAsync(AvContentApiClient av, MoviePlaybackData status)
        {
            await av.PauseStreamingAsync();
        }

        public static async Task StartMovieStreamingASync(AvContentApiClient av, MoviePlaybackData status)
        {
            await av.StartStreamingAsync();
        }

        public static async Task SeekMovieStreamingAsync(AvContentApiClient av, MoviePlaybackData status, TimeSpan seekTarget)
        {
            var originalStatus = status.StreamingStatus;

            if (status.StreamingStatus == StreamStatus.Error || status.StreamingStatus == StreamStatus.Invalid) { return; }

            if (status.StreamingStatus == StreamStatus.Started)
            {
                await PauseMovieStreamingAsync(av, status);
            }

            await av.SeekStreamingPositionAsync(new PlaybackPosition() { PositionMSec = (int)seekTarget.TotalMilliseconds });

            if (originalStatus == StreamStatus.Started || originalStatus == StreamStatus.PausedByEdge)
            {
                await StartMovieStreamingASync(av, status);
            }
        }
    }
}
