/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System.Diagnostics;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Authorization wizard page
    /// </summary>
    public class AuthorizationPage : ConnectWizardStandardPage
    {
        #region Properties

        /// <inheritdoc/>
        public DelegateCommand Cancel
        {
            get
            {
                if (_Cancel == null)
                    _Cancel = new DelegateCommand(() => Wizard.OperationInProgress?.Cancel());
                return _Cancel;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Cancel;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public AuthorizationPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
