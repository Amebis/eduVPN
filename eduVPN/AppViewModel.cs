/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN
{
    public class AppViewModel : BindableBase
    {
        #region Fields

        /// <summary>
        /// UI thread's dispatcher
        /// </summary>
        private Dispatcher _dispatcher;

        /// <summary>
        /// Token used to abort unfinished background processes in case of application shutdown.
        /// </summary>
        private static CancellationTokenSource _abort = new CancellationTokenSource();

        #endregion

        #region Properties

        /// <summary>
        /// The list of available instances
        /// </summary>
        public InstanceList InstanceList
        {
            get { return _instance_list; }
            set { _instance_list = value; RaisePropertyChanged(); }
        }
        private InstanceList _instance_list;

        /// <summary>
        /// The error message of the instance (re)load attempt; <c>null</c> when no error condition.
        /// </summary>
        public string InstanceListErrorMessage
        {
            get { return _instance_list_error_message; }
            set { _instance_list_error_message = value; RaisePropertyChanged(); }
        }
        private string _instance_list_error_message;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public AppViewModel()
        {
            // Initialization
            InstanceList = new InstanceList()
            {
                new Instance()
                {
                    Base = new Uri("nl.eduvpn.app.windows:other"),
                    DisplayName = Properties.Resources.OtherInstance
                }
            };

            // Save UI thread's dispatcher.
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Launch instance list load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetInstanceList));

            _dispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Raise the abort flag to gracefully shutdown all background threads.
                _abort.Cancel();
            };
        }

        #endregion

        #region InstanceList Background Load

        /// <summary>
        /// Loads instance list from web service.
        /// </summary>
        private void GetInstanceList(object param)
        {
            try
            {
                // Load instance list.
                var obj = InstanceList.Get(
                    new Uri(Properties.Settings.Default.InstanceDirectory),
                    Convert.FromBase64String(Properties.Settings.Default.InstanceDirectoryPubKey),
                    _abort.Token);

                // Send the loaded instance list back to the UI thread.
                _dispatcher.Invoke(DispatcherPriority.Normal,
                    (Action)(() =>
                    {
                        // Append "Other instance" entry to JSON data.
                        eduJSON.Parser.GetValue<List<object>>(obj, "instances").Add(new Dictionary<string, object>
                            {
                                { "base_uri", "nl.eduvpn.app.windows:other" },
                                { "display_name", Properties.Resources.OtherInstance }
                            });

                        // Load instances.
                        InstanceList.Load(obj);
                    }));

                return;
            }
            catch (OperationCanceledException)
            {
                // The load was aborted.
                return;
            }
            catch (Exception ex)
            {
                // Notify the sender the instance list loading failed.
                _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InstanceListErrorMessage = String.Format(Properties.Resources.ErrorInstanceList, ex.Message)));
            }
        }

        #endregion
    }
}
