/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Diagnostics;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// A page that is displayed temporary outside of regular Wizard flow
    /// </summary>
    public class ConnectWizardPopupPage : ConnectWizardPage
    {
        #region Properties

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
                            try { Wizard.CurrentPopupPage = null; }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        });

                return _navigate_back;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _navigate_back;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public ConnectWizardPopupPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
