/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for ConfigurationSelectPanel.xaml
    /// </summary>
    public partial class ConfigurationSelectPanel : UserControl
    {
        #region Constructors

        public ConfigurationSelectPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        private void ConfigurationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Using SelectionChanged of a ListBox renders the UI accessibility unfriendly. Discuss other options with Rogier.
            // Either:
            // 1. Change the UI to provide separate button after the selection is made.
            // 2. Change the list of profiles to a stack of buttons of profiles

            if (e.AddedItems.Count > 0 &&
                DataContext is ViewModels.ConfigurationSelectPanel view_model &&
                view_model.ConnectConfiguration.CanExecute(view_model.SelectedConfiguration))
            {
                // Connect selected configuration.
                view_model.ConnectConfiguration.Execute(view_model.SelectedConfiguration);

                // Reset selected configuration, to prevent repetitive triggering.
                view_model.SelectedConfiguration = null;
            }
        }

        #endregion
    }
}
