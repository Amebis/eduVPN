/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    public class StatusPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Client connection state
        /// </summary>
        public Models.StatusType State
        {
            get { return _state; }
            set { if (value != _state) { _state = value; RaisePropertyChanged(); } }
        }
        private Models.StatusType _state;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a status wizard page
        /// </summary>
        /// <param name="parent"></param>
        public StatusPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            State = Models.StatusType.Initializing;

            // Launch VPN connecting task in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                param =>
                {
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                    try
                    {
                        // Wait for two seconds, then switch to connecting state.
                        if (_abort.Token.WaitHandle.WaitOne(1000 * 2)) return;
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connecting));

                        // Wait for three seconds, then switch to connected state.
                        if (_abort.Token.WaitHandle.WaitOne(1000 * 3)) return;
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connected));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Notify the sender the profile list loading failed.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                    finally
                    {
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                    }
                }));
        }

        #endregion
    }
}
