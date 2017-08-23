/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Net;
using System.Threading;
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
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo SelectedInstance
        {
            get { return _selected_instance; }
            set {
                _selected_instance = value;
                RaisePropertyChanged();

                ProfileList = new JSON.Collection<Models.ProfileInfo>();
                if (_selected_instance != null)
                {
                    new Thread(new ThreadStart(
                        () =>
                        {
                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = null));
                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                            try
                            {
                                JSON.Collection<Models.ProfileInfo> profile_list = null;
                                try
                                {
                                    // Get and load profile list.
                                    profile_list = _selected_instance.GetProfileList(Parent.Configuration.AccessToken, ConnectWizard.Abort.Token);
                                }
                                catch (AggregateException ex)
                                {
                                    // Access token rejected (401) => Redirect back to authorization page.
                                    if (ex.InnerException is WebException ex_inner && ex_inner.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.CurrentPage = Parent.AuthorizationPage));
                                    else
                                        throw;
                                }

                                // Send the loaded profile list back to the UI thread.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ProfileList = profile_list));
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = ex)); }
                            finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
                        })).Start();
                }
            }
        }
        private Models.InstanceInfo _selected_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceAndProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            if (Parent.InstanceGroup is Models.FederatedInstanceGroupInfo)
            {
                // Federated instance group does not favour any particular instance to connect to.
                SelectedInstance = null;
            }
            else
            {
                // By default, select the same connecting instance as authenticating instance.
                SelectedInstance = Parent.Configuration.AuthenticatingInstance;
            }
        }

        protected override void DoConnectSelectedProfile()
        {
            Parent.Configuration.ConnectingInstance = SelectedInstance;

            base.DoConnectSelectedProfile();
        }

        protected override bool CanConnectSelectedProfile()
        {
            return
                SelectedInstance != null &&
                base.CanConnectSelectedProfile();
        }

        #endregion
    }
}
