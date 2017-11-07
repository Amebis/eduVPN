/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;

namespace eduVPN.ViewModels
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
        public ConnectingInstanceAndProfileSelectPanel[] Panels
        {
            get { return _panels; }
        }
        private ConnectingInstanceAndProfileSelectPanel[] _panels;

        /// <summary>
        /// Add another instance
        /// </summary>
        public DelegateCommand AddAnotherInstance
        {
            get
            {
                if (_add_another_instance == null)
                    _add_another_instance = new DelegateCommand(
                        //execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try { Parent.CurrentPage = Parent.InstanceSourceSelectPage; }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        });

                return _add_another_instance;
            }
        }
        private DelegateCommand _add_another_instance;

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
            var source_type_length = (int)Models.InstanceSourceType._end;
            _panels = new ConnectingInstanceAndProfileSelectPanel[Parent.InstanceSources.Length];
            for (var source_index = (int)Models.InstanceSourceType._start; source_index < source_type_length; source_index++)
                _panels[source_index] = new ConnectingInstanceAndProfileSelectPanel(Parent, (Models.InstanceSourceType)source_index);
        }

        #endregion
    }
}
