/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN;
using System;
using System.Net;
using System.Windows;

namespace eduVPNClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static InstanceList Instances = new InstanceList();

        public App()
        {
            // .NET 3.5 allows Schannel to use SSL 3 and TLS 1.0 by default. Instead of hacking user computer's registry, extend it in runtime.
            // System.Net.SecurityProtocolType lacks appropriate constants prior to .NET 4.5.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x0C00;

            Instances.Load(
                new Uri(eduVPNClient.Properties.Settings.Default.InstanceDirectory),
                Convert.FromBase64String(eduVPNClient.Properties.Settings.Default.InstanceDirectoryPubKey));
        }
    }
}
