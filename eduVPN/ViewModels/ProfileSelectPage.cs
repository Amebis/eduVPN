/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
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
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                param => {
                    // Set busy flag (in the UI thread).
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                    try
                    {
                        // Get and load profile list.
                        var profile_list = new JSON.Collection<Models.ProfileInfo>();
                        profile_list.LoadJSONAPIResponse(JSON.Response.Get(
                            Parent.ConnectingEndpoints.ProfileList,
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
                        // Notify the sender the profile list loading failed.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                    finally
                    {
                        // Clear busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                    }
                }));
        }

        #endregion
    }
}
