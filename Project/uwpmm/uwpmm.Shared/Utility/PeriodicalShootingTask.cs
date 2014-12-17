﻿using Kazyx.Uwpmm.CameraControl;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Kazyx.Uwpmm.Utility
{
    public class PeriodicalShootingTask
    {
        private List<TargetDevice> TargetDevices = new List<TargetDevice>();
        private int Interval = 1;
        private DispatcherTimer Timer = new DispatcherTimer();
        private int SkipCount = 0;
        private const int SKIP_LIMIT = 5;
        private int Count = 0;

        public Action<ShootingResult> Tick;
        public Action<StopReason> Stopped;
        public Action<PeriodicalShootingStatus> StatusUpdated;

        public PeriodicalShootingTask(List<TargetDevice> Devices, int Interval)
        {
            this.TargetDevices = Devices;
            this.Interval = Interval;
        }

        public void Start()
        {
            if (TargetDevices == null || Interval < 0) { return; }

            RequestTakePicture();
            Timer.Interval = TimeSpan.FromSeconds(Interval);
            Timer.Tick += (sender, e) =>
            {
                RequestTakePicture();
            };
            Timer.Start();
        }

        public void Stop()
        {
            if (Stopped != null) { Stopped(StopReason.RequestedByUser); }
            this._Stop();
        }

        private void _Stop()
        {
            Timer.Stop();
        }

        public bool IsRunning { get { return Timer.IsEnabled; } }

        private async void RequestTakePicture()
        {
            bool isSkipped = false;

            foreach (var device in TargetDevices)
            {
                if (device == null || device.Status == null || device.Api == null) { continue; }
                if (device.Status.Status != RemoteApi.Camera.EventParam.Idle)
                {
                    isSkipped = true;
                    continue;
                }

                try
                {
                    await device.Api.Camera.ActTakePictureAsync();
                }
                catch (RemoteApi.RemoteApiException)
                {
                    this._Stop();
                    if (Stopped != null) { Stopped(StopReason.ShootingFailed); }
                }
            }

            if (isSkipped)
            {
                SkipCount++;

                if (SkipCount > SKIP_LIMIT)
                {
                    this._Stop();
                    if (Stopped != null) { Stopped(StopReason.SkipLimitExceeded); }
                }
                else
                {
                    if (Tick != null) { Tick(ShootingResult.Skipped); }
                }
            }
            else
            {
                if (Tick != null) { Tick(ShootingResult.Succeed); }
                Count++;
            }

            if (StatusUpdated != null)
            {
                StatusUpdated(new PeriodicalShootingStatus() { Interval = this.Interval, Count = this.Count });
            }
        }

        public enum ShootingResult
        {
            Succeed,
            Skipped,
        }

        public enum StopReason
        {
            SkipLimitExceeded,
            ShootingFailed,
            RequestedByUser,
        }
    }

    public class PeriodicalShootingStatus
    {
        public int Count { get; set; }
        public int Interval { get; set; }
    }
}
