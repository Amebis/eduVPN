/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;
using System.Windows.Input;

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

        protected void ConfigurationList_SelectItem(object sender, InputEventArgs e)
        {
            if (sender is ListBoxItem listbox_item &&
                listbox_item.DataContext is Models.VPNConfiguration configuration &&
                DataContext is ViewModels.ConfigurationSelectPanel view_model)
            {
                // Connect selected configuration.
                if (view_model.ConnectConfiguration.CanExecute(configuration))
                    view_model.ConnectConfiguration.Execute(configuration);

                e.Handled = true;
            }
        }

        protected void ConfigurationList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                ConfigurationList_SelectItem(sender, e);
        }

        #endregion
    }
}
