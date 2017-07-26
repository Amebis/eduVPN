/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance and profile selection wizard page
    /// </summary>
    public class InstanceAndProfileSelectPage : ProfileSelectBasePage
    {
        #region Properties

        /// <summary>
        /// List of available instances
        /// </summary>
        public Models.InstanceList InstanceList
        {
            get { return _instance_list; }
            set { _instance_list = value; RaisePropertyChanged(); }
        }
        private Models.InstanceList _instance_list;

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.Instance SelectedInstance
        {
            get { return _selected_instance; }
            set {
                _selected_instance = value;
                RaisePropertyChanged();

                ProfileList = new JSON.Collection<Models.Profile>();
                ThreadPool.QueueUserWorkItem(new WaitCallback(
                    param =>
                    {
                        // Set busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                        try
                        {
                            // Get and load API endpoints.
                            var uri_builder = new UriBuilder(_selected_instance.Base);
                            uri_builder.Path += "info.json";
                            var api = new Models.InstanceEndpoints();
                            api.LoadJSON(JSON.Response.Get(
                                uri_builder.Uri,
                                null,
                                null,
                                null,
                                _abort.Token).Value);

                            // Set selected instance API endpoints.
                            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => SelectedInstanceEndpoints = api));

                            // Get and load profile list.
                            var profile_list = new JSON.Collection<Models.Profile>();
                            profile_list.LoadJSONAPIResponse(JSON.Response.Get(
                                api.ProfileList,
                                null,
                                Parent.AccessToken,
                                null,
                                _abort.Token).Value, "profile_list", _abort.Token);

                            // Send the loaded profile list back to the UI thread.
                            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                ProfileList = profile_list;
                                ErrorMessage = null;
                            }));
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            // Notify the sender the API endpoints loading failed.
                            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message ));
                        }
                        finally
                        {
                            // Clear busy flag (in the UI thread).
                            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                        }
                    }));
            }
        }
        private Models.Instance _selected_instance;

        /// <summary>
        /// Selected eduVPN instance API endpoints
        /// </summary>
        public Models.InstanceEndpoints SelectedInstanceEndpoints
        {
            get { return _selected_instance_endpoints; }
            set { _selected_instance_endpoints = value; RaisePropertyChanged(); }
        }
        private Models.InstanceEndpoints _selected_instance_endpoints;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceAndProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
            // Attach the instance list to the instance selection page's list.
            // By default, select the same connecting instance as authenticating instance.
            switch (Parent.AccessType)
            {
                case AccessType.SecureInternet:
                    InstanceList = Parent.SecureInternetSelectPage.InstanceList;
                    SelectedInstance = Parent.SecureInternetSelectPage.SelectedInstance;
                    break;

                case AccessType.InstituteAccess:
                    InstanceList = Parent.InstituteAccessSelectPage.InstanceList;
                    SelectedInstance = Parent.InstituteAccessSelectPage.SelectedInstance;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
    }
}
