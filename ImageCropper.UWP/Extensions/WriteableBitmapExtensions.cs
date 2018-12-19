using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ImageCropper.UWP.Extensions
{
    /// <summary>
    /// Provides some extension methods for WriteableBitmap.
    /// </summary>
    internal static class WriteableBitmapExtensions
    {
        internal static async Task<WriteableBitmap> GetCroppedImageAsync(this WriteableBitmap writeableBitmap,
            Rect croppedRect)
        {
            if (writeableBitmap == null)
            {
                return null;
            }
            var croppedBitmap = new WriteableBitmap((int)Math.Floor(croppedRect.Width),(int)Math.Floor(croppedRect.Height));
            using (var randomAccessStream = new InMemoryRandomAccessStream())
            {
                var bitmapEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
                await CropImageAsync(writeableBitmap, croppedRect, bitmapEncoder);
                croppedBitmap.SetSource(randomAccessStream);
            }
            return croppedBitmap;
        }

        internal static async Task CropImageAsync(WriteableBitmap writeableBitmap, Rect croppedRect, BitmapEncoder bitmapEncoder)
        {
            croppedRect.X = croppedRect.X > 0 ? croppedRect.X : 0;
            croppedRect.Y = croppedRect.Y > 0 ? croppedRect.Y : 0;
            var x = (uint)Math.Floor(croppedRect.X);
            var y = (uint)Math.Floor(croppedRect.Y);
            var width = (uint)Math.Floor(croppedRect.Width);
            var height = (uint)Math.Floor(croppedRect.Height);
            using (var sourceStream = writeableBitmap.PixelBuffer.AsStream())
            {
                var buffer = new byte[sourceStream.Length];
                await sourceStream.ReadAsync(buffer, 0, buffer.Length);
                bitmapEncoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)writeableBitmap.PixelWidth,
                    (uint)writeableBitmap.PixelHeight, 96.0, 96.0, buffer);
                bitmapEncoder.BitmapTransform.Bounds = new BitmapBounds
                {
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height
                };
                await bitmapEncoder.FlushAsync();
            }
        }
        internal static Guid GetEncoderId(BitmapFileFormat bitmapFileFormat)
        {
            switch (bitmapFileFormat)
            {
                case BitmapFileFormat.Bmp:
                    return BitmapEncoder.BmpEncoderId;
                case BitmapFileFormat.Png:
                    return BitmapEncoder.PngEncoderId;
                case BitmapFileFormat.Jpeg:
                    return BitmapEncoder.JpegEncoderId;
                case BitmapFileFormat.Tiff:
                    return BitmapEncoder.TiffEncoderId;
                case BitmapFileFormat.Gif:
                    return BitmapEncoder.GifEncoderId;
                case BitmapFileFormat.JpegXR:
                    return BitmapEncoder.JpegXREncoderId;
            }
            return BitmapEncoder.PngEncoderId;
        }
    }
}
