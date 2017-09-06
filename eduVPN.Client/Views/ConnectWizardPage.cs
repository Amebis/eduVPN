/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.ComponentModel;
using System.Windows.Controls;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for connection wizard pages
    /// </summary>
    public class ConnectWizardPage : Page, INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Brief text describing page intent
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { if (value != _description) { _description = value; OnPropertyChanged("Description"); } }
        }
        private string _description;

        #endregion

        #region INotifyPropertyChanged Support
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
