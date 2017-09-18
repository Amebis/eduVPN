/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows;

namespace eduVPN.Views
{
    /// <summary>
    /// Helper class to save the binding context for later reuse
    /// </summary>
    public class BindingProxy : Freezable
    {
        #region Properties

        /// <summary>
        /// Binding data
        /// </summary>
        public object Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), null);

        #endregion

        #region Methods

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        #endregion
    }
}
