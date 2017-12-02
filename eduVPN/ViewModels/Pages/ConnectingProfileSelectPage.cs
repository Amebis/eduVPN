/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Panels;
using eduVPN.ViewModels.Windows;
using System.ComponentModel;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Profile selection wizard page
    /// </summary>
    public class ConnectingProfileSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Profile select panel
        /// </summary>
        public ConnectingRefreshableProfileSelectPanel Panel
        {
            get
            {
                var source_index = (int)Parent.InstanceSourceType;
                if (_panels[source_index] == null)
                    _panels[source_index] = new ConnectingRefreshableProfileSelectPanel(Parent, Parent.InstanceSourceType);

                return _panels[source_index];
            }
        }
        private ConnectingRefreshableProfileSelectPanel[] _panels = new ConnectingRefreshableProfileSelectPanel[(int)InstanceSourceType._end];

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ConnectingProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
            Parent.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(Parent.InstanceSourceType))
                    RaisePropertyChanged(nameof(Panel));
            };
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            if (Parent.InstanceSource is LocalInstanceSource)
            {
                switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                {
                    case 0:
                    case 2:
                        if (Parent.InstanceSource.InstanceList.IndexOf(Parent.InstanceSource.AuthenticatingInstance) >= 0)
                            Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                        else
                            Parent.CurrentPage = Parent.CustomInstancePage;
                        break;

                    case 1:
                        Parent.CurrentPage = Parent.RecentConfigurationSelectPage;
                        break;
                }
            }
            else
                Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
