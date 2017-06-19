/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace eduVPN
{
    public class ObservableObject : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property.
        /// </summary>
        /// <param name="propertyName">The name of the property that was changed.</param>
        /// <remarks>
        /// The <c>CallerMemberName</c> attribute that is applied to the optional <paramref name="propertyName"/> 
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </remarks>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}
