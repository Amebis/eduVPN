/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Net.Cache;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace eduVPN.Views
{
    /// <summary>
    /// Image with fallback image URI
    /// </summary>
    public class ImageWithFallback : Image
    {
        #region Fields

        /// <summary>
        /// Default web-content caching policy.
        /// </summary>
        protected static readonly RequestCachePolicy _default_request_cache_policy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);

        #endregion

        #region Properties

        /// <summary>
        /// Image source URI
        /// </summary>
        public Uri UriSource
        {
            get { return GetValue(UriSourceProperty) as Uri; }
            set { SetValue(UriSourceProperty, value); }
        }
        public static readonly DependencyProperty UriSourceProperty = DependencyProperty.Register("UriSource", typeof(Uri), typeof(ImageWithFallback), new PropertyMetadata(null, null));

        /// <summary>
        /// Image source URI when <c>UriSource</c> failed downloading or decoding
        /// </summary>
        public Uri UriFallbackSource {
            get { return GetValue(UriFallbackSourceProperty) as Uri; }
            set { SetValue(UriFallbackSourceProperty, value); }
        }
        public static readonly DependencyProperty UriFallbackSourceProperty = DependencyProperty.Register("UriFallbackSource", typeof(Uri), typeof(ImageWithFallback), new PropertyMetadata(null, null));

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an image with fallback image URI
        /// </summary>
        public ImageWithFallback()
        {
            Loaded +=
                (object sender, RoutedEventArgs e) =>
                {
                    if (UriSource != null)
                    {
                        // Load the specified image.
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnDemand;
                        bi.UriCachePolicy = _default_request_cache_policy;
                        bi.DownloadFailed += (object sender2, System.Windows.Media.ExceptionEventArgs e2) => LoadFallbackImage();
                        bi.DecodeFailed += (object sender2, System.Windows.Media.ExceptionEventArgs e2) => LoadFallbackImage();
                        bi.UriSource = UriSource;
                        bi.EndInit();

                        Source = bi;
                    }
                    else
                        LoadFallbackImage();

                    e.Handled = true;
                };
        }

        #endregion

        #region Methods

        private void LoadFallbackImage()
        {
            if (UriFallbackSource != null)
            {
                // Load fall-back image.
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriCachePolicy = _default_request_cache_policy;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = UriFallbackSource;
                bi.EndInit();

                Source = bi;
            }
        }

        #endregion
    }
}
