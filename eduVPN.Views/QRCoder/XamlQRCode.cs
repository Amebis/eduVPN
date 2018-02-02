/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows;
using System.Windows.Media;

namespace QRCoder
{
    /// <summary>
    /// XAML QR code generator
    /// </summary>
    public class XamlQRCode : AbstractQRCode<DrawingImage>, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Constructs a QR generator
        /// </summary>
        /// <param name="data">QR code data</param>
        public XamlQRCode(QRCodeData data) :
            base(data)
        { }

        #endregion

        #region Methods

        /// <summary>
        /// Get QR graphics data
        /// </summary>
        /// <param name="pixelsPerModule">Width and height in px of each QR module</param>
        /// <returns>Data</returns>
        /// <remarks>
        /// <see cref="Colors.Black"/> solid brush is used for dark QR modules.
        /// <see cref="Colors.White"/> solid brush is used for light QR modules.
        /// The QR graphics will contain light margin (quiet zone).
        /// </remarks>
        public override DrawingImage GetGraphic(int pixelsPerModule)
        {
            return GetGraphic(pixelsPerModule, true);
        }

        /// <summary>
        /// Get QR graphics data
        /// </summary>
        /// <param name="pixelsPerModule">Width and height in px of each QR module</param>
        /// <param name="drawQuietZones">Should QR graphics contain light margin (quiet zone)?</param>
        /// <returns>Data</returns>
        /// <remarks>
        /// <see cref="Colors.Black"/> solid brush is used for dark QR modules.
        /// <see cref="Colors.White"/> solid brush is used for light QR modules.
        /// </remarks>
        public DrawingImage GetGraphic(int pixelsPerModule, bool drawQuietZones)
        {
            var drawableModulesCount = this.QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8);
            var viewBox = new Size(pixelsPerModule * drawableModulesCount, pixelsPerModule * drawableModulesCount);
            return GetGraphic(viewBox, new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.White), drawQuietZones);
        }

        /// <summary>
        /// Get QR graphics data
        /// </summary>
        /// <param name="viewBox">Final size of QR graphics</param>
        /// <param name="drawQuietZones">Should QR graphics contain light margin (quiet zone)?</param>
        /// <returns>Data</returns>
        /// <remarks>
        /// <see cref="Colors.Black"/> solid brush is used for dark QR modules.
        /// <see cref="Colors.White"/> solid brush is used for light QR modules.
        /// </remarks>
        public DrawingImage GetGraphic(Size viewBox, bool drawQuietZones = true)
        {
            return GetGraphic(viewBox, new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.White), drawQuietZones);
        }

        /// <summary>
        /// Get QR graphics data
        /// </summary>
        /// <param name="viewBox">Final size of QR graphics</param>
        /// <param name="darkBrush">Brush used for dark QR modules</param>
        /// <param name="lightBrush">Brush used for light QR modules</param>
        /// <param name="drawQuietZones">Should QR graphics contain light margin (quiet zone)?</param>
        /// <returns>Data</returns>
        public DrawingImage GetGraphic(Size viewBox, Brush darkBrush, Brush lightBrush, bool drawQuietZones = true)
        {
            var drawableModulesCount = this.QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8);
            var qrSize = Math.Min(viewBox.Width, viewBox.Height);
            var unitsPerModule = qrSize / drawableModulesCount;
            var offsetModules = drawQuietZones ? 0 : 4;

            DrawingGroup drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing(lightBrush, null, new RectangleGeometry(new Rect(new Point(0, 0), new Size(qrSize, qrSize)))));

            var group = new GeometryGroup();
            int xi = 0, yi = 0;
            for (var x = 0d; x < qrSize; x = x + unitsPerModule)
            {
                yi = 0;
                for (var y = 0d; y < qrSize; y = y + unitsPerModule)
                {
                    if (this.QrCodeData.ModuleMatrix[yi + offsetModules][xi + offsetModules])
                    {
                        group.Children.Add(new RectangleGeometry(new Rect(x, y, unitsPerModule, unitsPerModule)));
                    }
                    yi++;
                }
                xi++;
            }
            drawing.Children.Add(new GeometryDrawing(darkBrush, null, group));

            return new DrawingImage(drawing);
        }

        #endregion

        #region IDisposable Support
        /// <summary>
        /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Called to dispose the object.
        /// </summary>
        /// <param name="disposing">Dispose managed objects</param>
        /// <remarks>
        /// To release resources for inherited classes, override this method.
        /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
        /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (QrCodeData != null)
                        QrCodeData.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
        /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
