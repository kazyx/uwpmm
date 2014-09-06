﻿using Kazyx.RemoteApi;
using Kazyx.Uwpmm.CameraControl;
using System.Collections.Generic;

namespace Kazyx.Uwpmm.DataModel
{
    public class ApiCapabilityContainer : ObservableBase
    {
        private readonly Dictionary<string, List<string>> _SupportedApis = new Dictionary<string, List<string>>();

        /// <summary>
        /// API name and versions
        /// </summary>
        public Dictionary<string, List<string>> SupportedApis
        {
            get { return _SupportedApis; }
        }

        public void AddSupported(MethodType method)
        {
            if (SupportedApis.ContainsKey(method.Name))
            {
                SupportedApis[method.Name].Add(method.Version);
            }
            else
            {
                var list = new List<string>();
                list.Add(method.Version);
                SupportedApis.Add(method.Name, list);
            }
            NotifyChanged("SupportedApis");
        }

        public bool IsSupported(string apiName)
        {
            return SupportedApis.ContainsKey(apiName);
        }

        public bool IsSupported(string apiName, string version)
        {
            return SupportedApis.ContainsKey(apiName) && SupportedApis[apiName].Contains(version);
        }

        private ServerVersion version = ServerVersion.CreateDefault();

        public ServerVersion Version
        {
            set
            {
                version = value;
                NotifyChanged("Version");
            }
            get
            {
                if (version == null)
                {
                    version = ServerVersion.CreateDefault();
                }
                return version;
            }
        }

        private readonly IEnumerable<string> RestrictedApiSet =
            new string[]{
                "actHalfPressShutter",
                "setExposureCompensation",
                "setTouchAFPosition",
                "setExposureMode",
                "setFNumber",
                "setShutterSpeed",
                "setIsoSpeedRate",
                "setWhiteBalance",
                "setStillSize",
                "setBeepMode",
                "setMovieQuality",
                "setViewAngle",
                "setSteadyMode",
                "setCurrentTime",
            };

        public bool IsRestrictedApi(string apiName)
        {
            foreach (var api in RestrictedApiSet)
            {
                if (apiName == api)
                {
                    return true;
                }
            }
            return false;
        }

        private List<string> _AvailableApis;
        public List<string> AvailableApis
        {
            set
            {
                _AvailableApis = value;
                if (value != null)
                    AvailableApiList = new List<string>(value);
                else
                    AvailableApiList = new List<string>();

                NotifyChanged("AvailableApis");
            }
            get { return _AvailableApis; }
        }

        private List<string> AvailableApiList = new List<string>();

        public bool IsAvailable(string apiName)
        {
            if (!Version.IsLiberated && IsRestrictedApi(apiName))
            {
                return false;
            }
            return AvailableApiList.Contains(apiName);
        }
    }
}
