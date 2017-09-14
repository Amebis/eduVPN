/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Settings wizard page
    /// </summary>
    public class SettingsPage : ConnectWizardPage
    {
        #region Properties

        public ObservableCollection<Models.InterfaceInfo> InterfaceList
        {
            get { return _interface_list; }
        }
        private ObservableCollection<Models.InterfaceInfo> _interface_list;

        /// <summary>
        /// Selected interface
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InterfaceInfo SelectedInterface
        {
            get
            {
                // Find matching interface.
                foreach (var nic in _interface_list)
                    if (nic.DisplayName == Properties.Settings.Default.OpenVPNInterface)
                        return nic;

                // Return "Automatic".
                return _interface_list[0];
            }

            set
            {
                // When the page is navigated away the combo box resets selection (value is null). Ignore it.
                if (value != null)
                {
                    Properties.Settings.Default.OpenVPNInterface = value.Id != Guid.Empty ? value.DisplayName : null;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a settings wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public SettingsPage(ConnectWizard parent) :
            base(parent)
        {
            // Create available network interface list.
            _interface_list = new ObservableCollection<Models.InterfaceInfo>()
            {
                new Models.InterfaceInfo(Guid.Empty, Resources.Strings.InterfaceNameAutomatic)
            };
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var mac = nic.GetPhysicalAddress().GetAddressBytes();
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.Description.StartsWith("TAP-Windows Adapter V9") &&
                    mac.Length == 6 &&
                    mac[0] == 0x00 &&
                    mac[1] == 0xff)
                    _interface_list.Add(new Models.InterfaceInfo(Guid.TryParse(nic.Id, out var id) ? id : Guid.Empty, nic.Name));
            }
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.PreviousPage ?? Parent.StartingPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
