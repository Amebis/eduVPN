/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
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
        public static CancellationTokenSource Abort { get => _abort; }
        private static CancellationTokenSource _abort = new CancellationTokenSource();

        /// <summary>
        /// The page error; <c>null</c> when no error condition.
        /// </summary>
        public Exception Error
        {
            get { return _error; }
            set { _error = value; RaisePropertyChanged(); }
        }
        private Exception _error;

        /// <summary>
        /// Is window performing background tasks?
        /// </summary>
        public bool IsBusy
        {
            get { return _task_count > 0; }
        }

        /// <summary>
        /// Number of background tasks the window is performing
        /// </summary>
        public int TaskCount
        {
            get { lock (_task_count_lock) return _task_count; }
        }
        private int _task_count;
        private object _task_count_lock = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a window
        /// </summary>
        public Window()
        {
            // Save UI thread's dispatcher.
            Dispatcher = Dispatcher.CurrentDispatcher;

            Dispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Raise the abort flag to gracefully shutdown all background threads.
                Abort.Cancel();
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Increments or decrements number of background tasks the window is performing
        /// </summary>
        /// <param name="increment">Positive to increment, negative to decrement</param>
        public void ChangeTaskCount(int increment)
        {
            lock (_task_count_lock)
                _task_count += increment;

            RaisePropertyChanged("TaskCount");
            RaisePropertyChanged("IsBusy");
        }

        #endregion
    }
}
