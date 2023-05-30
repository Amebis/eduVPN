/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Self-update wizard page
    /// </summary>
    public class SelfUpdateProgressPage : ConnectWizardPopupPage
    {
        #region Fields

        /// <summary>
        /// Self-update cancellation token
        /// </summary>
        private CancellationTokenSource SelfUpdateInProgress;

        /// <summary>
        /// Update file SHA-256 hash
        /// </summary>
        public byte[] Hash;

        /// <summary>
        /// List of update file download URIs
        /// </summary>
        /// <remarks>May contain absolute or relative to self-update-dicovery URIs.</remarks>
        public List<Uri> DownloadUris;

        /// <summary>
        /// Update file command line arguments
        /// </summary>
        public string Arguments;

        #endregion

        #region Properties

        /// <summary>
        /// Self-update progress value
        /// </summary>
        public Range<int> Progress { get; } = new Range<int>(0, 100);

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(
                        () =>
                        {
                            SelfUpdateInProgress?.Cancel();
                            if (base.NavigateBack.CanExecute())
                                base.NavigateBack.Execute();
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
        public SelfUpdateProgressPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void OnActivate()
        {
            SelfUpdateInProgress?.Cancel();

            base.OnActivate();

            // Setup self-update.
            Progress.Value = 0;
            SelfUpdateInProgress = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(SelfUpdateInProgress.Token, Window.Abort.Token).Token;
            new Thread(() =>
            {
                try
                {
                    CGo.DownloadAndInstallSelfUpdate(DownloadUris, Hash, Arguments, ct,
                        new CGo.SetProgress((float value) => Wizard.TryInvoke((Action)(() => Progress.Value = (int)Math.Floor(value * 100)))));

                    // Quit to release open files.
                    Wizard.TryInvoke((Action)(() => Wizard.OnQuitApplication(this)));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
            }).Start();
        }

        #endregion
    }
}
