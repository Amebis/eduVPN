/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows.Input;

namespace eduVPN.Views
{
    /// <summary>
    /// eduVPN windows base class
    /// </summary>
    public class Window : System.Windows.Window
    {
        #region Methods

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

        #endregion
    }
}
