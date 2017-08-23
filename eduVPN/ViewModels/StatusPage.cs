/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connection status wizard page
    /// </summary>
    public class StatusPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Merged list of user and system messages
        /// </summary>
        public Models.MessageList MessageList
        {
            get { return _message_list; }
            set { _message_list = value; RaisePropertyChanged(); }
        }
        private Models.MessageList _message_list;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a status wizard page
        /// </summary>
        /// <param name="parent"></param>
        public StatusPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            MessageList = new Models.MessageList();

            // Load messages from all possible sources: authenticating/connecting instance, user/system list.
            // Any errors shall be ignored.
            var api_authenticating = Parent.AuthenticatingInstance.GetEndpoints(ConnectWizard.Abort.Token);
            var api_connecting = Parent.ConnectingInstance.GetEndpoints(ConnectWizard.Abort.Token);
            foreach (
                var list in new List<KeyValuePair<Uri, string>>() {
                    new KeyValuePair<Uri, string>(api_authenticating.UserMessages, "user_messages"),
                    new KeyValuePair<Uri, string>(api_connecting.UserMessages, "user_messages"),
                    new KeyValuePair<Uri, string>(api_authenticating.SystemMessages, "system_messages"),
                    new KeyValuePair<Uri, string>(api_connecting.SystemMessages, "system_messages"),
                }
                .Where(list => list.Key != null)
                .Distinct(new EqualityComparer<KeyValuePair<Uri, string>>((x, y) => x.Key.AbsoluteUri == y.Key.AbsoluteUri && x.Value == y.Value)))
            {
                new Thread(new ThreadStart(
                    () =>
                    {
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));
                        try
                        {
                            // Get and load user messages.
                            var message_list = new Models.MessageList();
                            message_list.LoadJSONAPIResponse(
                                JSON.Response.Get(
                                    uri: list.Key,
                                    token: Parent.AccessToken,
                                    ct: ConnectWizard.Abort.Token).Value,
                                list.Value,
                                ConnectWizard.Abort.Token);

                            if (message_list.Count > 0)
                            {
                                // Add user messages.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                {
                                    foreach (var msg in message_list)
                                        MessageList.Add(msg);
                                }));
                            }
                        }
                        catch (Exception) { }
                        finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--)); }
                    })).Start();
            }

            //// Add test messages.
            //Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            //{
            //    MessageList.Add(new Models.MessageMaintenance()
            //    {
            //        Text = "This is a test maintenance message.",
            //        Date = DateTime.Now,
            //        Begin = new DateTime(2017, 7, 31, 22, 00, 00),
            //        End = new DateTime(2017, 7, 31, 23, 59, 00)
            //    });
            //}));

            Parent.Session = new Models.OpenVPNSession(Parent.ConnectingInstance, Parent.ConnectingProfile, Parent.AccessToken);

            // Launch VPN session in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = null));
                    try
                    {
                        Parent.Session.Run(ConnectWizard.Abort.Token);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { Error = ex; })); }
                })).Start();
        }

        protected override void DoNavigateBack()
        {
            // Terminate connection.
            if (Parent.Session != null && Parent.Session.Disconnect.CanExecute(null))
                Parent.Session.Disconnect.Execute(null);

            Parent.CurrentPage = Parent.ProfileSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
