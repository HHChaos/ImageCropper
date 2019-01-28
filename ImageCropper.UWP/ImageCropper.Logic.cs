using ImageCropper.UWP.Extensions;
using System;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace ImageCropper.UWP
{
    public partial class ImageCropper
    {
        /// <summary>
        /// Initializes image source transform.
        /// </summary>
        private void InitImageLayout(bool animate = false)
        {
            if (SourceImage != null)
            {
                _restrictedCropRect = new Rect(0, 0, SourceImage.PixelWidth, SourceImage.PixelHeight);
                if (_restrictedCropRect.IsValid())
                {
                    _currentCroppedRect = KeepAspectRatio ? _restrictedCropRect.GetUniformRect(UsedAspectRatio) : _restrictedCropRect;
                    UpdateCropShape();
                    UpdateImageLayout(animate);
                }
            }
            else
            {
                _currentCroppedRect = Rect.Empty;
                _restrictedCropRect = Rect.Empty;
                _restrictedSelectRect = Rect.Empty;
            }
            UpdateControlButtonVisibility();
        }

        /// <summary>
        /// Update image source transform.
        /// </summary>
        private void UpdateImageLayout(bool animate = false)
        {
            if (SourceImage != null && CanvasRect.IsValid())
            {
                var uniformSelectedRect = CanvasRect.GetUniformRect(_currentCroppedRect.Width / _currentCroppedRect.Height);
                UpdateImageLayoutWithViewport(uniformSelectedRect, _currentCroppedRect, animate);
            }
        }

        /// <summary>
        /// Update image source transform.
        /// </summary>
        /// <param name="viewport">Viewport</param>
        /// <param name="viewportImageRect"> The real image area of viewport.</param>
        private void UpdateImageLayoutWithViewport(Rect viewport, Rect viewportImageRect, bool animate = false)
        {
            if (!viewport.IsValid() || !viewportImageRect.IsValid())
                return;
            var imageScale = viewport.Width / viewportImageRect.Width;
            _imageTransform.ScaleX = _imageTransform.ScaleY = imageScale;
            _imageTransform.TranslateX = viewport.X - viewportImageRect.X * imageScale;
            _imageTransform.TranslateY = viewport.Y - viewportImageRect.Y * imageScale;
            _inverseImageTransform.ScaleX = _inverseImageTransform.ScaleY = 1 / imageScale;
            _inverseImageTransform.TranslateX = -_imageTransform.TranslateX / imageScale;
            _inverseImageTransform.TranslateY = -_imageTransform.TranslateY / imageScale;
            var selectedRect = _imageTransform.TransformBounds(_currentCroppedRect);
            _restrictedSelectRect = _imageTransform.TransformBounds(_restrictedCropRect);
            var startPoint = _restrictedSelectRect.GetSafePoint(new Point(selectedRect.X, selectedRect.Y));
            var endPoint = _restrictedSelectRect.GetSafePoint(new Point(selectedRect.X + selectedRect.Width,
                selectedRect.Y + selectedRect.Height));
            if (animate)
            {
                AnimateUIElementOffset(new Point(_imageTransform.TranslateX, _imageTransform.TranslateY), _animationDuration, _sourceImage);
                AnimateUIElementScale(imageScale, _animationDuration, _sourceImage);
            }
            else
            {
                var targetVisual = ElementCompositionPreview.GetElementVisual(_sourceImage);
                targetVisual.Offset = new Vector3((float)_imageTransform.TranslateX, (float)_imageTransform.TranslateY, 0);
                targetVisual.Scale = new Vector3((float)imageScale);
            }
            UpdateSelectedRect(startPoint, endPoint, animate);
        }

        /// <summary>
        /// Update cropped area.
        /// </summary>
        /// <param name="dragPosition">The control point</param>
        /// <param name="diffPos">Position offset</param>
        private void UpdateCroppedRectWithAspectRatio(DragPosition dragPosition, Point diffPos)
        {
            if (diffPos == default(Point) || !CanvasRect.IsValid())
            {
                return;
            }

            double radian = 0d, diffPointRadian = 0d, effectiveLength = 0d;
            if (KeepAspectRatio)
            {
                radian = Math.Atan(UsedAspectRatio);
                diffPointRadian = Math.Atan(diffPos.X / diffPos.Y);
            }

            var startPoint = new Point(_startX, _startY);
            var endPoint = new Point(_endX, _endY);
            switch (dragPosition)
            {
                case DragPosition.Top:
                    startPoint.Y += diffPos.Y;
                    if (KeepAspectRatio)
                    {
                        var changeX = diffPos.Y * UsedAspectRatio;
                        startPoint.X += changeX / 2;
                        endPoint.X -= changeX / 2;
                    }

                    break;
                case DragPosition.Bottom:
                    endPoint.Y += diffPos.Y;
                    if (KeepAspectRatio)
                    {
                        var changeX = diffPos.Y * UsedAspectRatio;
                        startPoint.X -= changeX / 2;
                        endPoint.X += changeX / 2;
                    }

                    break;
                case DragPosition.Left:
                    startPoint.X += diffPos.X;
                    if (KeepAspectRatio)
                    {
                        var changeY = diffPos.X / UsedAspectRatio;
                        startPoint.Y += changeY / 2;
                        endPoint.Y -= changeY / 2;
                    }

                    break;
                case DragPosition.Right:
                    endPoint.X += diffPos.X;
                    if (KeepAspectRatio)
                    {
                        var changeY = diffPos.X / UsedAspectRatio;
                        startPoint.Y -= changeY / 2;
                        endPoint.Y += changeY / 2;
                    }

                    break;
                case DragPosition.UpperLeft:
                    if (KeepAspectRatio)
                    {
                        effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        diffPos.X = effectiveLength * Math.Sin(radian);
                        diffPos.Y = effectiveLength * Math.Cos(radian);
                    }

                    startPoint.X += diffPos.X;
                    startPoint.Y += diffPos.Y;
                    break;
                case DragPosition.UpperRight:
                    if (KeepAspectRatio)
                    {
                        diffPointRadian = -diffPointRadian;
                        effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        diffPos.X = -effectiveLength * Math.Sin(radian);
                        diffPos.Y = effectiveLength * Math.Cos(radian);
                    }

                    endPoint.X += diffPos.X;
                    startPoint.Y += diffPos.Y;
                    break;
                case DragPosition.LowerLeft:
                    if (KeepAspectRatio)
                    {
                        diffPointRadian = -diffPointRadian;
                        effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        diffPos.X = -effectiveLength * Math.Sin(radian);
                        diffPos.Y = effectiveLength * Math.Cos(radian);
                    }

                    startPoint.X += diffPos.X;
                    endPoint.Y += diffPos.Y;
                    break;
                case DragPosition.LowerRight:
                    if (KeepAspectRatio)
                    {
                        effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        diffPos.X = effectiveLength * Math.Sin(radian);
                        diffPos.Y = effectiveLength * Math.Cos(radian);
                    }

                    endPoint.X += diffPos.X;
                    endPoint.Y += diffPos.Y;
                    break;
            }

            if (!RectExtensions.IsSafeRect(startPoint, endPoint, MinSelectSize))
            {
                if (KeepAspectRatio)
                {
                    if ((endPoint.Y - startPoint.Y) < (_endY - _startY) ||
                        (endPoint.X - startPoint.X) < (_endX - _startX))
                    {
                        return;
                    }

                }
                else
                {
                    var safeRect = RectExtensions.GetSafeRect(startPoint, endPoint, MinSelectSize, dragPosition);
                    safeRect.Intersect(_restrictedSelectRect);
                    startPoint = new Point(safeRect.X, safeRect.Y);
                    endPoint = new Point(safeRect.X + safeRect.Width, safeRect.Y + safeRect.Height);
                }
            }

            var isEffectiveRegion = _restrictedSelectRect.IsSafePoint(startPoint) &&
                                    _restrictedSelectRect.IsSafePoint(endPoint);
            if (!isEffectiveRegion) return;
            var selectedRect = new Rect(startPoint, endPoint);
            selectedRect.Union(CanvasRect);
            if (selectedRect != CanvasRect)
            {
                var croppedRect = _inverseImageTransform.TransformBounds(
                    new Rect(startPoint, endPoint));
                croppedRect.Intersect(_restrictedCropRect);
                _currentCroppedRect = croppedRect;
                var viewportRect = CanvasRect.GetUniformRect(selectedRect.Width / selectedRect.Height);
                var viewportImgRect = _inverseImageTransform.TransformBounds(selectedRect);
                UpdateImageLayoutWithViewport(viewportRect, viewportImgRect);
            }
            else
            {
                UpdateSelectedRect(startPoint, endPoint);
            }
        }

        /// <summary>
        /// Update selection area.
        /// </summary>
        /// <param name="startPoint">The point on the upper left corner.</param>
        /// <param name="endPoint">The point on the lower right corner.</param>
        private void UpdateSelectedRect(Point startPoint, Point endPoint, bool animate = false)
        {
            _startX = startPoint.X;
            _startY = startPoint.Y;
            _endX = endPoint.X;
            _endY = endPoint.Y;
            var centerX = (_endX - _startX) / 2 + _startX;
            var centerY = (_endY - _startY) / 2 + _startY;
            if (_topButton != null)
            {
                UpdateThumbPosition(_topButton, new Point(centerX, _startY), animate);
            }

            if (_bottomButton != null)
            {
                UpdateThumbPosition(_bottomButton, new Point(centerX, _endY), animate);
            }

            if (_leftButton != null)
            {
                UpdateThumbPosition(_leftButton, new Point(_startX, centerY), animate);
            }

            if (_rigthButton != null)
            {
                UpdateThumbPosition(_rigthButton, new Point(_endX, centerY), animate);
            }

            if (_upperLeftButton != null)
            {
                UpdateThumbPosition(_upperLeftButton, new Point(_startX, _startY), animate);
            }

            if (_upperRightButton != null)
            {
                UpdateThumbPosition(_upperRightButton, new Point(_endX, _startY), animate);
            }

            if (_lowerLeftButton != null)
            {
                UpdateThumbPosition(_lowerLeftButton, new Point(_startX, _endY), animate);
            }

            if (_lowerRigthButton != null)
            {
                UpdateThumbPosition(_lowerRigthButton, new Point(_endX, _endY), animate);
            }

            UpdateMaskArea(animate);
        }

        private void UpdateThumbPosition(UIElement target, Point position, bool animate = false)
        {
            if (animate)
            {
                var storyboard = new Storyboard();
                storyboard.Children.Add(CreateDoubleAnimation(position.X, _animationDuration, target, "(Canvas.Left)", false));
                storyboard.Children.Add(CreateDoubleAnimation(position.Y, _animationDuration, target, "(Canvas.Top)", false));
                storyboard.Begin();
            }
            else
            {
                Canvas.SetLeft(target, position.X);
                Canvas.SetTop(target, position.Y);
            }
        }

        /// <summary>
        /// Update the mask layer.
        /// </summary>
        private void UpdateMaskArea(bool animate = false)
        {
            if (_layoutGrid == null || _maskAreaGeometryGroup.Children.Count < 2)
                return;
            _outerGeometry.Rect = new Rect(-_layoutGrid.Padding.Left, -_layoutGrid.Padding.Top, _layoutGrid.ActualWidth,
                                    _layoutGrid.ActualHeight);

            if (CircularCrop)
            {
                if (_innerGeometry is EllipseGeometry ellipseGeometry)
                {
                    var center = new Point((_endX - _startX) / 2 + _startX, (_endY - _startY) / 2 + _startY);
                    var radiusX = (_endX - _startX) / 2;
                    var radiusY = (_endY - _startY) / 2;
                    if (animate)
                    {
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(CreatePointAnimation(center, _animationDuration, ellipseGeometry, "EllipseGeometry.Center", true));
                        storyboard.Children.Add(CreateDoubleAnimation(radiusX, _animationDuration, ellipseGeometry, "EllipseGeometry.RadiusX", true));
                        storyboard.Children.Add(CreateDoubleAnimation(radiusY, _animationDuration, ellipseGeometry, "EllipseGeometry.RadiusY", true));
                        storyboard.Begin();
                    }
                    else
                    {
                        ellipseGeometry.Center = center;
                        ellipseGeometry.RadiusX = radiusX;
                        ellipseGeometry.RadiusY = radiusY;
                    }

                }
            }
            else
            {
                if (_innerGeometry is RectangleGeometry rectangleGeometry)
                {
                    var to = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
                    if (animate)
                    {
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(CreateRectangleAnimation(to, _animationDuration, rectangleGeometry, true));
                        storyboard.Begin();
                    }
                    else
                    {
                        rectangleGeometry.Rect = to;
                    }

                }
            }
            _layoutGrid.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, _layoutGrid.ActualWidth,
                    _layoutGrid.ActualHeight)
            };

        }

        private void UpdateCropShape()
        {
            _maskAreaGeometryGroup.Children.Clear();
            _outerGeometry = new RectangleGeometry();
            if (CircularCrop)
            {
                _innerGeometry = new EllipseGeometry();
            }
            else
            {
                _innerGeometry = new RectangleGeometry();
            }
            _maskAreaGeometryGroup.Children.Add(_outerGeometry);
            _maskAreaGeometryGroup.Children.Add(_innerGeometry);
            UpdateMaskArea();
        }

        /// <summary>
        /// Update image aspect ratio.
        /// </summary>
        private void UpdateAspectRatio(bool animate = false)
        {
            if (KeepAspectRatio && SourceImage != null && _restrictedSelectRect.IsValid())
            {
                var centerX = (_endX - _startX) / 2 + _startX;
                var centerY = (_endY - _startY) / 2 + _startY;
                var restrictedMinLength = MinCroppedPixelLength * _imageTransform.ScaleX;
                var maxSelectedLength = Math.Max(_endX - _startX, _endY - _startY);
                var viewRect = new Rect(centerX - maxSelectedLength / 2, centerY - maxSelectedLength / 2, maxSelectedLength, maxSelectedLength);
                var uniformSelectedRect = viewRect.GetUniformRect(UsedAspectRatio);
                if (uniformSelectedRect.Width > _restrictedSelectRect.Width || uniformSelectedRect.Height > _restrictedSelectRect.Height)
                {
                    uniformSelectedRect = _restrictedSelectRect.GetUniformRect(UsedAspectRatio);
                }
                if (uniformSelectedRect.Width < restrictedMinLength || uniformSelectedRect.Height < restrictedMinLength)
                {
                    var scale = restrictedMinLength / Math.Min(uniformSelectedRect.Width, uniformSelectedRect.Height);
                    uniformSelectedRect.Width *= scale;
                    uniformSelectedRect.Height *= scale;
                    if (uniformSelectedRect.Width > _restrictedSelectRect.Width || uniformSelectedRect.Height > _restrictedSelectRect.Height)
                    {
                        AspectRatio = -1;
                        return;
                    }
                }
                if (_restrictedSelectRect.X > uniformSelectedRect.X)
                {
                    uniformSelectedRect.X += _restrictedSelectRect.X - uniformSelectedRect.X;
                }
                if (_restrictedSelectRect.Y > uniformSelectedRect.Y)
                {
                    uniformSelectedRect.Y += _restrictedSelectRect.Y - uniformSelectedRect.Y;
                }
                if ((_restrictedSelectRect.X + _restrictedSelectRect.Width) < (uniformSelectedRect.X + uniformSelectedRect.Width))
                {
                    uniformSelectedRect.X += (_restrictedSelectRect.X + _restrictedSelectRect.Width) - (uniformSelectedRect.X + uniformSelectedRect.Width);
                }
                if ((_restrictedSelectRect.Y + _restrictedSelectRect.Height) < (uniformSelectedRect.Y + uniformSelectedRect.Height))
                {
                    uniformSelectedRect.Y += (_restrictedSelectRect.Y + _restrictedSelectRect.Height) - (uniformSelectedRect.Y + uniformSelectedRect.Height);
                }
                var croppedRect = _inverseImageTransform.TransformBounds(uniformSelectedRect);
                croppedRect.Intersect(_restrictedCropRect);
                _currentCroppedRect = croppedRect;
                UpdateImageLayout(animate);
            }
        }

        /// <summary>
        /// Update the visibility of the control button.
        /// </summary>
        private void UpdateControlButtonVisibility()
        {
            var cornerBtnVisibility = CircularCrop ? Visibility.Collapsed : Visibility.Visible;
            var otherBtnVisibility = (CircularCrop || IsSecondaryControlButtonVisible)
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (SourceImage == null)
                cornerBtnVisibility = otherBtnVisibility = Visibility.Collapsed;

            if (_topButton != null)
                _topButton.Visibility = otherBtnVisibility;
            if (_bottomButton != null)
                _bottomButton.Visibility = otherBtnVisibility;
            if (_leftButton != null)
                _leftButton.Visibility = otherBtnVisibility;
            if (_rigthButton != null)
                _rigthButton.Visibility = otherBtnVisibility;
            if (_upperLeftButton != null)
                _upperLeftButton.Visibility = cornerBtnVisibility;
            if (_upperRightButton != null)
                _upperRightButton.Visibility = cornerBtnVisibility;
            if (_lowerLeftButton != null)
                _lowerLeftButton.Visibility = cornerBtnVisibility;
            if (_lowerRigthButton != null)
                _lowerRigthButton.Visibility = cornerBtnVisibility;
        }
    }
}
