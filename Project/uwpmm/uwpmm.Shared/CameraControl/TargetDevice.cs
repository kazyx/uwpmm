﻿using Kazyx.DeviceDiscovery;
using Kazyx.Uwpmm.DataModel;

namespace Kazyx.Uwpmm.CameraControl
{
    public class TargetDevice
    {
        public TargetDevice(SonyCameraDeviceInfo info)
        {
            Udn = info.UDN;
            DeviceName = info.ModelName;
            FriendlyName = info.FriendlyName;
            _Api = new DeviceApiHolder(info);
            _Status = new CameraStatus();
            _Observer = new StatusObserver(this);
        }

        public string DeviceName { private set; get; }

        public string FriendlyName { private set; get; }

        public string Udn { private set; get; }

        private readonly DeviceApiHolder _Api;
        public DeviceApiHolder Api
        {
            get { return _Api; }
        }

        private StatusObserver _Observer;
        public StatusObserver Observer
        {
            get { return _Observer; }
        }

        private CameraStatus _Status;
        public CameraStatus Status
        {
            get { return _Status; }
        }

        public bool StorageAccessSupported
        {
            get { return Api.AvContent != null; }
        }
    }
}
