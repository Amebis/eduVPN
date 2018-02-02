/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Connection status wizard page
    /// </summary>
    public class StatusPage : ConnectWizardPage
    {
        #region Properties

        /// <inheritdoc/>
        public override string Title
        {
            get { return Resources.Strings.StatusPageTitle; }
        }

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_navigate_back == null)
                    _navigate_back = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try { Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage; }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        });

                return _navigate_back;
            }
        }
        private DelegateCommand _navigate_back;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a status wizard page
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public StatusPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
