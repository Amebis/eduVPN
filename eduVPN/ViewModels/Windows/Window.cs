/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// Base class for eduVPN window view-models
    /// </summary>
    public class Window : BindableBase
    {
        #region Properties

        /// <summary>
        /// UI thread's dispatcher
        /// </summary>
        /// <remarks>
        /// Background threads must raise property change events in the UI thread.
        /// </remarks>
        public Dispatcher Dispatcher { get; }

        /// <summary>
        /// Token used to abort unfinished background processes in case of application shutdown.
        /// </summary>
        public static CancellationTokenSource Abort { get; } = new CancellationTokenSource();

        /// <summary>
        /// The page error; <c>null</c> when no error condition.
        /// </summary>
        public Exception Error
        {
            get { return _Error; }
            set
            {
                if (SetProperty(ref _Error, value))
                {
                    _DismissError?.RaiseCanExecuteChanged();
                    _CopyError?.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Exception _Error;

        /// <summary>
        /// Is window performing background tasks?
        /// </summary>
        public bool IsBusy
        {
            get { return _TaskCount > 0; }
        }

        /// <summary>
        /// Number of background tasks the window is performing
        /// </summary>
        public int TaskCount
        {
            get { return _TaskCount; }
            set
            {
                var wasBusy = IsBusy;
                if (SetProperty(ref _TaskCount, value) && wasBusy != IsBusy)
                    RaisePropertyChanged(nameof(IsBusy));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _TaskCount;

        /// <summary>
        /// Clears current error information
        /// </summary>
        public DelegateCommand DismissError
        {
            get
            {
                if (_DismissError == null)
                    _DismissError = new DelegateCommand(
                        () => Error = null,
                        () => Error != null);
                return _DismissError;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _DismissError;

        /// <summary>
        /// Copies current error information to the clipboard
        /// </summary>
        public DelegateCommand CopyError
        {
            get
            {
                if (_CopyError == null)
                    _CopyError = new DelegateCommand(
                        () =>
                        {
                            try { Clipboard.SetDataObject(Error.ToString()); }
                            catch (Exception ex) { Error = ex; }
                        },
                        () => Error != null);
                return _CopyError;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _CopyError;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a window
        /// </summary>
        public Window()
        {
            // Save UI thread's dispatcher.
            Dispatcher = Dispatcher.CurrentDispatcher;

            Dispatcher.ShutdownStarted += (object sender, EventArgs e) =>
            {
                // Raise the abort flag to gracefully shutdown all background threads.
                Abort.Cancel();
            };
        }

        #endregion
    }
}
