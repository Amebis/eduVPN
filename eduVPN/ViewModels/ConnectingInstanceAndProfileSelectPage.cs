/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance and profile selection wizard page
    /// </summary>
    public class ConnectingInstanceAndProfileSelectPage : ProfileSelectBasePage
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
                if (value != _selected_instance)
                {
                    _selected_instance = value;
                    RaisePropertyChanged();

                    ProfileList = new JSON.Collection<Models.ProfileInfo>();
                    if (_selected_instance != null)
                    {
                        new Thread(new ThreadStart(
                            () =>
                            {
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                                try
                                {
                                    // Get and load profile list.
                                    var profile_list = _selected_instance.GetProfileList(Parent.Configuration.AuthenticatingInstance, Window.Abort.Token);

                                    // Send the loaded profile list back to the UI thread.
                                    // We're not navigating to another page and OnActivate() will not be called to auto-reset error message. Therefore, reset it manually.
                                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                        () =>
                                        {
                                            ProfileList = profile_list;
                                            Parent.Error = null;
                                        }));
                                }
                                catch (OperationCanceledException) { }
                                catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = ex)); }
                                finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
                            })).Start();
                    }
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
        public ConnectingInstanceAndProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            // Initialize selected instance.
            SelectedInstance = Parent.Configuration.ConnectingInstance;
        }

        protected override void DoConnectSelectedProfile(Models.ProfileInfo profile)
        {
            // Save selected instance.
            Parent.Configuration.ConnectingInstance = SelectedInstance;

            // Let base class do the rest.
            base.DoConnectSelectedProfile(profile);
        }

        protected override bool CanConnectSelectedProfile(Models.ProfileInfo profile)
        {
            return
                SelectedInstance != null &&
                base.CanConnectSelectedProfile(profile);
        }

        #endregion
    }
}
