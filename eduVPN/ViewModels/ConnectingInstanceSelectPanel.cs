/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance select panel
    /// </summary>
    public class ConnectingInstanceSelectPanel : BindableBase
    {
        #region Properties

        /// <summary>
        /// The page parent
        /// </summary>
        public ConnectWizard Parent { get; }

        /// <summary>
        /// Selected instance source type
        /// </summary>
        public Models.InstanceSourceType InstanceSourceType { get; }

        /// <summary>
        /// Selected instance source
        /// </summary>
        public Models.InstanceSource InstanceSource
        {
            get { return Parent.InstanceSources[(int)InstanceSourceType]; }
        }

        /// <summary>
        /// Select instance command
        /// </summary>
        public DelegateCommand SelectInstance
        {
            get
            {
                if (_select_instance == null)
                {
                    _select_instance = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceSourceType = InstanceSourceType;

                                // Go to profile selection page.
                                Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => InstanceSource.ConnectingInstance != null);

                    // Setup canExecute refreshing.
                    InstanceSource.PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(InstanceSource.ConnectingInstance)) _select_instance.RaiseCanExecuteChanged(); };
                }

                return _select_instance;
            }
        }
        private DelegateCommand _select_instance;

        /// <summary>
        /// Menu label for <c>ForgetSelectedInstance</c> command
        /// </summary>
        public string ForgetSelectedInstanceLabel
        {
            get { return string.Format(Resources.Strings.InstanceForget, InstanceSource.ConnectingInstance); }
        }

        /// <summary>
        /// Forget selected instance command
        /// </summary>
        public DelegateCommand ForgetSelectedInstance
        {
            get
            {
                if (_forget_selected_instance == null)
                {
                    _forget_selected_instance = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Remove instance from history.
                                InstanceSource.ConnectingInstanceList.Remove(InstanceSource.ConnectingInstance);
                                InstanceSource.ConnectingInstance = InstanceSource.ConnectingInstanceList.FirstOrDefault();

                                // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                                if (Parent.StartingPage != Parent.CurrentPage)
                                    Parent.CurrentPage = Parent.StartingPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            InstanceSource is Models.LocalInstanceSource &&
                            InstanceSource.ConnectingInstance != null &&
                            InstanceSource.ConnectingInstanceList.IndexOf(InstanceSource.ConnectingInstance) >= 0 &&
                            !Parent.Sessions.Any(session =>
                                session.AuthenticatingInstance.Equals(InstanceSource.AuthenticatingInstance) &&
                                session.ConnectingProfile.Instance.Equals(InstanceSource.ConnectingInstance)));

                    // Setup canExecute refreshing.
                    InstanceSource.PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(InstanceSource.ConnectingInstance)) _forget_selected_instance.RaiseCanExecuteChanged(); };
                    InstanceSource.ConnectingInstanceList.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_instance.RaiseCanExecuteChanged();
                    Parent.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_instance.RaiseCanExecuteChanged();
                }

                return _forget_selected_instance;
            }
        }
        private DelegateCommand _forget_selected_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingInstanceSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type)
        {
            Parent = parent;
            InstanceSourceType = instance_source_type;

            InstanceSource.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(InstanceSource.ConnectingInstance))
                    RaisePropertyChanged(nameof(ForgetSelectedInstanceLabel));
            };
        }

        #endregion
    }
}
