/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Custom instance group entry wizard page
    /// </summary>
    public class CustomInstanceGroupPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Instance URI
        /// </summary>
        public string URI
        {
            get { return _uri; }
            set
            {
                if (value != _uri)
                {
                    _uri = value;
                    RaisePropertyChanged();
                    ((DelegateCommandBase)SelectCustomInstanceGroup).RaiseCanExecuteChanged();
                }
            }
        }
        private string _uri;

        /// <summary>
        /// Authorize Other Instance Command
        /// </summary>
        public ICommand SelectCustomInstanceGroup
        {
            get
            {
                if (_select_custom_instance_group == null)
                {
                    _select_custom_instance_group = new DelegateCommand(
                        // execute
                        async () => {
                            Error = null;
                            TaskCount++;
                            try
                            {
                                // Get and parse instance group JSON.
                                var instance_group = Models.InstanceGroupInfo.FromJSON(
                                    (Dictionary<string, object>)eduJSON.Parser.Parse(
                                        (await JSON.Response.GetAsync(
                                            uri: new Uri(URI),
                                            ct: ConnectWizard.Abort.Token)).Value,
                                        ConnectWizard.Abort.Token));

                                // Reuse instance group selection page's SelectInstanceGroup command to set instance group.
                                if (Parent.InstanceGroupSelectPage.SelectInstanceGroup.CanExecute(instance_group))
                                    Parent.InstanceGroupSelectPage.SelectInstanceGroup.Execute(instance_group);
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { TaskCount--; }
                        },

                        // canExecute
                        () => {
                            try { new Uri(URI); }
                            catch (Exception) { return false; }
                            return true;
                        });
                }
                return _select_custom_instance_group;
            }
        }
        private ICommand _select_custom_instance_group;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public CustomInstanceGroupPage(ConnectWizard parent) :
            base(parent)
        {
            URI = "https://";
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            Parent.CurrentPage = Parent.InstanceGroupSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
