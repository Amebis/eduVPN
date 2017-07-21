/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance selection wizard page
    /// </summary>
    public class InstanceSelectPage : ConnectWizardPage
    {
        #region Fields

        /// <summary>
        /// Cached instance list
        /// </summary>
        private Dictionary<string, object> _instance_list_cache;

        #endregion

        #region Properties

        /// <summary>
        /// List of available instances
        /// </summary>
        public JSON.InstanceList InstanceList
        {
            get { return _instance_list; }
            set { _instance_list = value; RaisePropertyChanged(); }
        }
        private JSON.InstanceList _instance_list;

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public JSON.Instance SelectedInstance
        {
            get { return _selected_instance; }
            set
            {
                _selected_instance = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)AuthorizeSelectedInstance).RaiseCanExecuteChanged();
            }
        }
        private JSON.Instance _selected_instance;

        /// <summary>
        /// Authorize selected instance command
        /// </summary>
        public ICommand AuthorizeSelectedInstance
        {
            get
            {
                if (_authorize_instance == null)
                {
                    _authorize_instance = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.Instance = SelectedInstance;
                            if (SelectedInstance.IsCustom)
                                Parent.CurrentPage = Parent.CustomInstancePage;
                            else
                                Parent.CurrentPage = Parent.AuthorizationPage;
                        },

                        // canExecute
                        () => SelectedInstance != null);
                }
                return _authorize_instance;
            }
        }
        private ICommand _authorize_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceSelectPage(ConnectWizard parent) :
            base(parent)
        {
            _dispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Persist settings (instance cache) to disk.
                Properties.Settings.Default.Save();
            };

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
            InstanceList = new JSON.InstanceList();
            InstanceList.Load(_instance_list_cache);
            InstanceList.Add(new JSON.Instance()
            {
                DisplayName = Resources.Strings.CustomInstance,
                IsCustom = true,
            });

            // Launch instance list load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(InstanceListLoader));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads instance list from web service.
        /// </summary>
        private void InstanceListLoader(object param)
        {
            JSON.Response json = null;

            for (;;)
            {
                try
                {
                    // Set busy flag (in the UI thread).
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = true));

                    try
                    {
                        // Get instance list.
                        json = JSON.Response.Get(
                            new Uri(Properties.Settings.Default.InstanceDirectory),
                            null,
                            null,
                            Convert.FromBase64String(Properties.Settings.Default.InstanceDirectoryPubKey),
                            _abort.Token,
                            json);

                        if (json.IsFresh)
                        {
                            // Parse instance list.
                            var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(json.Value, _abort.Token);

                            // Load instance list.
                            var instance_list = new JSON.InstanceList();
                            instance_list.Load(obj);

                            // Append "Other instance" entry.
                            instance_list.Add(new JSON.Instance()
                            {
                                DisplayName = Resources.Strings.CustomInstance,
                                IsCustom = true,
                            });

                            // Send the loaded instance list back to the UI thread.
                            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                InstanceList = instance_list;
                                ErrorMessage = null;
                            }));

                            try
                            {
                                // If we got here, the loaded instance list is (probably) OK.
                                bool update_cache = false;
                                try { update_cache = eduJSON.Parser.GetValue<int>(obj, "seq") >= eduJSON.Parser.GetValue<int>(_instance_list_cache, "seq"); }
                                catch (Exception) { update_cache = true; }
                                if (update_cache)
                                {
                                    // Update cache.
                                    _instance_list_cache = obj;
                                    Properties.Settings.Default.InstanceListCache = json.Value;
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    finally
                    {
                        // Clear busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = false));
                    }

                    // Wait for five minutes.
                    if (_abort.Token.WaitHandle.WaitOne(1000 * 60 * 5))
                        break;
                }
                catch (OperationCanceledException)
                {
                    // The load was aborted.
                    break;
                }
                catch (Exception ex)
                {
                    // Notify the sender the instance list loading failed.
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));

                    // Wait for ten seconds.
                    if (_abort.Token.WaitHandle.WaitOne(1000 * 10))
                        break;

                    // Make it a clean start.
                    json = null;
                    continue;
                }
            }
        }

        protected override void DoNavigateBack()
        {
            Parent.CurrentPage = Parent.AccessTypePage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        public override void OnActivate()
        {
            // Reset selected instance, to prevent automatic continuation to
            // CustomInstance/Authorization page.
            SelectedInstance = null;

            base.OnActivate();
        }

        #endregion
    }
}
