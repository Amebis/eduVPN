/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns status icon according to status state.
    /// </summary>
    public class OpenVPNStateTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OpenVPNStateType status_type)
            {
                switch (status_type)
                {
                    case OpenVPNStateType.Connecting:
                    case OpenVPNStateType.Exiting:
                        return Application.Current.TryFindResource("eduVPNStatusTypeInitializingIcon") as Canvas;

                    case OpenVPNStateType.Waiting:
                    case OpenVPNStateType.Authenticating:
                    case OpenVPNStateType.GettingConfiguration:
                    case OpenVPNStateType.AssigningIP:
                    case OpenVPNStateType.AddingRoutes:
                    case OpenVPNStateType.Reconnecting:
                        return Application.Current.TryFindResource("eduVPNStatusTypeConnectingIcon") as Canvas;

                    case OpenVPNStateType.Connected:
                        return Application.Current.TryFindResource("eduVPNStatusTypeConnectedIcon") as Canvas;

                    default:
                        return null;
                }
            }
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
