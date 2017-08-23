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
    /// Profile selection wizard page
    /// </summary>
    public class ProfileSelectPage : ProfileSelectBasePage
    {
        #region Constructors

        /// <summary>
        /// Constructs a profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            // Launch profile list load in the background.
            new Thread(new ThreadStart(
                () => {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = null));
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                    try
                    {
                        JSON.Collection<Models.ProfileInfo> profile_list = null;
                        try
                        {
                            // Get and load profile list.
                            profile_list = Parent.Configuration.ConnectingInstance.GetProfileList(Parent.Configuration.AccessToken, ConnectWizard.Abort.Token);
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

        #endregion
    }
}
