using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ImageCropper.UWP.Helpers
{
    public static class WriteableBitmapExtensions
    {
        public static async Task<WriteableBitmap> GetCroppedBitmapAsync(this WriteableBitmap writeableBitmap,
            Rect croppedRect)
        {
            var x = (uint) Math.Floor(croppedRect.X);
            var y = (uint) Math.Floor(croppedRect.Y);
            var width = (uint) Math.Floor(croppedRect.Width);
            var height = (uint) Math.Floor(croppedRect.Height);
            WriteableBitmap croppedBitmap;
            var sourceStream = writeableBitmap.PixelBuffer.AsStream();
            var buffer = new byte[sourceStream.Length];
            await sourceStream.ReadAsync(buffer, 0, buffer.Length);
            using (var memoryRandom = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryRandom);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint) writeableBitmap.PixelWidth,
                    (uint) writeableBitmap.PixelHeight, 96.0, 96.0, buffer);
                encoder.BitmapTransform.Bounds = new BitmapBounds
                {
                    X = x,
                    Y = y,
                    Height = height,
                    Width = width
                };
                await encoder.FlushAsync();
                croppedBitmap = new WriteableBitmap((int) encoder.BitmapTransform.Bounds.Width,
                    (int) encoder.BitmapTransform.Bounds.Height);
                croppedBitmap.SetSource(memoryRandom);
            }
            return croppedBitmap;
        }
    }
}
