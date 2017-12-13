/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows;
using System.Windows.Media;

namespace QRCoder
{
    class XamlQRCode : AbstractQRCode<DrawingImage>, IDisposable
    {
        public XamlQRCode(QRCodeData data) : base(data) { }

        public override DrawingImage GetGraphic(int pixelsPerModule)
        {
            return this.GetGraphic(pixelsPerModule, true);
        }

        public DrawingImage GetGraphic(int pixelsPerModule, bool drawQuietZones)
        {
            var drawableModulesCount = this.QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8);
            var viewBox = new Size(pixelsPerModule * drawableModulesCount, pixelsPerModule * drawableModulesCount);
            return this.GetGraphic(viewBox, new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.White), drawQuietZones);
        }

        public DrawingImage GetGraphic(Size viewBox, bool drawQuietZones = true)
        {
            return this.GetGraphic(viewBox, new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.White), drawQuietZones);
        }

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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.QrCodeData != null)
                        this.QrCodeData.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
