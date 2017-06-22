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

        /// <summary>
        /// Cached instance list
        /// </summary>
        private Dictionary<string, object> _instance_list_cache;

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
            try
            {
                // Restore instance list cache.
                _instance_list_cache = (Dictionary<string, object>)eduJSON.Parser.Parse(Properties.Settings.Default.InstanceListCache);
            }
            catch (Exception)
            {
                // Revert cache to default initial value.
                _instance_list_cache = new Dictionary<string, object>
                {
                    { "instances", new List<object>() },
                    { "seq", 0 }
                };
            }

            // Initialize instance list.
            InstanceList = new InstanceList();
            InstanceList.Load(_instance_list_cache);
            InstanceList.Add(new Instance()
                {
                    Base = new Uri("nl.eduvpn.app.windows:other"),
                    DisplayName = Properties.Resources.OtherInstance
                });

            // Save UI thread's dispatcher.
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Launch instance list load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetInstanceList));

            _dispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Raise the abort flag to gracefully shutdown all background threads.
                _abort.Cancel();

                // Persist settings (instance cache) to disk.
                Properties.Settings.Default.Save();
            };
        }

        #endregion

        #region InstanceList Background Load

        /// <summary>
        /// Loads instance list from web service.
        /// </summary>
        private void GetInstanceList(object param)
        {
            string json = null;
            Dictionary<string, object> obj = null;

            try
            {
                // Get instance list.
                json = InstanceList.Get(
                    new Uri(Properties.Settings.Default.InstanceDirectory),
                    Convert.FromBase64String(Properties.Settings.Default.InstanceDirectoryPubKey),
                    _abort.Token);

                // Parse instance list.
                obj = (Dictionary<string, object>)eduJSON.Parser.Parse(json, _abort.Token);

                // Load instance list.
                var instance_list = new InstanceList();
                instance_list.Load(obj);

                // Append "Other instance" entry.
                instance_list.Add(new Instance()
                {
                    Base = new Uri("nl.eduvpn.app.windows:other"),
                    DisplayName = Properties.Resources.OtherInstance
                });

                // Send the loaded instance list back to the UI thread.
                _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InstanceList = instance_list));
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

            // If we got here, the loaded instance list is (probably) OK.
            // It is only now that we are confident enough to cache it.
            bool update_cache = false;
            try { update_cache = eduJSON.Parser.GetValue<int>(obj, "seq") >= eduJSON.Parser.GetValue<int>(_instance_list_cache, "seq"); }
            catch (Exception) { update_cache = true; }
            if (update_cache)
            {
                // Update cache.
                _instance_list_cache = obj;
                Properties.Settings.Default.InstanceListCache = json;
            }
        }

        #endregion
    }
}
