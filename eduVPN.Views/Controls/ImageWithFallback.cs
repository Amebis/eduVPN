/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace eduVPN.Views.Controls
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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly RequestCachePolicy _default_request_cache_policy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);

        /// <summary>
        /// Are we displaying the fall-back image?
        /// </summary>
        private bool _is_fallback;

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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly DependencyProperty UriSourceProperty = DependencyProperty.Register("UriSource", typeof(Uri), typeof(ImageWithFallback), new PropertyMetadata(null, OnUriSourceChanged));

        /// <summary>
        /// Image source URI when <see cref="UriSource"/> failed downloading or decoding
        /// </summary>
        public Uri UriFallbackSource {
            get { return GetValue(UriFallbackSourceProperty) as Uri; }
            set { SetValue(UriFallbackSourceProperty, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly DependencyProperty UriFallbackSourceProperty = DependencyProperty.Register("UriFallbackSource", typeof(Uri), typeof(ImageWithFallback), new PropertyMetadata(null, OnUriFallbackSourceChanged));

        #endregion

        #region Methods

        private static void OnUriSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageWithFallback _this)
            {
                if (e.OldValue == null || !e.OldValue.Equals(e.NewValue))
                {
                    // Value changed.
                    if (e.NewValue is Uri uri)
                        _this.LoadImage(uri);
                    else
                        _this.LoadFallbackImage();
                }
                else if (e.OldValue != null && e.NewValue == null)
                {
                    // Value changed to null.
                    _this.LoadFallbackImage();
                }
            }
        }

        private static void OnUriFallbackSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageWithFallback _this)
            {
                if (_this._is_fallback && e.NewValue is Uri uri)
                {
                    // We're displaying the fall-back image and it changed. Reload it.
                    _this.LoadFallbackImage();
                }
            }
        }

        private void LoadImage(Uri uri)
        {
            // Set a blank image while waiting to load.
            Source = null;
            _is_fallback = false;

            // Spawn image loading as a background thread as this might be a time-consuming task and might block UI.
            var worker = new BackgroundWorker();
            worker.DoWork +=
                (object sender, DoWorkEventArgs e) =>
                {
                    try
                    {
                        // Download the specified image.
                        var request = WebRequest.Create(uri);
                        request.CachePolicy = _default_request_cache_policy;
                        request.Proxy = null;
                        using (var response = request.GetResponse())
                        using (var stream = response.GetResponseStream())
                        {
                            // Read to memory. BitmapImage doesn't fancy non-seekable streams.
                            using (var memory_stream = new MemoryStream())
                            {
                                var buffer = new byte[1048576];
                                for (;;)
                                {
                                    // Wait for the data to arrive.
                                    var buffer_length = stream.Read(buffer, 0, buffer.Length);
                                    if (buffer_length == 0)
                                        break;

                                    // Append it to the memory stream.
                                    memory_stream.Write(buffer, 0, buffer_length);
                                }

                                // Decode image.
                                var bitmap_image = new BitmapImage();
                                bitmap_image.BeginInit();
                                bitmap_image.StreamSource = memory_stream;
                                bitmap_image.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap_image.EndInit();
                                bitmap_image.Freeze();
                                e.Result = bitmap_image;
                            }
                        }
                    }
                    catch (Exception ex) { e.Result = ex; }
                };

            worker.RunWorkerCompleted +=
                (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    if (e.Result is BitmapImage bitmap_image)
                        Source = bitmap_image;
                    else
                        LoadFallbackImage();

                    worker.Dispose();
                };

            worker.RunWorkerAsync();
        }

        private void LoadFallbackImage()
        {
            if (UriFallbackSource != null)
            {
                // Load fall-back image. No need to load it in the background thread, since fall-back images should be locally and quickly loadable.
                var bitmap_image = new BitmapImage();
                bitmap_image.BeginInit();
                bitmap_image.UriCachePolicy = _default_request_cache_policy;
                bitmap_image.CacheOption = BitmapCacheOption.OnLoad;
                bitmap_image.UriSource = UriFallbackSource;
                bitmap_image.EndInit();
                bitmap_image.Freeze();

                Source = bitmap_image;
                _is_fallback = true;
            }
        }

        #endregion
    }
}
