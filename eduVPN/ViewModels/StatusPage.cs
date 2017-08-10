﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream tolerates multiple disposes.")]
        public override void OnActivate()
        {
            base.OnActivate();

            // State >> Initializing...
            State = Models.StatusType.Initializing;
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

            // Launch VPN connecting task in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = null));
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));
                    try
                    {
                        // Get profile's OpenVPN configuration and instance client certificate (in parallel).
                        string profile_config = null;
                        byte[] client_certificate_hash = null;
                        new List<Action>()
                        {
                            () => { profile_config = Parent.ConnectingProfile.GetOpenVPNConfig(Parent.ConnectingInstance, Parent.AccessToken, ConnectWizard.Abort.Token); },
                            () => { client_certificate_hash = Parent.ConnectingInstance.GetClientCertificate(Parent.AccessToken, ConnectWizard.Abort.Token); }
                        }.Select(
                            action =>
                            {
                                var t = new Thread(new ThreadStart(
                                    () =>
                                    {
                                        try
                                        {
                                            action();
                                        }
                                        catch (OperationCanceledException) { }
                                        catch (Exception ex)
                                        {
                                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                            {
                                                Error = ex;
                                                State = Models.StatusType.Error;
                                            }));
                                        }
                                    }));
                                t.Start();
                                return t;
                            }).ToList().ForEach(t => t.Join());
                        if (Error != null)
                            return;

                        var profile_config_path =
                            Path.GetTempPath() +
                            "eduVPN-" + Guid.NewGuid().ToString() + ".ovpn";
                        try
                        {
                            try
                            {
                                using (var fs = new FileStream(
                                    profile_config_path,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.Read,
                                    1048576,
                                    FileOptions.SequentialScan))
                                using (var sw = new StreamWriter(fs))
                                {
                                    // Save profile's OpenVPN configuration to file.
                                    sw.Write(profile_config);
                                    sw.Write(
                                        "\n\n# eduVPN Client for Windows Specific\n" +
                                        "cryptoapicert \"THUMB: " + BitConverter.ToString(client_certificate_hash).Replace("-", " ") + "\"\n");
                                }
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex) { throw new AggregateException(String.Format(Resources.Strings.ErrorSavingProfileConfiguration, profile_config_path), ex); }

                            // TODO: Connect to OpenVPN Interactive Service (using named pipe) and ask it to start openvpn.exe for us.
                        }
                        finally
                        {
                            try { File.Delete(profile_config_path); }
                            catch (Exception) { }
                        }

                        // State >> Connecting...
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connecting));

                        // Wait for three seconds, then switch to connected state.
                        if (ConnectWizard.Abort.Token.WaitHandle.WaitOne(1000 * 3)) return;
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connected));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) {
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            Error = ex;
                            State = Models.StatusType.Error;
                        }));
                    }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--)); }
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
