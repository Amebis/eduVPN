/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPNClient
{
    /// <summary>
    /// Interaction logic for InstanceSelectionPage.xaml
    /// </summary>
    public partial class InstanceSelectionPage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public InstanceSelectionPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Handlers

        private void InstanceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // User selected an instance.
            var viewmodel = (InstanceViewModel)DataContext;

            if (viewmodel.SelectedInstance.Base == new Uri("nl.eduvpn.app.windows:other"))
            {
                // User selected "Other instance".
                NavigationCommands.GoToPage.Execute("OtherInstancePage.xaml", this);
            }
            else
            {
                // A known instance was selected. Proceed to authentication.
                if (viewmodel.AuthorizeSelectedInstanceCommand.CanExecute(null))
                    viewmodel.AuthorizeSelectedInstanceCommand.Execute(null);
            }
        }

        #endregion
    }
}
