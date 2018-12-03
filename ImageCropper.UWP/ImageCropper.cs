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
            UpdateDragButtonVisibility();
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
                _topButton.ManipulationDelta += DragButton_ManipulationDelta;
                _topButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _topButton.KeyDown += DragButton_KeyDown;
                _topButton.KeyUp += DragButton_KeyUp;
            }

            if (_bottomButton != null)
            {
                _bottomButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _bottomButton.Tag = DragPosition.Bottom;
                _bottomButton.ManipulationDelta += DragButton_ManipulationDelta;
                _bottomButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _bottomButton.KeyDown += DragButton_KeyDown;
                _bottomButton.KeyUp += DragButton_KeyUp;
            }

            if (_leftButton != null)
            {
                _leftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _leftButton.Tag = DragPosition.Left;
                _leftButton.ManipulationDelta += DragButton_ManipulationDelta;
                _leftButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _leftButton.KeyDown += DragButton_KeyDown;
                _leftButton.KeyUp += DragButton_KeyUp;
            }

            if (_rigthButton != null)
            {
                _rigthButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _rigthButton.Tag = DragPosition.Right;
                _rigthButton.ManipulationDelta += DragButton_ManipulationDelta;
                _rigthButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _rigthButton.KeyDown += DragButton_KeyDown;
                _rigthButton.KeyUp += DragButton_KeyUp;
            }

            if (_upperLeftButton != null)
            {
                _upperLeftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _upperLeftButton.Tag = DragPosition.UpperLeft;
                _upperLeftButton.ManipulationDelta += DragButton_ManipulationDelta;
                _upperLeftButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _upperLeftButton.KeyDown += DragButton_KeyDown;
                _upperLeftButton.KeyUp += DragButton_KeyUp;
            }

            if (_upperRightButton != null)
            {
                _upperRightButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _upperRightButton.Tag = DragPosition.UpperRight;
                _upperRightButton.ManipulationDelta += DragButton_ManipulationDelta;
                _upperRightButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _upperRightButton.KeyDown += DragButton_KeyDown;
                _upperRightButton.KeyUp += DragButton_KeyUp;
            }

            if (_lowerLeftButton != null)
            {
                _lowerLeftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _lowerLeftButton.Tag = DragPosition.LowerLeft;
                _lowerLeftButton.ManipulationDelta += DragButton_ManipulationDelta;
                _lowerLeftButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _lowerLeftButton.KeyDown += DragButton_KeyDown;
                _lowerLeftButton.KeyUp += DragButton_KeyUp;
            }

            if (_lowerRigthButton != null)
            {
                _lowerRigthButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _lowerRigthButton.Tag = DragPosition.LowerRight;
                _lowerRigthButton.ManipulationDelta += DragButton_ManipulationDelta;
                _lowerRigthButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _lowerRigthButton.KeyDown += DragButton_KeyDown;
                _lowerRigthButton.KeyUp += DragButton_KeyUp;
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
                _topButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _topButton.ManipulationCompleted -= DragButton_ManipulationCompleted;
                _topButton.KeyDown -= DragButton_KeyDown;
                _topButton.KeyUp -= DragButton_KeyUp;
            }

            if (_bottomButton != null)
            {
                _bottomButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _bottomButton.ManipulationCompleted -= DragButton_ManipulationCompleted;
                _bottomButton.KeyDown -= DragButton_KeyDown;
                _bottomButton.KeyUp -= DragButton_KeyUp;
            }

            if (_leftButton != null)
            {
                _leftButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _leftButton.ManipulationCompleted += DragButton_ManipulationCompleted;
                _leftButton.KeyDown -= DragButton_KeyDown;
                _leftButton.KeyUp -= DragButton_KeyUp;
            }

            if (_rigthButton != null)
            {
                _rigthButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _rigthButton.ManipulationCompleted -= DragButton_ManipulationCompleted;
                _rigthButton.KeyDown -= DragButton_KeyDown;
                _rigthButton.KeyUp -= DragButton_KeyUp;
            }

            if (_upperLeftButton != null)
            {
                _upperLeftButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _upperLeftButton.ManipulationCompleted -= DragButton_ManipulationCompleted;
                _upperLeftButton.KeyDown -= DragButton_KeyDown;
                _upperLeftButton.KeyUp -= DragButton_KeyUp;
            }

            if (_upperRightButton != null)
            {
                _upperRightButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _upperRightButton.ManipulationCompleted -= DragButton_ManipulationCompleted;
                _upperRightButton.KeyDown -= DragButton_KeyDown;
                _upperRightButton.KeyUp -= DragButton_KeyUp;
            }

            if (_lowerLeftButton != null)
            {
                _lowerLeftButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _lowerLeftButton.ManipulationCompleted -= DragButton_ManipulationCompleted;
                _lowerLeftButton.KeyDown -= DragButton_KeyDown;
                _lowerLeftButton.KeyUp -= DragButton_KeyUp;
            }

            if (_lowerRigthButton != null)
            {
                _lowerRigthButton.ManipulationDelta -= DragButton_ManipulationDelta;
                _lowerRigthButton.ManipulationCompleted -= DragButton_ManipulationCompleted;
                _lowerRigthButton.KeyDown -= DragButton_KeyDown;
                _lowerRigthButton.KeyUp -= DragButton_KeyUp;
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
            UpdateDragButtonVisibility();
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

            if (!KeepAspectRatio && !RectExtensions.IsSafeRect(startPoint, endPoint))
                switch (dragPosition)
                {
                    case DragPosition.Top:
                    case DragPosition.Bottom:
                    case DragPosition.Left:
                    case DragPosition.Right:
                        break;
                    case DragPosition.UpperLeft:
                        if (startPoint.X > endPoint.X) startPoint.X = endPoint.X - MinSelectSize.Width;
                        if (startPoint.Y > endPoint.Y) startPoint.Y = endPoint.Y - MinSelectSize.Height;
                        break;
                    case DragPosition.UpperRight:
                        if (startPoint.X > endPoint.X) endPoint.X = startPoint.X + MinSelectSize.Width;
                        if (startPoint.Y > endPoint.Y) startPoint.Y = endPoint.Y - MinSelectSize.Height;
                        break;
                    case DragPosition.LowerLeft:
                        if (startPoint.X > endPoint.X) startPoint.X = endPoint.X - MinSelectSize.Width;
                        if (startPoint.Y > endPoint.Y) endPoint.Y = startPoint.Y + MinSelectSize.Height;
                        break;
                    case DragPosition.LowerRight:
                        if (startPoint.X > endPoint.X) endPoint.X = startPoint.X + MinSelectSize.Width;
                        if (startPoint.Y > endPoint.Y) endPoint.Y = startPoint.Y + MinSelectSize.Height;
                        break;
                }
            if (RectExtensions.IsSafeRect(startPoint, endPoint)
                && _restrictedSelectRect.IsSafePoint(startPoint)
                && _restrictedSelectRect.IsSafePoint(endPoint))
            {
                var selectedRect = new Rect(startPoint, endPoint);
                selectedRect.Union(CanvasRect);
                if (selectedRect.X < CanvasRect.X || selectedRect.Y < CanvasRect.Y ||
                    selectedRect.Width > CanvasRect.Width ||
                    selectedRect.Height > CanvasRect.Height)
                {
                    var inverseImageTransform = _imageTransform.Inverse;
                    if (inverseImageTransform != null)
                    {
                        var croppedRect = inverseImageTransform.TransformBounds(
                            new Rect(startPoint, endPoint));
                        croppedRect.Intersect(_restrictedCropRect);
                        _currentCroppedRect = croppedRect;
                        var viewportRect = CanvasRect.GetUniformRect(selectedRect.Width / selectedRect.Height);
                        var viewportImgRect = inverseImageTransform.TransformBounds(selectedRect);
                        UpdateImageLayoutWithViewport(viewportRect, viewportImgRect);
                    }
                }
                else
                {
                    UpdateSelectedRect(startPoint, endPoint);
                }
            }
        }

        private void UpdateSelectedRect(Point startPoint, Point endPoint)
        {
            if (endPoint.X - startPoint.X < MinSelectSize.Width ||
                endPoint.Y - startPoint.Y < MinSelectSize.Height)
                return;
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
                    var marginArray = new double[4]
                    {
                        _restrictedSelectRect.X + _restrictedSelectRect.Width - centerX,
                        centerX - _restrictedSelectRect.X,
                        _restrictedSelectRect.Y + _restrictedSelectRect.Height - centerY,
                        centerY - _restrictedSelectRect.Y
                    };
                    var restrictedLength = marginArray.Min() * 2;
                    var maxSelectLength = Math.Max(_endX - _startX, _endY - _startY);
                    var maxLength = Math.Min(restrictedLength, maxSelectLength);
                    var viewRect = new Rect(centerX - maxLength / 2, centerY - maxLength / 2, maxLength, maxLength);
                    var uniformSelectedRect = viewRect.GetUniformRect(UsedAspectRatio);
                    _currentCroppedRect = inverseImageTransform.TransformBounds(uniformSelectedRect);
                    UpdateImageLayout();
                }
            }
        }

        private void UpdateDragButtonVisibility()
        {
            var cornerBtnVisibility = RoundedCrop ? Visibility.Collapsed : Visibility.Visible;
            var otherBtnVisibility = Visibility.Visible;
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

            UpdateMaskArea();
        }

        #endregion
    }
}