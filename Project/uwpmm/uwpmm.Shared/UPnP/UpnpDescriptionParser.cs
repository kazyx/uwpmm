﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Kazyx.Uwpmm.UPnP
{
    public class UpnpDescriptionParser
    {
        private const string NS_UPNP = "{urn:schemas-upnp-org:device-1-0}";

        public static UpnpDevice ParseDescription(XDocument xml, Uri location)
        {
            var rootAddress = ConvertLocationToRootAddress(location);

            var device = xml.Root.Element(NS_UPNP + "device");
            var services = device.Element(NS_UPNP + "serviceList");
            var udn = device.Element(NS_UPNP + "UDN").Value;
            var fn = device.Element(NS_UPNP + "friendlyName").Value;
            var mn = device.Element(NS_UPNP + "modelName").Value;

            var result = new Dictionary<string, UpnpService>();
            foreach (var service in services.Elements())
            {
                var key = service.Element(NS_UPNP + "serviceType").Value;
                result.Add(key, new UpnpService
                {
                    RootAddress = rootAddress,
                    ServiceType = key,
                    ServiceId = service.Element(NS_UPNP + "serviceId").Value,
                    ScpdUrl = service.Element(NS_UPNP + "SCPDURL").Value,
                    ControlUrl = service.Element(NS_UPNP + "controlURL").Value,
                    EventSubUrl = service.Element(NS_UPNP + "eventSubURL").Value,
                });
            }

            return new UpnpDevice
            {
                RootAddress = rootAddress,
                FriendlyName = fn,
                ModelName = mn,
                UDN = udn,
                Services = result
            };
        }

        private static string ConvertLocationToRootAddress(Uri location)
        {
            return "http://" + location.Host + ":" + location.Port;
        }
    }
}
