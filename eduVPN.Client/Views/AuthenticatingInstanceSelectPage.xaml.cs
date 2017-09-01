/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for SecureInternetSelectPage.xaml and InstituteAccessSelectPage.xaml
    /// </summary>
    public partial class AuthenticatingInstanceSelectPage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public AuthenticatingInstanceSelectPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        private void InstanceSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Using SelectionChanged of a ListBox renders the UI accessibility unfriendly. Discuss other options with Rogier.
            // Either:
            // 1. Change the UI to provide separate button after the selection is made.
            // 2. Change the list of instances to a stack of buttons of instances

            if (e.AddedItems.Count > 0)
            {
                // User selected an instance.
                var view_model = (ViewModels.AuthenticatingInstanceSelectPage)DataContext;
                if (view_model != null && // Sometimes this event gets called with null view model.
                    view_model.AuthorizeSelectedInstance.CanExecute())
                {
                    view_model.AuthorizeSelectedInstance.Execute();

                    // Reset selected instance, to prevent repetitive triggering.
                    view_model.SelectedInstance = null;
                }
            }
        }

        #endregion
    }
}
