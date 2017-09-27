/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

//using NETWORKLIST;
using System;
using System.Collections.ObjectModel;
//using System.Linq;
using System.Net.NetworkInformation;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Settings wizard page
    /// </summary>
    public class SettingsPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// List of installed TAP network interfaces
        /// </summary>
        public ObservableCollection<Models.InterfaceInfo> InterfaceList
        {
            get { return _interface_list; }
        }
        private ObservableCollection<Models.InterfaceInfo> _interface_list;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a settings wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public SettingsPage(ConnectWizard parent) :
            base(parent)
        {
            //var network_connections = new NetworkListManager().GetNetworkConnections().Cast<INetworkConnection>();
            //ObservableCollection<Guid> networks = new ObservableCollection<Guid>();

            // Create available network interface list.
            _interface_list = new ObservableCollection<Models.InterfaceInfo>()
            {
                new Models.InterfaceInfo(Guid.Empty, Resources.Strings.InterfaceNameAutomatic)
            };
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in interfaces)
            {
                Guid.TryParse(nic.Id, out var nic_id);
                var mac = nic.GetPhysicalAddress().GetAddressBytes();
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.Description.StartsWith("TAP-Windows Adapter V9") &&
                    mac.Length == 6 &&
                    mac[0] == 0x00 &&
                    mac[1] == 0xff)
                    _interface_list.Add(new Models.InterfaceInfo(nic_id, nic.Name));

                //if (nic.OperationalStatus == OperationalStatus.Up &&
                //    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                //    nic.GetIPProperties()?.GatewayAddresses.Select(g => g?.Address).Where(a => a != null).Count() > 0)
                //{
                //    try
                //    {
                //        var net_conn = network_connections.Where(c => c.GetAdapterId() == nic_id).FirstOrDefault();
                //        var net_id = net_conn.GetNetwork().GetNetworkId();
                //        if (net_conn != null && !networks.Where(n => n == net_id).Any())
                //            networks.Add(net_id);
                //    }
                //    catch { }
                //}
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.PreviousPage ?? Parent.StartingPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
