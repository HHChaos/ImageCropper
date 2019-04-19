using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ImageCropper.UWP.Extensions
{
    /// <summary>
    /// Provides some extension methods for Rect.
    /// </summary>
    internal static class RectExtensions
    {
        /// <summary>
        /// Gets the closest point in the rectangle to a given point.
        /// </summary>
        /// <param name="targetRect">The rectangle.</param>
        /// <param name="point">The test point.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Test whether the point is in the rectangle.
        /// Similar to the <see cref="Rect.Contains"/> method, this method adds redundancy.
        /// </summary>
        /// <param name="targetRect">the rectangle.</param>
        /// <param name="point">The test point.</param>
        /// <returns></returns>
        public static bool IsSafePoint(this Rect targetRect, Point point)
        {
            if (point.X - targetRect.X < -0.001)
                return false;
            if (point.X - (targetRect.X + targetRect.Width) > 0.001)
                return false;
            if (point.Y - targetRect.Y < -0.001)
                return false;
            if (point.Y - (targetRect.Y + targetRect.Height) > 0.001)
                return false;
            return true;
        }

        /// <summary>
        /// Determines whether a rectangle satisfies the minimum size limit.
        /// </summary>
        /// <param name="startPoint">The point on the upper left corner.</param>
        /// <param name="endPoint">The point on the lower right corner.</param>
        /// <param name="minSize">The minimum size.</param>
        /// <returns></returns>
        public static bool IsSafeRect(Point startPoint, Point endPoint, Size minSize)
        {
            var checkPoint = new Point(startPoint.X + minSize.Width, startPoint.Y + minSize.Height);
            return checkPoint.X - endPoint.X < 0.001
                   && checkPoint.Y - endPoint.Y < 0.001;
        }
        /// <summary>
        /// Gets a rectangle with a minimum size limit.
        /// </summary>
        /// <param name="startPoint">The point on the upper left corner.</param>
        /// <param name="endPoint">The point on the lower right corner.</param>
        /// <param name="minSize">The minimum size.</param>
        /// <param name="dragPosition">The control point.</param>
        /// <returns></returns>
        public static Rect GetSafeRect(Point startPoint, Point endPoint, Size minSize, DragPosition dragPosition)
        {
            var checkPoint = new Point(startPoint.X + minSize.Width, startPoint.Y + minSize.Height);
            switch (dragPosition)
            {
                case DragPosition.Top:
                    if (checkPoint.Y > endPoint.Y) startPoint.Y = endPoint.Y - minSize.Height;
                    break;
                case DragPosition.Bottom:
                    if (checkPoint.Y > endPoint.Y) endPoint.Y = startPoint.Y + minSize.Height;
                    break;
                case DragPosition.Left:
                    if (checkPoint.X > endPoint.X) startPoint.X = endPoint.X - minSize.Width;
                    break;
                case DragPosition.Right:
                    if (checkPoint.X > endPoint.X) endPoint.X = startPoint.X + minSize.Width;
                    break;
                case DragPosition.UpperLeft:
                    if (checkPoint.X > endPoint.X) startPoint.X = endPoint.X - minSize.Width;
                    if (checkPoint.Y > endPoint.Y) startPoint.Y = endPoint.Y - minSize.Height;
                    break;
                case DragPosition.UpperRight:
                    if (checkPoint.X > endPoint.X) endPoint.X = startPoint.X + minSize.Width;
                    if (checkPoint.Y > endPoint.Y) startPoint.Y = endPoint.Y - minSize.Height;
                    break;
                case DragPosition.LowerLeft:
                    if (checkPoint.X > endPoint.X) startPoint.X = endPoint.X - minSize.Width;
                    if (checkPoint.Y > endPoint.Y) endPoint.Y = startPoint.Y + minSize.Height;
                    break;
                case DragPosition.LowerRight:
                    if (checkPoint.X > endPoint.X) endPoint.X = startPoint.X + minSize.Width;
                    if (checkPoint.Y > endPoint.Y) endPoint.Y = startPoint.Y + minSize.Height;
                    break;
            }

            return new Rect(startPoint, endPoint);
        }

        /// <summary>
        /// Gets the maximum rectangle embedded in the rectangle by a given aspect ratio.
        /// </summary>
        /// <param name="targetRect">The rectangle.</param>
        /// <param name="aspectRatio">The aspect ratio.</param>
        /// <returns></returns>
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

        public static bool IsValid(this Rect targetRect)
        {
            return !targetRect.IsEmpty && targetRect.Width > 0 && targetRect.Height > 0;
        }

        public static bool CanContains(this Rect targetRect, Rect testRect)
        {
            return targetRect.Width - testRect.Width > -0.001 && targetRect.Height - testRect.Height > -0.001;
        }

        public static bool GetContainsRect(this Rect targetRect, ref Rect testRect)
        {
            if (!targetRect.CanContains(testRect))
            {
                return false;
            }
            if (targetRect.Left > testRect.Left)
            {
                testRect.X += targetRect.Left - testRect.Left;
            }
            if (targetRect.Top > testRect.Top)
            {
                testRect.Y += targetRect.Top - testRect.Top;
            }
            if (targetRect.Right < testRect.Right)
            {
                testRect.X += targetRect.Right - testRect.Right;
            }
            if (targetRect.Bottom < testRect.Bottom)
            {
                testRect.Y += targetRect.Bottom - testRect.Bottom;
            }
            return true;
        }
        public static Point GetSafeSizeChangeWhenKeepAspectRatio(this Rect targetRect, DragPosition dragPosition, Rect selectedRect, Point originSizeChange, double aspectRatio)
        {
            var safeWidthChange = originSizeChange.X;
            var safeHeightChange = originSizeChange.Y;
            var maxWidthChange = 0d;
            var maxHeightChange = 0d;
            switch (dragPosition)
            {
                case DragPosition.Top:
                    maxWidthChange = targetRect.Width - selectedRect.Width;
                    maxHeightChange = selectedRect.Top - targetRect.Top;
                    break;
                case DragPosition.Bottom:
                    maxWidthChange = targetRect.Width - selectedRect.Width;
                    maxHeightChange = targetRect.Bottom - selectedRect.Bottom;
                    break;
                case DragPosition.Left:
                    maxWidthChange = selectedRect.Left - targetRect.Left;
                    maxHeightChange = targetRect.Height - selectedRect.Height;
                    break;
                case DragPosition.Right:
                    maxWidthChange = targetRect.Right - selectedRect.Right;
                    maxHeightChange = targetRect.Height - selectedRect.Height;
                    break;
                case DragPosition.UpperLeft:
                    maxWidthChange = selectedRect.Left - targetRect.Left;
                    maxHeightChange = selectedRect.Top - targetRect.Top;
                    break;
                case DragPosition.UpperRight:
                    maxWidthChange = targetRect.Right - selectedRect.Right;
                    maxHeightChange = selectedRect.Top - targetRect.Top;
                    break;
                case DragPosition.LowerLeft:
                    maxWidthChange = selectedRect.Left - targetRect.Left;
                    maxHeightChange = targetRect.Bottom - selectedRect.Bottom;
                    break;
                case DragPosition.LowerRight:
                    maxWidthChange = targetRect.Right - selectedRect.Right;
                    maxHeightChange = targetRect.Bottom - selectedRect.Bottom;
                    break;
            }
            if (originSizeChange.X > maxWidthChange)
            {
                safeWidthChange = maxWidthChange;
                safeHeightChange = safeWidthChange / aspectRatio;
            }
            if (originSizeChange.Y > maxHeightChange)
            {
                safeHeightChange = maxHeightChange;
                safeWidthChange = safeHeightChange * aspectRatio;
            }
            return new Point(safeWidthChange, safeHeightChange);
        }
    }
}
