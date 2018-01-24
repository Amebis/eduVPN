/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using QRCoder;
using System;
using System.Globalization;
using System.Web;
using System.Windows.Data;

namespace eduVPN.Client.Converters
{
    /// <summary>
    /// Returns TOTP QR code generated from TOTP secret and authenticating instance host name and display name.
    /// </summary>
    public class TOTPQRConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="MultiBinding"/> produces. The value <see cref="System.Windows.DependencyProperty.UnsetValue"/> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>
        /// A converted value.
        /// If the method returns <c>null</c>, the valid null value is used.
        /// A return value of <see cref="System.Windows.DependencyProperty.UnsetValue"/> indicates that the converter did not produce a value, and that the binding will use the <see cref="BindingBase.FallbackValue"/> if it is available, or else will use the default value.
        /// A return value of <see cref="Binding.DoNothing"/> indicates that the binding does not transfer the value or use the <see cref="BindingBase.FallbackValue"/> or the default value.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var secret   = values[0] as string;
                var user     = values[1] as UserInfo;
                var instance = values[2] as Instance;
                var otp_uri = string.Format(user.ID != null ? "otpauth://totp/{1}:{2}?secret={0}&issuer={3}" : "otpauth://totp/{1}?secret={0}&issuer={3}",
                    secret,
                    instance.Base.Host,
                    user.ID,
                    HttpUtility.UrlEncode(instance.ToString()));

                var qr_generator = new QRCodeGenerator();
                var qr_code_data = qr_generator.CreateQrCode(otp_uri, QRCodeGenerator.ECCLevel.Q);
                var qr_code = new XamlQRCode(qr_code_data);
                return qr_code.GetGraphic(3, true);
            }
            catch { }

            // Fallback to blank image.
            return new Uri("pack://application:,,,/Resources/Blank.png");
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>An array of values that have been converted from the target value back to the source values</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
