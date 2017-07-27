/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Access type selection wizard page
    /// </summary>
    public class AccessTypePage : ConnectWizardPage
    {
        #region Fields

        /// <summary>
        /// Instance directory URI IDs as used in <c>Properties.Settings.Default</c> collection
        /// </summary>
        private static readonly string[] _instance_directory_id = new string[]
        {
            null, // AccessType.Unknown
            "SecureInternetDirectory", // AccessType.SecureInternet
            "InstituteAccessDirectory", // AccessType.InstituteAccess
        };

        /// <summary>
        /// Instance directory cache
        /// </summary>
        private Dictionary<string, object>[] _obj_cache;

        #endregion

        #region Properties

        /// <summary>
        /// List of available instances
        /// </summary>
        public Models.InstanceInfoList[] InstanceList
        {
            get { return _instance_list; }
            set {
                _instance_list = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)SetAccessType).RaiseCanExecuteChanged();
            }
        }
        private Models.InstanceInfoList[] _instance_list;

        /// <summary>
        /// Set access type
        /// </summary>
        public ICommand SetAccessType
        {
            get
            {
                if (_set_access_type == null)
                {
                    _set_access_type = new DelegateCommand<AccessType?>(
                        // execute
                        async param =>
                        {
                            Parent.AccessType = param.Value;
                            Parent.InstanceList = InstanceList[(int)Parent.AccessType];

                            if (Parent.InstanceList is Models.InstanceInfoFederatedList instance_list)
                            {
                                // Set authenticating instance.
                                Parent.AuthenticatingInstance = null;
                                Parent.AuthenticatingEndpoints = new Models.InstanceEndpoints()
                                {
                                    AuthorizationEndpoint = instance_list.AuthorizationEndpoint,
                                    TokenEndpoint = instance_list.TokenEndpoint
                                };

                                // Try to restore the access token from the settings.
                                Parent.AccessToken = null;
                                try
                                {
                                    var at = Properties.Settings.Default.AccessTokens[Parent.AuthenticatingEndpoints.AuthorizationEndpoint.AbsoluteUri];
                                    if (at != null)
                                        Parent.AccessToken = AccessToken.FromBase64String(at);
                                }
                                catch (Exception) { }

                                if (Parent.AccessToken != null && Parent.AccessToken.Expires.HasValue && Parent.AccessToken.Expires.Value <= DateTime.Now)
                                {
                                    // The access token expired. Try refreshing it.
                                    try
                                    {
                                        Parent.AccessToken = await Parent.AccessToken.RefreshTokenAsync(
                                            Parent.AuthenticatingEndpoints.TokenEndpoint,
                                            null,
                                            ConnectWizard.Abort.Token);
                                    }
                                    catch (Exception)
                                    {
                                        Parent.AccessToken = null;
                                    }
                                }

                                // Reset connecting instance.
                                Parent.ConnectingInstance = null;
                                Parent.ConnectingEndpoints = null;

                                if (Parent.AccessToken == null)
                                    Parent.CurrentPage = Parent.AuthorizationPage;
                                else
                                    Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
                            }
                            else
                            {
                                switch (param)
                                {
                                    case AccessType.SecureInternet: Parent.CurrentPage = Parent.SecureInternetSelectPage; break;
                                    case AccessType.InstituteAccess: Parent.CurrentPage = Parent.InstituteAccessSelectPage; break;
                                }
                            }
                        },

                        // canExecute
                        param =>
                        {
                            if (!param.HasValue) return false;
                            return InstanceList[(int)param.Value] != null;
                        });
                }
                return _set_access_type;
            }
        }
        private ICommand _set_access_type;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an access type selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public AccessTypePage(ConnectWizard parent) :
            base(parent)
        {
            _obj_cache = new Dictionary<string, object>[_instance_directory_id.Length];
            InstanceList = new Models.InstanceInfoList[_instance_directory_id.Length];
            for (var i = 0; i < _instance_directory_id.Length; i++)
            {
                if (_instance_directory_id[i] != null)
                {
                    try
                    {
                        // Get cached instance list JSON response from settings and parse it.
                        _obj_cache[i] = (Dictionary<string, object>)eduJSON.Parser.Parse(
                            ((JSON.Response)Properties.Settings.Default[_instance_directory_id[i] + "Cache"]).Value,
                            ConnectWizard.Abort.Token);

                        // Load instance list from cache.
                        var instance_list = Models.InstanceInfoList.FromJSON(_obj_cache[i]);
                        if (i == (int)AccessType.InstituteAccess)
                        {
                            // Append "Other instance" entry to institute access instance list.
                            instance_list.Add(new Models.InstanceInfo()
                            {
                                DisplayName = Resources.Strings.CustomInstance,
                                IsCustom = true,
                            });
                        }

                        InstanceList[i] = instance_list;
                        ((DelegateCommandBase)SetAccessType).RaiseCanExecuteChanged();
                    }
                    catch (Exception)
                    {
                        // Revert cache to default initial value.
                        Properties.Settings.Default[_instance_directory_id[i] + "Cache"] = null;
                        _obj_cache[i] = new Dictionary<string, object>()
                        {
                            { "instances", new List<object>() },
                            { "seq", 0 }
                        };
                    }
                }
            }
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            // Launch instance list load in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    try
                    {
                        var json_get_tasks = new Task<JSON.Response>[_instance_directory_id.Length];
                        for (var i = 0; i < _instance_directory_id.Length; i++)
                        {
                            if (_instance_directory_id[i] != null)
                            {
                                // Spawn instance list get.
                                json_get_tasks[i] = JSON.Response.GetAsync(
                                    new Uri((string)Properties.Settings.Default[_instance_directory_id[i]]),
                                    null,
                                    null,
                                    Convert.FromBase64String((string)Properties.Settings.Default[_instance_directory_id[i] + "PubKey"]),
                                    ConnectWizard.Abort.Token,
                                    (JSON.Response)Properties.Settings.Default[_instance_directory_id[i] + "Cache"]);
                            }
                        }

                        for (var i = 0; i < _instance_directory_id.Length; i++)
                        {
                            if (_instance_directory_id[i] != null)
                            {
                                try
                                {
                                    // Wait for the instance list get.
                                    JSON.Response response_cache = null;
                                    try
                                    {
                                        json_get_tasks[i].Wait(ConnectWizard.Abort.Token);
                                        response_cache = json_get_tasks[i].Result;
                                    }
                                    catch (AggregateException ex) { throw ex.InnerException; }

                                    if (response_cache.IsFresh)
                                    {
                                        // Parse instance list JSON.
                                        var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(
                                            response_cache.Value,
                                            ConnectWizard.Abort.Token);

                                        // Load instance list.
                                        var instance_list = Models.InstanceInfoList.FromJSON(obj);
                                        if (i == (int)AccessType.InstituteAccess)
                                        {
                                            // Append "Other instance" entry to institute access instance list.
                                            instance_list.Add(new Models.InstanceInfo()
                                            {
                                                DisplayName = Resources.Strings.CustomInstance,
                                                IsCustom = true,
                                            });
                                        }

                                        // Send the loaded instance list back to the UI thread.
                                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                        {
                                            InstanceList[i] = instance_list;
                                            ((DelegateCommandBase)SetAccessType).RaiseCanExecuteChanged();
                                        }));

                                        try
                                        {
                                            // If we got here, the loaded instance list is (probably) OK.
                                            bool update_cache = false;
                                            try { update_cache = eduJSON.Parser.GetValue<int>(obj, "seq") >= eduJSON.Parser.GetValue<int>(_obj_cache[i], "seq"); }
                                            catch (Exception) { update_cache = true; }
                                            if (update_cache)
                                            {
                                                // Update cache.
                                                Properties.Settings.Default[_instance_directory_id[i] + "Cache"] = response_cache;
                                                _obj_cache[i] = obj;
                                            }
                                        }
                                        catch (Exception) { }
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    // The load was aborted.
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    // Make it a clean start next time.
                                    Properties.Settings.Default[_instance_directory_id[i] + "Cache"] = null;

                                    // Notify the sender the instance list loading failed.
                                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Notify the sender the instance list loading failed.
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                })).Start();
        }

        #endregion
    }
}
