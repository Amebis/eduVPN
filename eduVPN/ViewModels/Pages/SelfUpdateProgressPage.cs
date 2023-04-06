/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            SelfUpdateInProgress = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(SelfUpdateInProgress.Token, Window.Abort.Token).Token;
            var selfUpdate = new BackgroundWorker() { WorkerReportsProgress = true };
            selfUpdate.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                selfUpdate.ReportProgress(0);
                CGo.DownloadAndInstallSelfUpdate(DownloadUris, Hash, Arguments, ct,
                    new CGo.SetProgress((float value) => selfUpdate.ReportProgress((int)Math.Floor(value * 100))));
            };

            // Self-update progress.
            selfUpdate.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                Progress.Value = e.ProgressPercentage;
            };

            // Self-update completition.
            selfUpdate.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (e.Error == null)
                {
                    // Self-updating successfuly launched. Quit to release open files.
                    Wizard.OnQuitApplication(this);
                }
                else if (!(e.Error is OperationCanceledException))
                    Wizard.Error = e.Error;

                // Self-dispose.
                (sender as BackgroundWorker)?.Dispose();
            };

            selfUpdate.RunWorkerAsync();
        }

        #endregion
    }
}
