/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for ConnectingInstanceAndProfileSelectPanel.xaml
    /// </summary>
    public partial class ConnectingInstanceAndProfileSelectPanel : UserControl
    {
        #region Constructors

        public ConnectingInstanceAndProfileSelectPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Using SelectionChanged of a ListBox renders the UI accessibility unfriendly. Discuss other options with Rogier.
            // Either:
            // 1. Change the UI to provide separate button after the selection is made.
            // 2. Change the list of profiles to a stack of buttons of profiles

            if (e.AddedItems.Count > 0)
            {
                // User selected a recent configuration.
                var view_model = (ViewModels.ConnectingInstanceAndProfileSelectPanel)DataContext;
                if (view_model != null && // Sometimes this event gets called with null view model.
                    view_model.ConnectSelectedProfile.CanExecute())
                {
                    view_model.ConnectSelectedProfile.Execute();
                }
            }
        }

        #endregion
    }
}
