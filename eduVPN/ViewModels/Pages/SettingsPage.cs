/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

//using NETWORKLIST;
using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System;
//using System.Linq;
using System.Net.NetworkInformation;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Settings wizard page
    /// </summary>
    public class SettingsPage : ConnectWizardPopupPage
    {
        #region Properties

        /// <summary>
        /// List of installed TAP network interfaces
        /// </summary>
        public ObservableCollectionEx<eduVPN.Models.NetworkInterface> InterfaceList { get; } = new ObservableCollectionEx<eduVPN.Models.NetworkInterface>();

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a settings wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SettingsPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void OnActivate()
        {
            base.OnActivate();

            //var network_connections = new NetworkListManager().GetNetworkConnections().Cast<INetworkConnection>();
            //ObservableCollection<Guid> networks = new ObservableCollection<Guid>();

            // Create available network interface list.
            var list = InterfaceList.BeginUpdate();
            try
            {
                list.Clear();
                list.Add(new eduVPN.Models.NetworkInterface(Guid.Empty, Resources.Strings.InterfaceNameAutomatic));
                foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    Guid.TryParse(nic.Id, out var nic_id);
                    var mac = nic.GetPhysicalAddress().GetAddressBytes();
                    if (nic.NetworkInterfaceType == (NetworkInterfaceType)53 &&
                        nic.Description.StartsWith("TAP-Windows Adapter V9") &&
                        mac.Length == 6 &&
                        mac[0] == 0x00 &&
                        mac[1] == 0xff)
                        list.Add(new eduVPN.Models.NetworkInterface(nic_id, nic.Name));

                    //if (nic.OperationalStatus == OperationalStatus.Up &&
                    //    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    //    nic.GetIPProperties()?.GatewayAddresses.Select(g => g?.Address).Where(a => a != null).Count() > 0)
                    //{
                    //    try
                    //    {
                    //        var net_conn = network_connections.FirstOrDefault(c => c.GetAdapterId() == nic_id);
                    //        var net_id = net_conn.GetNetwork().GetNetworkId();
                    //        if (net_conn != null && !networks.Where(n => n == net_id).Any())
                    //            networks.Add(net_id);
                    //    }
                    //    catch { }
                    //}
                }
            }
            finally { InterfaceList.EndUpdate(); }
        }

        #endregion
    }
}
