/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows;
using System.Windows.Input;

namespace eduVPN.Views.Windows
{
    /// <summary>
    /// eduVPN windows base class
    /// </summary>
    public class Window : System.Windows.Window
    {
        #region Properties

        /// <summary>
        /// Brief text describing window intent
        /// </summary>
        public string Description
        {
            get { return GetValue(DescriptionProperty) as string; }
            set { SetValue(DescriptionProperty, value); }
        }
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(Window), null);

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Make window draggable.
            MouseDown +=
                (object sender, MouseButtonEventArgs e_mouse_down) =>
                {
                    if (e_mouse_down.ChangedButton == MouseButton.Left)
                        DragMove();
                };
        }

        /// <summary>
        /// Sets <see cref="System.Windows.Window.DialogResult"/> to <c>true</c> to close the dialog.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        #endregion
    }
}
