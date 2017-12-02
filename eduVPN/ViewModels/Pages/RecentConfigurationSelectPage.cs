/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Panels;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Recent profile selection wizard page
    /// </summary>
    public class RecentConfigurationSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Configuration history panels
        /// </summary>
        public ConnectingSelectPanel[] Panels
        {
            get { return _panels; }
        }
        private ConnectingSelectPanel[] _panels;

        /// <summary>
        /// Add another instance or profile
        /// </summary>
        public DelegateCommand AddAnotherEntry
        {
            get
            {
                if (_add_another_entry == null)
                    _add_another_entry = new DelegateCommand(
                        //execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try { Parent.CurrentPage = Parent.InstanceSourceSelectPage; }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        });

                return _add_another_entry;
            }
        }
        private DelegateCommand _add_another_entry;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a recent profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public RecentConfigurationSelectPage(ConnectWizard parent) :
            base(parent)
        {
            // Create history panels.
            var source_type_length = (int)InstanceSourceType._end;
            _panels = new ConnectingSelectPanel[Parent.InstanceSources.Length];
            for (var source_index = (int)InstanceSourceType._start; source_index < source_type_length; source_index++)
            {
                if (Parent.InstanceSources[source_index] is LocalInstanceSource)
                {
                    switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                    {
                        case 0: _panels[source_index] = new ConnectingProfileSelectPanel(Parent, (InstanceSourceType)source_index); break;
                        case 1: _panels[source_index] = new ConnectingInstanceSelectPanel(Parent, (InstanceSourceType)source_index); break;
                        case 2: _panels[source_index] = new ConnectingProfileSelectPanel(Parent, (InstanceSourceType)source_index); break;
                        case 3: _panels[source_index] = new ConnectingInstanceAndProfileSelectPanel(Parent, (InstanceSourceType)source_index); break;
                    }
                }
                else if (
                    Parent.InstanceSources[source_index] is DistributedInstanceSource ||
                    Parent.InstanceSources[source_index] is FederatedInstanceSource)
                    _panels[source_index] = new ConnectingInstanceAndProfileSelectPanel(Parent, (InstanceSourceType)source_index);
            }
        }

        #endregion
    }
}
