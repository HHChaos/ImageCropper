using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using ImageCropper.UWP.Helpers;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace ImageCropper.UWP
{
    [TemplatePart(Name = LayoutGridName, Type = typeof(Grid))]
    [TemplatePart(Name = ImageCanvasPartName, Type = typeof(Canvas))]
    [TemplatePart(Name = SourceImagePartName, Type = typeof(Image))]
    [TemplatePart(Name = MaskAreaPathPartName, Type = typeof(Path))]
    [TemplatePart(Name = TopButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = BottomButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = LeftButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = RightButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = UpperLeftButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = UpperRightButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = LowerLeftButtonPartName, Type = typeof(Button))]
    [TemplatePart(Name = LowerRightButtonPartName, Type = typeof(Button))]
    public partial class ImageCropper : Control
    {
        private Grid _layoutGrid;
        private Canvas _imageCanvas;
        private Image _sourceImage;
        private Path _maskAreaPath;
        private Button _topButton;
        private Button _bottomButton;
        private Button _leftButton;
        private Button _rigthButton;
        private Button _upperLeftButton;
        private Button _upperRightButton;
        private Button _lowerLeftButton;
        private Button _lowerRigthButton;
        private double _startX;
        private double _startY;
        private double _endX = 20d;
        private double _endY = 20d;
        private readonly CompositeTransform _imageTransform = new CompositeTransform();
        private readonly GeometryGroup _maskAreaGeometryGroup = new GeometryGroup {FillRule = FillRule.EvenOdd};
        private Rect _currentCroppedRect = Rect.Empty;
        private Rect _restrictedCropRect = Rect.Empty;
        private Rect _restrictedSelectRect = Rect.Empty;

        public ImageCropper()
        {
            DefaultStyleKey = typeof(ImageCropper);
        }

        private Rect CanvasRect => new Rect(0, 0, _imageCanvas.ActualWidth, _imageCanvas.ActualHeight);
        private bool KeepAspectRatio => UsedAspectRatio > 0;
        private double UsedAspectRatio => RoundedCrop ? 1 : AspectRatio;

        private Size MinCropSize
        {
            get
            {
                var aspectRatio = KeepAspectRatio ? UsedAspectRatio : 1;
                var size = new Size(MinCroppedPixelLength, MinCroppedPixelLength);
                if (aspectRatio >= 1)
                    size.Width = size.Height * aspectRatio;
                else
                    size.Height = size.Width / aspectRatio;
                return size;
            }
        }

        private Size MinSelectSize
        {
            get
            {
                var realMinSelectSize = _imageTransform.TransformBounds(new Rect(new Point(), MinCropSize));
                var minLength = Math.Min(realMinSelectSize.Width, realMinSelectSize.Height);
                if (minLength < MinSelectedLength)
                {
                    var aspectRatio = KeepAspectRatio ? UsedAspectRatio : 1;
                    var minSelectSize = new Size(MinSelectedLength, MinSelectedLength);
                    if (aspectRatio >= 1)
                        minSelectSize.Width = minSelectSize.Height * aspectRatio;
                    else
                        minSelectSize.Height = minSelectSize.Width / aspectRatio;
                    return minSelectSize;
                }

                return new Size(realMinSelectSize.Width, realMinSelectSize.Height);
            }
        }

        protected override void OnApplyTemplate()
        {
            UnhookEvents();
            _layoutGrid = GetTemplateChild(LayoutGridName) as Grid;
            _imageCanvas = GetTemplateChild(ImageCanvasPartName) as Canvas;
            _sourceImage = GetTemplateChild(SourceImagePartName) as Image;
            _maskAreaPath = GetTemplateChild(MaskAreaPathPartName) as Path;
            _topButton = GetTemplateChild(TopButtonPartName) as Button;
            _bottomButton = GetTemplateChild(BottomButtonPartName) as Button;
            _leftButton = GetTemplateChild(LeftButtonPartName) as Button;
            _rigthButton = GetTemplateChild(RightButtonPartName) as Button;
            _upperLeftButton = GetTemplateChild(UpperLeftButtonPartName) as Button;
            _upperRightButton = GetTemplateChild(UpperRightButtonPartName) as Button;
            _lowerLeftButton = GetTemplateChild(LowerLeftButtonPartName) as Button;
            _lowerRigthButton = GetTemplateChild(LowerRightButtonPartName) as Button;
            HookUpEvents();
            UpdateControlButtonVisibility();
        }

        private void HookUpEvents()
        {
            if (_imageCanvas != null)
                _imageCanvas.SizeChanged += ImageCanvas_SizeChanged;
            if (_sourceImage != null)
            {
                _sourceImage.RenderTransform = _imageTransform;
                _sourceImage.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _sourceImage.ManipulationDelta += SourceImage_ManipulationDelta;
            }

            if (_maskAreaPath != null) _maskAreaPath.Data = _maskAreaGeometryGroup;

            if (_topButton != null)
            {
                _topButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _topButton.Tag = DragPosition.Top;
                _topButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _topButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _topButton.KeyDown += ControlButton_KeyDown;
                _topButton.KeyUp += ControlButton_KeyUp;
            }

            if (_bottomButton != null)
            {
                _bottomButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _bottomButton.Tag = DragPosition.Bottom;
                _bottomButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _bottomButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _bottomButton.KeyDown += ControlButton_KeyDown;
                _bottomButton.KeyUp += ControlButton_KeyUp;
            }

            if (_leftButton != null)
            {
                _leftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _leftButton.Tag = DragPosition.Left;
                _leftButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _leftButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _leftButton.KeyDown += ControlButton_KeyDown;
                _leftButton.KeyUp += ControlButton_KeyUp;
            }

            if (_rigthButton != null)
            {
                _rigthButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _rigthButton.Tag = DragPosition.Right;
                _rigthButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _rigthButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _rigthButton.KeyDown += ControlButton_KeyDown;
                _rigthButton.KeyUp += ControlButton_KeyUp;
            }

            if (_upperLeftButton != null)
            {
                _upperLeftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _upperLeftButton.Tag = DragPosition.UpperLeft;
                _upperLeftButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _upperLeftButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _upperLeftButton.KeyDown += ControlButton_KeyDown;
                _upperLeftButton.KeyUp += ControlButton_KeyUp;
            }

            if (_upperRightButton != null)
            {
                _upperRightButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _upperRightButton.Tag = DragPosition.UpperRight;
                _upperRightButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _upperRightButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _upperRightButton.KeyDown += ControlButton_KeyDown;
                _upperRightButton.KeyUp += ControlButton_KeyUp;
            }

            if (_lowerLeftButton != null)
            {
                _lowerLeftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _lowerLeftButton.Tag = DragPosition.LowerLeft;
                _lowerLeftButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _lowerLeftButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _lowerLeftButton.KeyDown += ControlButton_KeyDown;
                _lowerLeftButton.KeyUp += ControlButton_KeyUp;
            }

            if (_lowerRigthButton != null)
            {
                _lowerRigthButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _lowerRigthButton.Tag = DragPosition.LowerRight;
                _lowerRigthButton.ManipulationDelta += ControlButton_ManipulationDelta;
                _lowerRigthButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _lowerRigthButton.KeyDown += ControlButton_KeyDown;
                _lowerRigthButton.KeyUp += ControlButton_KeyUp;
            }
        }

        private void UnhookEvents()
        {
            if (_imageCanvas != null)
                _imageCanvas.SizeChanged -= ImageCanvas_SizeChanged;
            if (_sourceImage != null)
            {
                _sourceImage.RenderTransform = null;
                _sourceImage.ManipulationDelta -= SourceImage_ManipulationDelta;
            }

            if (_maskAreaPath != null) _maskAreaPath.Data = null;

            if (_topButton != null)
            {
                _topButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _topButton.ManipulationCompleted -= ControlButton_ManipulationCompleted;
                _topButton.KeyDown -= ControlButton_KeyDown;
                _topButton.KeyUp -= ControlButton_KeyUp;
            }

            if (_bottomButton != null)
            {
                _bottomButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _bottomButton.ManipulationCompleted -= ControlButton_ManipulationCompleted;
                _bottomButton.KeyDown -= ControlButton_KeyDown;
                _bottomButton.KeyUp -= ControlButton_KeyUp;
            }

            if (_leftButton != null)
            {
                _leftButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _leftButton.ManipulationCompleted += ControlButton_ManipulationCompleted;
                _leftButton.KeyDown -= ControlButton_KeyDown;
                _leftButton.KeyUp -= ControlButton_KeyUp;
            }

            if (_rigthButton != null)
            {
                _rigthButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _rigthButton.ManipulationCompleted -= ControlButton_ManipulationCompleted;
                _rigthButton.KeyDown -= ControlButton_KeyDown;
                _rigthButton.KeyUp -= ControlButton_KeyUp;
            }

            if (_upperLeftButton != null)
            {
                _upperLeftButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _upperLeftButton.ManipulationCompleted -= ControlButton_ManipulationCompleted;
                _upperLeftButton.KeyDown -= ControlButton_KeyDown;
                _upperLeftButton.KeyUp -= ControlButton_KeyUp;
            }

            if (_upperRightButton != null)
            {
                _upperRightButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _upperRightButton.ManipulationCompleted -= ControlButton_ManipulationCompleted;
                _upperRightButton.KeyDown -= ControlButton_KeyDown;
                _upperRightButton.KeyUp -= ControlButton_KeyUp;
            }

            if (_lowerLeftButton != null)
            {
                _lowerLeftButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _lowerLeftButton.ManipulationCompleted -= ControlButton_ManipulationCompleted;
                _lowerLeftButton.KeyDown -= ControlButton_KeyDown;
                _lowerLeftButton.KeyUp -= ControlButton_KeyUp;
            }

            if (_lowerRigthButton != null)
            {
                _lowerRigthButton.ManipulationDelta -= ControlButton_ManipulationDelta;
                _lowerRigthButton.ManipulationCompleted -= ControlButton_ManipulationCompleted;
                _lowerRigthButton.KeyDown -= ControlButton_KeyDown;
                _lowerRigthButton.KeyUp -= ControlButton_KeyUp;
            }
        }

        public async Task LoadImageFromFile(StorageFile imageFile)
        {
            var writeableBitmap = new WriteableBitmap(1, 1);
            using (var stream = await imageFile.OpenReadAsync())
            {
                await writeableBitmap.SetSourceAsync(stream);
            }

            SourceImage = writeableBitmap;
        }

        public async Task<WriteableBitmap> GetCroppedBitmapAsync()
        {
            if (SourceImage == null)
                return null;
            return await SourceImage.GetCroppedBitmapAsync(_currentCroppedRect);
        }

        public async Task SaveCroppedBitmapAsync(StorageFile imageFile, Guid encoderId)
        {
            if (SourceImage == null)
                return;
            var croppedBitmap = await SourceImage.GetCroppedBitmapAsync(_currentCroppedRect);
            await croppedBitmap.RenderToFile(imageFile, encoderId);
        }

        #region UpdateCropperLayout

        private void InitImageLayout()
        {
            _restrictedCropRect = new Rect(0, 0, SourceImage.PixelWidth, SourceImage.PixelHeight);
            var maxSelectedRect = _restrictedCropRect;
            _currentCroppedRect = KeepAspectRatio ? maxSelectedRect.GetUniformRect(UsedAspectRatio) : maxSelectedRect;
            UpdateImageLayout();
            UpdateControlButtonVisibility();
        }

        private void UpdateImageLayout()
        {
            var uniformSelectedRect = CanvasRect.GetUniformRect(_currentCroppedRect.Width / _currentCroppedRect.Height);
            UpdateImageLayoutWithViewport(uniformSelectedRect, _currentCroppedRect);
        }

        private void UpdateImageLayoutWithViewport(Rect viewport, Rect viewportImgRect)
        {
            var imageScale = viewport.Width / viewportImgRect.Width;
            _imageTransform.ScaleX = _imageTransform.ScaleY = imageScale;
            _imageTransform.TranslateX = viewport.X - viewportImgRect.X * imageScale;
            _imageTransform.TranslateY = viewport.Y - viewportImgRect.Y * imageScale;
            var selectedRect = _imageTransform.TransformBounds(_currentCroppedRect);
            _restrictedSelectRect = _imageTransform.TransformBounds(_restrictedCropRect);
            var startPoint = _restrictedSelectRect.GetSafePoint(new Point(selectedRect.X, selectedRect.Y));
            var endPoint = _restrictedSelectRect.GetSafePoint(new Point(selectedRect.X + selectedRect.Width,
                selectedRect.Y + selectedRect.Height));
            UpdateSelectedRect(startPoint, endPoint);
        }

        private void UpdateCroppedRectWithAspectRatio(DragPosition dragPosition, Point diffPos)
        {
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
                var inverseImageTransform = _imageTransform.Inverse;
                if (inverseImageTransform == null) return;
                var croppedRect = inverseImageTransform.TransformBounds(
                    new Rect(startPoint, endPoint));
                croppedRect.Intersect(_restrictedCropRect);
                _currentCroppedRect = croppedRect;
                var viewportRect = CanvasRect.GetUniformRect(selectedRect.Width / selectedRect.Height);
                var viewportImgRect = inverseImageTransform.TransformBounds(selectedRect);
                UpdateImageLayoutWithViewport(viewportRect, viewportImgRect);
            }
            else
            {
                UpdateSelectedRect(startPoint, endPoint);
            }
        }

        private void UpdateSelectedRect(Point startPoint, Point endPoint)
        {
            _startX = startPoint.X;
            _startY = startPoint.Y;
            _endX = endPoint.X;
            _endY = endPoint.Y;
            var centerX = (_endX - _startX) / 2 + _startX;
            var centerY = (_endY - _startY) / 2 + _startY;
            if (_topButton != null)
            {
                Canvas.SetLeft(_topButton, centerX);
                Canvas.SetTop(_topButton, _startY);
            }

            if (_bottomButton != null)
            {
                Canvas.SetLeft(_bottomButton, centerX);
                Canvas.SetTop(_bottomButton, _endY);
            }

            if (_leftButton != null)
            {
                Canvas.SetLeft(_leftButton, _startX);
                Canvas.SetTop(_leftButton, centerY);
            }

            if (_rigthButton != null)
            {
                Canvas.SetLeft(_rigthButton, _endX);
                Canvas.SetTop(_rigthButton, centerY);
            }

            if (_upperLeftButton != null)
            {
                Canvas.SetLeft(_upperLeftButton, _startX);
                Canvas.SetTop(_upperLeftButton, _startY);
            }

            if (_upperRightButton != null)
            {
                Canvas.SetLeft(_upperRightButton, _endX);
                Canvas.SetTop(_upperRightButton, _startY);
            }

            if (_lowerLeftButton != null)
            {
                Canvas.SetLeft(_lowerLeftButton, _startX);
                Canvas.SetTop(_lowerLeftButton, _endY);
            }

            if (_lowerRigthButton != null)
            {
                Canvas.SetLeft(_lowerRigthButton, _endX);
                Canvas.SetTop(_lowerRigthButton, _endY);
            }

            UpdateMaskArea();
        }

        private void UpdateMaskArea()
        {
            _maskAreaGeometryGroup.Children.Clear();
            _maskAreaGeometryGroup.Children.Add(new RectangleGeometry
            {
                Rect = new Rect(-_layoutGrid.Padding.Left, -_layoutGrid.Padding.Top, _layoutGrid.ActualWidth,
                    _layoutGrid.ActualHeight)
            });
            if (RoundedCrop)
            {
                var centerX = (_endX - _startX) / 2 + _startX;
                var centerY = (_endY - _startY) / 2 + _startY;
                _maskAreaGeometryGroup.Children.Add(new EllipseGeometry
                {
                    Center = new Point(centerX, centerY),
                    RadiusX = (_endX - _startX) / 2,
                    RadiusY = (_endY - _startY) / 2
                });
            }
            else
            {
                _maskAreaGeometryGroup.Children.Add(new RectangleGeometry
                {
                    Rect = new Rect(new Point(_startX, _startY), new Point(_endX, _endY))
                });
            }

            _layoutGrid.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, _layoutGrid.ActualWidth,
                    _layoutGrid.ActualHeight)
            };
        }

        private void UpdateAspectRatio()
        {
            if (KeepAspectRatio && SourceImage != null)
            {
                var inverseImageTransform = _imageTransform.Inverse;
                if (inverseImageTransform != null)
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
                    _currentCroppedRect = inverseImageTransform.TransformBounds(uniformSelectedRect);
                    UpdateImageLayout();
                }
            }
        }

        private void UpdateControlButtonVisibility()
        {
            var cornerBtnVisibility = RoundedCrop ? Visibility.Collapsed : Visibility.Visible;
            var otherBtnVisibility = RoundedCrop ? Visibility.Visible : SecondaryControlButtonVisibility;
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

        #endregion
    }
}