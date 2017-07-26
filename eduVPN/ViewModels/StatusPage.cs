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
    public class StatusPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Client connection state
        /// </summary>
        public Models.StatusType State
        {
            get { return _state; }
            set { if (value != _state) { _state = value; RaisePropertyChanged(); } }
        }
        private Models.StatusType _state;

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

            // State >> Initializing...
            State = Models.StatusType.Initializing;
            MessageList = new Models.MessageList();

            // Launch VPN connecting task in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                param =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                    try
                    {
                        var message_list = new Models.MessageList();

                        // Spawn user messages get.
                        var user_messages_get_task = JSON.Response.GetAsync(
                                Parent.AuthenticatingEndpoints.UserMessages,
                                null,
                                Parent.AccessToken,
                                null,
                                ConnectWizard.Abort.Token);

                        // Spawn system messages get.
                        var system_messages_get_task = JSON.Response.GetAsync(
                                Parent.AuthenticatingEndpoints.SystemMessages,
                                null,
                                Parent.AccessToken,
                                null,
                                ConnectWizard.Abort.Token);

                        // State >> Connecting...
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connecting));

                        try
                        {
                            // Wait for and load user messages.
                            user_messages_get_task.Wait(ConnectWizard.Abort.Token);
                            message_list.LoadJSONAPIResponse(user_messages_get_task.Result.Value, "user_messages", ConnectWizard.Abort.Token);

                            if (message_list.Count > 0)
                            {
                                // Add user messages.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                {
                                    foreach (var msg in message_list)
                                        MessageList.Add(msg);
                                }));
                            }

                            // Wait for and load system messages.
                            system_messages_get_task.Wait(ConnectWizard.Abort.Token);
                            message_list.LoadJSONAPIResponse(system_messages_get_task.Result.Value, "system_messages", ConnectWizard.Abort.Token);

                            if (message_list.Count > 0)
                            {
                                // Add system messages.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                {
                                    foreach (var msg in message_list)
                                        MessageList.Add(msg);
                                }));
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
                        }
                        catch (AggregateException ex)
                        {
                            // Access token rejected (401) => Redirect back to authorization page.
                            if (ex.InnerException is WebException ex_inner && ex_inner.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.CurrentPage = Parent.AuthorizationPage));
                        }
                        catch (Exception) { }

                        // Wait for three seconds, then switch to connected state.
                        if (ConnectWizard.Abort.Token.WaitHandle.WaitOne(1000 * 3)) return;
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connected));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Notify the sender the profile list loading failed.
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                    finally
                    {
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                    }
                }));
        }

        #endregion
    }
}
