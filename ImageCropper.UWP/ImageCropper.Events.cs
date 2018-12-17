using ImageCropper.UWP.Extensions;
using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace ImageCropper.UWP
{
    public partial class ImageCropper
    {
        private void ControlButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var changed = false;
            var diffPos = new Point();
            if (e.Key == VirtualKey.Left)
            {
                diffPos.X--;
                var upKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Up);
                var downKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Down);
                if (Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Up) == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y--;
                }
                if (Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Down) == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y++;
                }
                changed = true;
            }
            else if (e.Key == VirtualKey.Right)
            {
                diffPos.X++;
                var upKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Up);
                var downKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Down);
                if (upKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y--;
                }
                if (downKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y++;
                }
                changed = true;
            }
            else if (e.Key == VirtualKey.Up)
            {
                diffPos.Y--;
                var leftKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Left);
                var rightKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Right);
                if (leftKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X--;
                }
                if (rightKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X++;
                }
                changed = true;
            }
            else if (e.Key == VirtualKey.Down)
            {
                diffPos.Y++;
                var leftKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Left);
                var rightKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Right);
                if (leftKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X--;
                }
                if (rightKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X++;
                }
                changed = true;
            }

            if (changed)
            {
                var controlButton = (FrameworkElement)sender;
                var tag = controlButton.Tag;
                if (tag != null && Enum.TryParse(tag.ToString(), false, out DragPosition dragPosition))
                    UpdateCroppedRectWithAspectRatio(dragPosition, diffPos);
            }
        }

        private void ControlButton_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var inverseImageTransform = _imageTransform.Inverse;
            if (inverseImageTransform != null)
            {
                var selectedRect = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
                var croppedRect = inverseImageTransform.TransformBounds(selectedRect);
                if (croppedRect.Width > MinCropSize.Width && croppedRect.Height > MinCropSize.Height)
                {
                    croppedRect.Intersect(_restrictedCropRect);
                    _currentCroppedRect = croppedRect;
                }

                UpdateImageLayout(true);
            }
        }

        private void ControlButton_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var inverseImageTransform = _imageTransform.Inverse;
            if (inverseImageTransform != null)
            {
                var selectedRect = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
                var croppedRect = inverseImageTransform.TransformBounds(selectedRect);
                if (croppedRect.Width > MinCropSize.Width && croppedRect.Height > MinCropSize.Height)
                {
                    croppedRect.Intersect(_restrictedCropRect);
                    _currentCroppedRect = croppedRect;
                }

                UpdateImageLayout(true);
            }
        }

        private void ControlButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var controlButton = (FrameworkElement) sender;
            var dragButtomPosition = new Point(Canvas.GetLeft(controlButton), Canvas.GetTop(controlButton));
            var currentPointerPosition = new Point(
                dragButtomPosition.X + e.Position.X + e.Delta.Translation.X - controlButton.ActualWidth / 2,
                dragButtomPosition.Y + e.Position.Y + e.Delta.Translation.Y - controlButton.ActualHeight / 2);
            var safePosition = _restrictedSelectRect.GetSafePoint(currentPointerPosition);
            var safeDiffPoint = new Point(safePosition.X - dragButtomPosition.X, safePosition.Y - dragButtomPosition.Y);
            var tag = controlButton.Tag;
            if (tag != null && Enum.TryParse(tag.ToString(), false, out DragPosition dragPosition))
                UpdateCroppedRectWithAspectRatio(dragPosition, safeDiffPoint);
        }

        private void SourceImage_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var diffPos = e.Delta.Translation;
            var inverseImageTransform = _imageTransform.Inverse;
            if (inverseImageTransform != null)
            {
                var startPoint = new Point(_startX - diffPos.X, _startY - diffPos.Y);
                var endPoint = new Point(_endX - diffPos.X, _endY - diffPos.Y);
                if (_restrictedSelectRect.IsSafePoint(startPoint) && _restrictedSelectRect.IsSafePoint(endPoint))
                {
                    var selectedRect = new Rect(startPoint, endPoint);
                    if ((selectedRect.Width - MinSelectSize.Width) < -0.001 || (selectedRect.Height - MinSelectSize.Height) < -0.001)
                        return;
                    var movedRect = inverseImageTransform.TransformBounds(selectedRect);
                    movedRect.Intersect(_restrictedCropRect);
                    _currentCroppedRect = movedRect;
                    UpdateImageLayout();
                }
            }
        }

        private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SourceImage == null)
                return;
            UpdateImageLayout();
            UpdateMaskArea();
        }
    }
}