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
        /// User info
        /// </summary>
        public Models.UserInfo UserInfo
        {
            get { return _user_info; }
            set { _user_info = value; RaisePropertyChanged(); }
        }
        private Models.UserInfo _user_info;

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

            // Set blank user info. This prevents flickering of user disabled message,
            // since UserInfo.IsEnabled will be available for binding before page displays.
            UserInfo = new Models.UserInfo();

            // Launch user info load in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = null));
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                    try
                    {
                        var user_info = Parent.Configuration.AuthenticatingInstance.GetUserInfo(Parent.Configuration.AuthenticatingInstance, Window.Abort.Token);
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => UserInfo = user_info));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = ex)); }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
                })).Start();

            MessageList = new Models.MessageList();

            // Load messages from all possible sources: authenticating/connecting instance, user/system list.
            // Any errors shall be ignored.
            var api_authenticating = Parent.Configuration.AuthenticatingInstance.GetEndpoints(Window.Abort.Token);
            var api_connecting = Parent.Configuration.ConnectingInstance.GetEndpoints(Window.Abort.Token);
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
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                        try
                        {
                            // Get and load user messages.
                            var message_list = new Models.MessageList();
                            message_list.LoadJSONAPIResponse(
                                JSON.Response.Get(
                                    uri: list.Key,
                                    token: Parent.Configuration.AuthenticatingInstance.PeekAccessToken(Window.Abort.Token),
                                    ct: Window.Abort.Token).Value,
                                list.Value,
                                Window.Abort.Token);

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
                        finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
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

            Parent.StartSession();
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
