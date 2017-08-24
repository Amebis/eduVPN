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
    /// Custom instance source entry wizard page
    /// </summary>
    public class CustomInstanceSourcePage : ConnectWizardPage
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
                    ((DelegateCommandBase)SelectCustomInstanceSource).RaiseCanExecuteChanged();
                }
            }
        }
        private string _uri;

        /// <summary>
        /// Authorize Other Instance Command
        /// </summary>
        public ICommand SelectCustomInstanceSource
        {
            get
            {
                if (_select_custom_instance_source == null)
                {
                    _select_custom_instance_source = new DelegateCommand(
                        // execute
                        async () => {
                            Parent.Error = null;
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Get and parse instance source JSON.
                                var instance_source = Models.InstanceSourceInfo.FromJSON(
                                    (Dictionary<string, object>)eduJSON.Parser.Parse(
                                        (await JSON.Response.GetAsync(
                                            uri: new Uri(URI),
                                            ct: ConnectWizard.Abort.Token)).Value,
                                        ConnectWizard.Abort.Token));

                                // Reuse instance source selection page's SelectInstanceSource command to set instance source.
                                if (Parent.InstanceSourceSelectPage.SelectInstanceSource.CanExecute(instance_source))
                                    Parent.InstanceSourceSelectPage.SelectInstanceSource.Execute(instance_source);
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => {
                            try { new Uri(URI); }
                            catch (Exception) { return false; }
                            return true;
                        });
                }
                return _select_custom_instance_source;
            }
        }
        private ICommand _select_custom_instance_source;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public CustomInstanceSourcePage(ConnectWizard parent) :
            base(parent)
        {
            URI = "https://";
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
