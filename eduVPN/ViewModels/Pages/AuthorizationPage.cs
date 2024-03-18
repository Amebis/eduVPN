/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System.Diagnostics;
using System.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Authorization wizard page
    /// </summary>
    public class AuthorizationPage : ConnectWizardStandardPage
    {
        #region Properties

        /// <summary>
        /// OAuth authorization URI
        /// </summary>
        public string Uri
        {
            get => _Uri;
            set {
                if (SetProperty(ref _Uri, value))
                    _CopyUri?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Uri;

        /// <summary>
        /// Copies OAuth authorization URI to the clipboard
        /// </summary>
        public DelegateCommand CopyUri
        {
            get
            {
                if (_CopyUri == null)
                    _CopyUri = new DelegateCommand(
                        () => Clipboard.SetDataObject(Uri),
                        () => !string.IsNullOrEmpty(Uri));
                return _CopyUri;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _CopyUri;

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
