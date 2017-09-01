/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for LocalConfigurationHistoryPanel.xaml
    /// </summary>
    public partial class LocalConfigurationHistoryPanel : UserControl
    {
        #region Constructors

        public LocalConfigurationHistoryPanel()
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

            if (e.AddedItems.Count > 0)
            {
                // User selected a recent configuration.
                var view_model = (ViewModels.LocalConfigurationHistoryPanel)DataContext;
                if (view_model != null && // Sometimes this event gets called with null view model.
                    view_model.ConnectSelectedConfiguration.CanExecute())
                {
                    view_model.ConnectSelectedConfiguration.Execute();

                    // Reset selected configuration, to prevent repetitive triggering.
                    view_model.SelectedConfiguration = null;
                }
            }
        }

        #endregion
    }
}
