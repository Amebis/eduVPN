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
    /// Instance group selection wizard page
    /// </summary>
    public class InstanceGroupSelectPage : ConnectWizardPage
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
                ((DelegateCommandBase)SelectInstanceGroup).RaiseCanExecuteChanged();
            }
        }
        private ObservableCollection<Models.InstanceGroupInfo> _instance_groups;

        /// <summary>
        /// Select instance group
        /// </summary>
        public ICommand SelectInstanceGroup
        {
            get
            {
                if (_select_instance_group == null)
                {
                    _select_instance_group = new DelegateCommand<Models.InstanceGroupInfo>(
                        // execute
                        async param =>
                        {
                            Parent.Error = null;
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceGroup = param;

                                if (Parent.InstanceGroup is Models.FederatedInstanceGroupInfo instance_group)
                                {
                                    // Set authenticating instance.
                                    Parent.Configuration.AuthenticatingInstance = new Models.InstanceInfo(instance_group);

                                    // Restore the access token from the settings.
                                    Parent.Configuration.AccessToken = await Parent.Configuration.AuthenticatingInstance.GetAccessTokenAsync(ConnectWizard.Abort.Token);

                                    // Reset connecting instance.
                                    Parent.Configuration.ConnectingInstance = null;

                                    if (Parent.Configuration.AccessToken == null)
                                        Parent.CurrentPage = Parent.AuthorizationPage;
                                    else
                                        Parent.CurrentPage = Parent.ProfileSelectPage;
                                }
                                else
                                    Parent.CurrentPage = Parent.InstanceSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        param => param != null);
                }
                return _select_instance_group;
            }
        }
        private ICommand _select_instance_group;

        /// <summary>
        /// Select custom instance group
        /// </summary>
        public ICommand SelectCustomInstanceGroup
        {
            get
            {
                if (_select_custom_instance_group == null)
                {
                    _select_custom_instance_group = new DelegateCommand<Models.InstanceGroupInfo>(
                        // execute
                        param =>
                        {
                            Parent.Error = null;
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceGroup = null;
                                Parent.CurrentPage = Parent.CustomInstanceGroupPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        param => true);
                }
                return _select_custom_instance_group;
            }
        }
        private ICommand _select_custom_instance_group;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance group selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceGroupSelectPage(ConnectWizard parent) :
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
                ((DelegateCommandBase)SelectInstanceGroup).RaiseCanExecuteChanged();
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
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = null));
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
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

                                    // Send the loaded instance group back to the UI thread.
                                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                        () =>
                                        {
                                            InstanceGroups[i] = instance_group;
                                            ((DelegateCommandBase)SelectInstanceGroup).RaiseCanExecuteChanged();
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
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = new AggregateException(String.Format(Resources.Strings.ErrorInstanceGroupInfoLoad, _instance_directory_id[i]), ex)));
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = ex)); }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
                })).Start();
        }

        #endregion
    }
}
