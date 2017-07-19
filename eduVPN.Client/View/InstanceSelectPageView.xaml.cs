/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModel;
using System.Windows.Controls;

namespace eduVPN.View
{
    /// <summary>
    /// Interaction logic for InstanceSelectPageView.xaml
    /// </summary>
    public partial class InstanceSelectPageView : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public InstanceSelectPageView()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        private void InstanceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Using SelectionChanged of a ListBox renders the UI accessibility unfriendly.
            // Either:
            // 1. Change the UI to provide separate button after the selection is made.
            // 2. Change the list of instances to a stack of buttons of instances

            // User selected an instance.
            var viewmodel = (InstanceSelectPageViewModel)DataContext;
            if (viewmodel != null && // Sometimes this event gets called with null view model.
                viewmodel.AuthorizeSelectedInstance.CanExecute(null))
            {
                viewmodel.AuthorizeSelectedInstance.Execute(null);
            }
        }

        #endregion
    }
}
