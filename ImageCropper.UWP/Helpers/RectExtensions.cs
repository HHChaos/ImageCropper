using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ImageCropper.UWP.Helpers
{
    public static class RectExtensions
    {
        public static Point GetSafePoint(this Rect targetRect, Point point)
        {
            var safePoint = new Point(point.X, point.Y);
            if (safePoint.X < targetRect.X)
                safePoint.X = targetRect.X;
            if (safePoint.X > targetRect.X + targetRect.Width)
                safePoint.X = targetRect.X + targetRect.Width;
            if (safePoint.Y < targetRect.Y)
                safePoint.Y = targetRect.Y;
            if (safePoint.Y > targetRect.Y + targetRect.Height)
                safePoint.Y = targetRect.Y + targetRect.Height;
            return safePoint;
        }

        public static Rect GetUniformRect(this Rect targetRect, double aspectRatio)
        {
            var ratio = targetRect.Width / targetRect.Height;
            var cx = targetRect.X + targetRect.Width / 2;
            var cy = targetRect.Y + targetRect.Height / 2;
            double width, height;
            if (aspectRatio > ratio)
            {
                width = targetRect.Width;
                height = width / aspectRatio;
            }
            else
            {
                height = targetRect.Height;
                width = height * aspectRatio;
            }

            return new Rect(cx - width / 2f, cy - height / 2f, width, height);
        }
    }
}
