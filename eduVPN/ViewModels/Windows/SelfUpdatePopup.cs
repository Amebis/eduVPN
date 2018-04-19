/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using Prism.Commands;
using System;
using System.Diagnostics;

namespace eduVPN.ViewModels.Windows
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
        public Version InstalledVersion { get; }

        /// <summary>
        /// Available product version
        /// </summary>
        public Version AvailableVersion { get; }

        /// <summary>
        /// Product changelog
        /// </summary>
        public Uri ChangelogPath { get; }

        /// <summary>
        /// Action instructed by user
        /// </summary>
        /// <remarks>Should be populated by action on event end.</remarks>
        public PromptSelfUpdateActionType Action
        {
            get { return _action; }
            set { SetProperty(ref _action, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private PromptSelfUpdateActionType _action;

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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<PromptSelfUpdateActionType?> _do_action;

        /// <summary>
        /// Show changelog command
        /// </summary>
        public DelegateCommand ShowChangelog
        {
            get
            {
                if (_show_changelog == null)
                    _show_changelog = new DelegateCommand(
                        // execute
                        () =>
                        {
                            ChangeTaskCount(+1);
                            try
                            {
                                Process.Start(ChangelogPath.ToString());
                                Error = null;
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => ChangelogPath != null);

                return _show_changelog;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _show_changelog;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a popup
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments</param>
        public SelfUpdatePopup(object sender, PromptSelfUpdateEventArgs e)
        {
            InstalledVersion = e.InstalledVersion;
            AvailableVersion = e.AvailableVersion;
            ChangelogPath = e.ChangelogPath;
        }

        #endregion
    }
}
