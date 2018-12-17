﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace ImageCropper.UWP
{
    public partial class ImageCropper
    {
        private TimeSpan animationDuration = TimeSpan.FromSeconds(0.2);

        private static void AnimateUIElementOffset(Point to, TimeSpan duration,UIElement target)
        {
            var targetVisual = ElementCompositionPreview.GetElementVisual(target);
            var compositor = targetVisual.Compositor;
            var linear = compositor.CreateLinearEasingFunction();
            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Duration = duration;
            offsetAnimation.Target = "Offset";
            offsetAnimation.InsertKeyFrame(1.0f, new Vector3((float)to.X, (float)to.Y, 0), linear);
            targetVisual.StartAnimation("Offset", offsetAnimation);
        }

        private static void AnimateUIElementScale(double to, TimeSpan duration, UIElement target)
        {
            var targetVisual = ElementCompositionPreview.GetElementVisual(target);
            var compositor = targetVisual.Compositor;
            var linear = compositor.CreateLinearEasingFunction();
            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.Duration = duration;
            scaleAnimation.Target = "Scale";
            scaleAnimation.InsertKeyFrame(1.0f, new Vector3((float)to), linear);
            targetVisual.StartAnimation("Scale", scaleAnimation);
        }

        private static DoubleAnimation CreateDoubleAnimation(double to, TimeSpan duration, DependencyObject target, string propertyName, bool enableDependentAnimation)
        {
            var animation = new DoubleAnimation()
            {
                To = to,
                Duration = duration,
                EnableDependentAnimation = enableDependentAnimation
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, propertyName);

            return animation;
        }

        private static PointAnimation CreatePointAnimation(Point to, TimeSpan duration, DependencyObject target, string propertyName, bool enableDependentAnimation)
        {
            var animation = new PointAnimation()
            {
                To = to,
                Duration = duration,
                EnableDependentAnimation = enableDependentAnimation
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, propertyName);

            return animation;
        }

        private static ObjectAnimationUsingKeyFrames CreateRectangleAnimation(Rect to, TimeSpan duration, RectangleGeometry rectangle, bool enableDependentAnimation)
        {
            var animation = new ObjectAnimationUsingKeyFrames
            {
                Duration = duration,
                EnableDependentAnimation = enableDependentAnimation
            };

            var frames = GetKeyframe(rectangle.Rect, to, duration);
            foreach (var item in frames)
            {
                animation.KeyFrames.Add(item);
            }

            Storyboard.SetTarget(animation, rectangle);
            Storyboard.SetTargetProperty(animation, "RectangleGeometry.Rect");

            return animation;
        }

        private static List<DiscreteObjectKeyFrame> GetKeyframe(Rect from, Rect to, TimeSpan duration)
        {
            var list = new List<DiscreteObjectKeyFrame>();
            var step = TimeSpan.FromMilliseconds(10);
            var total = duration.TotalMilliseconds;
            var startPointFrom = new Point(from.X, from.Y);
            var endPointFrom = new Point(from.X + from.Width, from.Y + from.Height);
            var startPointTo = new Point(to.X, to.Y);
            var endPointTo = new Point(to.X + to.Width, to.Y + to.Height);
            for (var i = new TimeSpan(); i < duration; i += step)
            {
                var progress = i.TotalMilliseconds / duration.TotalMilliseconds;
                list.Add(new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(i),
                    Value = new Rect(GetProgressPoint(progress, startPointFrom, startPointTo), GetProgressPoint(progress, endPointFrom, endPointTo))
                });
            }
            list.Add(new DiscreteObjectKeyFrame
            {
                KeyTime = duration,
                Value = to
            });
            return list;
        }
        private static Point GetProgressPoint(double normalizedProgress, Point from, Point to)
        {
            return new Point
            {
                X = from.X + normalizedProgress * (to.X - from.X),
                Y = from.Y + normalizedProgress * (to.Y - from.Y),
            };
        }
    }
}