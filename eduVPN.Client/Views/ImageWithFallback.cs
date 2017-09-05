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
        private static readonly RequestCachePolicy _default_request_cache_policy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);

        /// <summary>
        /// Source bitmap image
        /// </summary>
        private BitmapImage _bitmap_image;

        /// <summary>
        /// Fall-back source bitmap image
        /// </summary>
        private BitmapImage _fallback_bitmap_image;

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
        public static readonly DependencyProperty UriSourceProperty = DependencyProperty.Register("UriSource", typeof(Uri), typeof(ImageWithFallback), new PropertyMetadata(null, OnUriSourceChanged));

        /// <summary>
        /// Image source URI when <c>UriSource</c> failed downloading or decoding
        /// </summary>
        public Uri UriFallbackSource {
            get { return GetValue(UriFallbackSourceProperty) as Uri; }
            set { SetValue(UriFallbackSourceProperty, value); }
        }
        public static readonly DependencyProperty UriFallbackSourceProperty = DependencyProperty.Register("UriFallbackSource", typeof(Uri), typeof(ImageWithFallback), new PropertyMetadata(null, OnUriFallbackSourceChanged));

        #endregion

        #region Methods

        private static void OnUriSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageWithFallback _this)
            {
                if (e.NewValue is Uri uri)
                {
                    // Load the specified image.
                    _this._bitmap_image = new BitmapImage();
                    _this._bitmap_image.BeginInit();
                    _this._bitmap_image.UriCachePolicy = _default_request_cache_policy;
                    _this._bitmap_image.CacheOption = BitmapCacheOption.OnDemand;
                    _this._bitmap_image.UriSource = uri;
                    _this._bitmap_image.DownloadFailed += (object sender2, System.Windows.Media.ExceptionEventArgs e2) => _this.LoadFallbackImage();
                    _this._bitmap_image.DecodeFailed += (object sender2, System.Windows.Media.ExceptionEventArgs e2) => _this.LoadFallbackImage();
                    _this._bitmap_image.EndInit();

                    _this.Source = _this._bitmap_image;
                }
                else
                    _this.LoadFallbackImage();
            }
        }

        private static void OnUriFallbackSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageWithFallback _this)
            {
                if (_this.Source == _this._fallback_bitmap_image && e.NewValue is Uri uri)
                {
                    // We're displaying the fall-back image and it changed. Reload it.
                    _this.LoadFallbackImage();
                }
            }
        }

        private void LoadFallbackImage()
        {
            if (UriFallbackSource != null)
            {
                // Load fall-back image.
                _fallback_bitmap_image = new BitmapImage();
                _fallback_bitmap_image.BeginInit();
                _fallback_bitmap_image.UriCachePolicy = _default_request_cache_policy;
                _fallback_bitmap_image.CacheOption = BitmapCacheOption.OnLoad;
                _fallback_bitmap_image.UriSource = UriFallbackSource;
                _fallback_bitmap_image.EndInit();

                Source = _fallback_bitmap_image;
            }
        }

        #endregion
    }
}
