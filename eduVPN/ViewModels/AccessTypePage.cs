/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            "SecureInternetDirectory",
            "InstituteAccessDirectory",
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
        public ObservableCollection<Models.InstanceGroupInfo> InstanceGroups
        {
            get { return _instance_groups; }
            set {
                _instance_groups = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)SetAccessType).RaiseCanExecuteChanged();
            }
        }
        private ObservableCollection<Models.InstanceGroupInfo> _instance_groups;

        /// <summary>
        /// Set access type
        /// </summary>
        public ICommand SetAccessType
        {
            get
            {
                if (_set_access_type == null)
                {
                    _set_access_type = new DelegateCommand<Models.InstanceGroupInfo>(
                        // execute
                        async param =>
                        {
                            Error = null;
                            TaskCount++;
                            try
                            {
                                Parent.InstanceGroup = param;

                                if (Parent.InstanceGroup is Models.FederatedInstanceGroupInfo instance_group)
                                {
                                    // Set authenticating instance.
                                    Parent.AuthenticatingInstance = new Models.InstanceInfo(instance_group);

                                    // Restore the access token from the settings.
                                    Parent.AccessToken = await Parent.AuthenticatingInstance.GetAccessTokenAsync(ConnectWizard.Abort.Token);

                                    // Reset connecting instance.
                                    Parent.ConnectingInstance = null;

                                    if (Parent.AccessToken == null)
                                        Parent.CurrentPage = Parent.AuthorizationPage;
                                    else
                                        Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
                                }
                                else
                                    Parent.CurrentPage = Parent.InstanceSelectPage;
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { TaskCount--; }
                        },

                        // canExecute
                        param => param != null);
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
            InstanceGroups = new ObservableCollection<Models.InstanceGroupInfo>();
            for (var i = 0; i < _instance_directory_id.Length; i++)
            {
                Models.InstanceGroupInfo instance_group = null;
                try
                {
                    // Get cached instance group JSON response from settings and parse it.
                    _obj_cache[i] = (Dictionary<string, object>)eduJSON.Parser.Parse(
                        ((JSON.Response)Properties.Settings.Default[_instance_directory_id[i] + "Cache"]).Value,
                        ConnectWizard.Abort.Token);

                    // Load instance group from cache.
                    instance_group = Models.InstanceGroupInfo.FromJSON(_obj_cache[i]);
                    if (instance_group is Models.LocalInstanceGroupInfo)
                    {
                        // Append "Other instance" entry to institute access instance group.
                        instance_group.Add(new Models.InstanceInfo()
                        {
                            DisplayName = Resources.Strings.CustomInstance,
                            IsCustom = true,
                        });
                    }
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

                    instance_group = null;
                }

                InstanceGroups.Add(instance_group);
                ((DelegateCommandBase)SetAccessType).RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            // Launch instance group load in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = null));
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));
                    try
                    {
                        var json_get_tasks = new Task<JSON.Response>[_instance_directory_id.Length];
                        for (var i = 0; i < _instance_directory_id.Length; i++)
                        {
                            // Spawn instance group get.
                            json_get_tasks[i] = JSON.Response.GetAsync(
                                uri: new Uri((string)Properties.Settings.Default[_instance_directory_id[i]]),
                                pub_key: Convert.FromBase64String((string)Properties.Settings.Default[_instance_directory_id[i] + "PubKey"]),
                                ct: ConnectWizard.Abort.Token,
                                previous: (JSON.Response)Properties.Settings.Default[_instance_directory_id[i] + "Cache"]);
                        }

                        for (var i = 0; i < _instance_directory_id.Length; i++)
                        {
                            try
                            {
                                // Wait for the instance group get.
                                JSON.Response response_cache = null;
                                try
                                {
                                    json_get_tasks[i].Wait(ConnectWizard.Abort.Token);
                                    response_cache = json_get_tasks[i].Result;
                                }
                                catch (AggregateException ex) { throw ex.InnerException; }

                                if (response_cache.IsFresh)
                                {
                                    // Parse instance group JSON.
                                    var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(
                                        response_cache.Value,
                                        ConnectWizard.Abort.Token);

                                    // Load instance group.
                                    var instance_group = Models.InstanceGroupInfo.FromJSON(obj);
                                    if (instance_group is Models.LocalInstanceGroupInfo)
                                    {
                                        // Append "Other instance" entry to institute access instance group.
                                        instance_group.Add(new Models.InstanceInfo()
                                        {
                                            DisplayName = Resources.Strings.CustomInstance,
                                            IsCustom = true,
                                        });
                                    }

                                    // Send the loaded instance group back to the UI thread.
                                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                        () =>
                                        {
                                            InstanceGroups[i] = instance_group;
                                            ((DelegateCommandBase)SetAccessType).RaiseCanExecuteChanged();
                                        }));

                                    try
                                    {
                                        // If we got here, the loaded instance group is (probably) OK.
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
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                // Make it a clean start next time.
                                Properties.Settings.Default[_instance_directory_id[i] + "Cache"] = null;

                                // Notify the sender the instance group loading failed. However, continue with other lists.
                                // This will overwrite all previous error messages.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = new AggregateException(String.Format(Resources.Strings.ErrorInstanceGroupInfoLoad, _instance_directory_id[i]), ex)));
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = ex)); }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--)); }
                })).Start();
        }

        #endregion
    }
}
