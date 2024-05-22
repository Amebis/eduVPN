/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for SearchPage.xaml
    /// </summary>
    public partial class SearchPage : Page
    {
        #region Types

        class TypedStyleSelector<T> : StyleSelector
        {
            #region Fields

            static readonly ResourceDictionary resourceDictionary = Application.LoadComponent(new Uri("eduVPN.Views;component/Resources/Styles.xaml", UriKind.Relative)) as ResourceDictionary;
            public Style DefaultStyle { get; set; }

            #endregion

            #region Methods

            public override Style SelectStyle(object item, DependencyObject container)
            {
                if (item is T)
                    return resourceDictionary["PassiveListBoxItemStyle"] as Style;
                return DefaultStyle;
            }

            #endregion
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a page
        /// </summary>
        public SearchPage()
        {
            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, RoutedEventArgs e) =>
            {
                InstituteAccessServers.ItemContainerStyle = null;
                InstituteAccessServers.ItemContainerStyleSelector = new TypedStyleSelector<MoreHitsInstituteAccessServer>
                {
                    DefaultStyle = Resources["InstituteAccessServersStyle"] as Style
                };
                Organizations.ItemContainerStyle = null;
                Organizations.ItemContainerStyleSelector = new TypedStyleSelector<MoreHitsOrganization>
                {
                    DefaultStyle = Resources["OrganizationsStyle"] as Style
                };

                Query.Focus();
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Confirms institute access server selection on the list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void InstituteAccessServers_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.SearchPage viewModel)
            {
                // Authorize selected server.
                if (viewModel.ConfirmInstituteAccessServerSelection.CanExecute())
                    viewModel.ConfirmInstituteAccessServerSelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms institute access server selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void InstituteAccessServers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
            {
                ((ListBoxItem)sender).IsSelected = true;
                InstituteAccessServers_SelectItem(sender, e);
            }
        }

        /// <summary>
        /// Confirms organization selection on the list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void Organizations_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.SearchPage viewModel)
            {
                // Authorize selected organization.
                if (viewModel.ConfirmOrganizationSelection.CanExecute())
                    viewModel.ConfirmOrganizationSelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms organization selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void Organizations_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
            {
                ((ListBoxItem)sender).IsSelected = true;
                Organizations_SelectItem(sender, e);
            }
        }

        /// <summary>
        /// Confirms own server selection on the list
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void OwnServers_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.SearchPage viewModel)
            {
                // Authorize selected server.
                if (viewModel.ConfirmOwnServerSelection.CanExecute())
                    viewModel.ConfirmOwnServerSelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms own server selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void OwnServers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
            {
                ((ListBoxItem)sender).IsSelected = true;
                OwnServers_SelectItem(sender, e);
            }
        }

        #endregion
    }
}
