/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(
                        () =>
                        {
                            try { Wizard.CurrentPopupPage = null; }
                            catch (Exception ex) { Wizard.Error = ex; }
                        });
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _NavigateBack;

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
