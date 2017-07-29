/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
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
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = null));
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));
                    try
                    {
                        // Get and load profile list.
                        var profile_list = new JSON.Collection<Models.ProfileInfo>();
                        try
                        {
                            profile_list.LoadJSONAPIResponse(JSON.Response.Get(
                                uri: Parent.ConnectingEndpoints.ProfileList,
                                token: Parent.AccessToken,
                                ct: ConnectWizard.Abort.Token).Value, "profile_list", ConnectWizard.Abort.Token);
                        }
                        catch (WebException ex)
                        {
                            // Access token rejected (401) => Redirect back to authorization page.
                            if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.CurrentPage = Parent.AuthorizationPage));
                            else
                                throw;
                        }

                        // Send the loaded profile list back to the UI thread.
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ProfileList = profile_list));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = new AggregateException(Resources.Strings.ErrorProfileListLoad, ex))); }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--)); }
                })).Start();
        }

        #endregion
    }
}
