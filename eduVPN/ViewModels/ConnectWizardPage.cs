/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Base class for all wizard pages
    /// </summary>
    public class ConnectWizardPage : BindableBase
    {
        #region Fields

        /// <summary>
        /// UI thread's dispatcher
        /// </summary>
        /// <remarks>
        /// Background threads must raise property change events in the UI thread.
        /// </remarks>
        protected Dispatcher _dispatcher;

        /// <summary>
        /// Token used to abort unfinished background processes in case of application shutdown.
        /// </summary>
        protected static CancellationTokenSource _abort = new CancellationTokenSource();

        #endregion

        #region Properties

        /// <summary>
        /// The page parent
        /// </summary>
        public ConnectWizard Parent { get; }

        /// <summary>
        /// Navigate back
        /// </summary>
        public ICommand NavigateBack
        {
            get
            {
                if (_navigate_back == null)
                    _navigate_back = new DelegateCommand(DoNavigateBack, CanNavigateBack);
                return _navigate_back;
            }
        }
        private ICommand _navigate_back;

        /// <summary>
        /// The page error message; <c>null</c> when no error condition.
        /// </summary>
        public string ErrorMessage
        {
            get { return _error_message; }
            set { _error_message = value; RaisePropertyChanged(); }
        }
        private string _error_message;

        /// <summary>
        /// Is wizard page performing background tasks?
        /// </summary>
        public bool IsBusy
        {
            get { return _is_busy; }
            set { if (value != _is_busy) { _is_busy = value; RaisePropertyChanged(); } }
        }
        private bool _is_busy;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ConnectWizardPage(ConnectWizard parent)
        {
            Parent = parent;

            // Save UI thread's dispatcher.
            _dispatcher = Dispatcher.CurrentDispatcher;

            _dispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Raise the abort flag to gracefully shutdown all background threads.
                _abort.Cancel();
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when NavigateBack command is invoked.
        /// </summary>
        protected virtual void DoNavigateBack()
        {
        }

        /// <summary>
        /// Called to test if NavigateBack command is enabled.
        /// </summary>
        /// <returns><c>true</c> if enabled; <c>false</c> otherwise</returns>
        protected virtual bool CanNavigateBack()
        {
            return false;
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public virtual void OnActivate()
        {
        }

        #endregion
    }
}
