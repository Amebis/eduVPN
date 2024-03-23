/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Prompts user to make the self-update choice.
    /// </summary>
    public class SelfUpdatePromptPage : ConnectWizardPopupPage
    {
        #region Properties

        /// <summary>
        /// Mark not to re-prompt again
        /// </summary>
        public DelegateCommand SkipUpdate
        {
            get
            {
                if (_SkipUpdate == null)
                {
                    _SkipUpdate = new DelegateCommand(
                        () =>
                        {
                            Trace.TraceInformation("User choose to skip this update");
                            Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.MaxValue;
                            if (NavigateBack.CanExecute())
                                NavigateBack.Execute();
                        },
                        () => Wizard.AvailableVersion != null);
                    Wizard.PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(Wizard.AvailableVersion)) _SkipUpdate.RaiseCanExecuteChanged(); };
                }
                return _SkipUpdate;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _SkipUpdate;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelfUpdatePromptPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
