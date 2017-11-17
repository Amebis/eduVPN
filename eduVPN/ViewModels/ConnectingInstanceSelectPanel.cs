/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.ComponentModel;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connecting instance select panel
    /// </summary>
    public class ConnectingInstanceSelectPanel : ConnectingSelectPanel
    {
        #region Properties

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

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingInstanceSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
        }

        #endregion
    }
}
