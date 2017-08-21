/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Globalization;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns title string to be used on the instance select page based on instance group type
    /// </summary>
    public class InstanceGroupToInstanceSelectTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.LocalInstanceGroupInfo)
                return Resources.Strings.ConnectingInstanceSelectPageTitle;
            else if (value is Models.DistributedInstanceGroupInfo)
                return Resources.Strings.AuthenticatingInstanceSelectPageTitle;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
