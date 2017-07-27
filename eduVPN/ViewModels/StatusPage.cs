/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web;
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

            // Load messages from all possible sources: authenticating/connecting instance, user/system list
            foreach (
                var list in new List<KeyValuePair<Uri, string>>() {
                    new KeyValuePair<Uri, string>(Parent.AuthenticatingEndpoints.UserMessages, "user_messages"),
                    new KeyValuePair<Uri, string>(Parent.ConnectingEndpoints.UserMessages, "user_messages"),
                    new KeyValuePair<Uri, string>(Parent.AuthenticatingEndpoints.SystemMessages, "system_messages"),
                    new KeyValuePair<Uri, string>(Parent.ConnectingEndpoints.SystemMessages, "system_messages"),
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
                        finally
                        {
                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                        }
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

            // Launch VPN connecting task in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                    try
                    {
                        // Spawn client certificate get.
                        var certificate_get_task = JSON.Response.GetAsync(
                            uri: Parent.ConnectingEndpoints.CreateCertificate,
                            param: new NameValueCollection
                            {
                                { "display_name", Resources.Strings.CertificateTitle }
                            },
                            token: Parent.AccessToken,
                            ct: ConnectWizard.Abort.Token);

                        // Spawn profile config get.
                        var uri_builder = new UriBuilder(Parent.ConnectingEndpoints.ProfileConfig);
                        var query = HttpUtility.ParseQueryString(uri_builder.Query);
                        query["profile_id"] = Parent.ConnectingProfile.ID;
                        uri_builder.Query = query.ToString();
                        var profile_config_get_task = JSON.Response.GetAsync(
                            uri: uri_builder.Uri,
                            token: Parent.AccessToken,
                            response_type: "application/x-openvpn-profile",
                            ct: ConnectWizard.Abort.Token);

                        // Load profile config.
                        try { profile_config_get_task.Wait(ConnectWizard.Abort.Token); }
                        catch (AggregateException ex) { throw ex.InnerException; }

                        // Load certificate and import it to Windows user certificate store.
                        try { certificate_get_task.Wait(ConnectWizard.Abort.Token); }
                        catch (AggregateException ex) { throw ex.InnerException; }
                        var certificate = new Models.Certificate();
                        certificate.LoadJSONAPIResponse(certificate_get_task.Result.Value, "create_keypair", ConnectWizard.Abort.Token);

                        var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);
                        try
                        {
                            store.Add(certificate.Value);
                        }
                        finally { store.Close(); }

                        // State >> Connecting...
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connecting));

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
                })).Start();
        }

        protected override void DoNavigateBack()
        {
            if (Parent.InstanceList is Models.InstanceInfoFederatedList)
                Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
            else if (Parent.InstanceList is Models.InstanceInfoDistributedList)
                Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
            else
                Parent.CurrentPage = Parent.ProfileSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
