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
    /// Self update prompt pop-up
    /// </summary>
    public class SelfUpdatePopup : Window
    {
        #region Properties

        /// <summary>
        /// Installed product version
        /// </summary>
        public Version InstalledVersion
        {
            get { return _detected_version; }
            set { SetProperty(ref _detected_version, value); }
        }
        private Version _detected_version;

        /// <summary>
        /// Available product version
        /// </summary>
        public Version AvailableVersion
        {
            get { return _available_version; }
            set { SetProperty(ref _available_version, value); }
        }
        private Version _available_version;

        /// <summary>
        /// Action instructed by user
        /// </summary>
        /// <remarks>Should be populated by action on event end.</remarks>
        public PromptSelfUpdateActionType Action { get; set; }

        /// <summary>
        /// Action command
        /// </summary>
        public DelegateCommand<PromptSelfUpdateActionType?> DoAction
        {
            get
            {
                if (_do_action == null)
                    _do_action = new DelegateCommand<PromptSelfUpdateActionType?>(
                        // execute
                        action =>
                        {
                            ChangeTaskCount(+1);
                            try
                            {
                                Action = action.Value;
                                Error = null;
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { ChangeTaskCount(-1); }
                        },

                        // canExecute
                        action => action != null);

                return _do_action;
            }
        }
        private DelegateCommand<PromptSelfUpdateActionType?> _do_action;

        #endregion
    }
}
