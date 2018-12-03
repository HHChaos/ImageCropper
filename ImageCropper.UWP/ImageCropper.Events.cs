using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using ImageCropper.UWP.Helpers;

namespace ImageCropper.UWP
{
    public partial class ImageCropper
    {
        private void DragButton_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
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

                UpdateImageLayout();
            }
        }

        private void DragButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var dragButtom = (FrameworkElement) sender;
            var dragButtomPosition = new Point(Canvas.GetLeft(dragButtom), Canvas.GetTop(dragButtom));
            var currentPointerPosition = new Point(dragButtomPosition.X + e.Position.X + e.Delta.Translation.X,
                dragButtomPosition.Y + e.Position.Y + e.Delta.Translation.Y);
            var safePosition = _restrictedSelectRect.GetSafePoint(currentPointerPosition);
            var safeDiffPoint = new Point(safePosition.X - dragButtomPosition.X, safePosition.Y - dragButtomPosition.Y);
            var tag = dragButtom.Tag;
            if (tag != null && Enum.TryParse(tag.ToString(), false, out DragPoint dragPoint))
                UpdateCroppedRectWithAspectRatio(dragPoint, safeDiffPoint);
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
                    if (selectedRect.Width < MinSelectSize.Width || selectedRect.Height < MinSelectSize.Height)
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