/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for connection wizard pages
    /// </summary>
    public class ConnectWizardPage : Page
    {
        #region Properties

        /// <summary>
        /// Brief text describing page intent
        /// </summary>
        public string Description
        {
            get { return GetValue(DescriptionProperty) as string; }
            set { SetValue(DescriptionProperty, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ConnectWizardPage), null);

        #endregion
    }
}
