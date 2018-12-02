using System;
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
using System.Collections.Generic;
using System.Linq;

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
        public ImageCropper()
        {
            DefaultStyleKey = typeof(ImageCropper);
        }
        
        private Rect CanvasRect => new Rect(0,0, _imageCanvas.ActualWidth, _imageCanvas.ActualHeight);
        private bool KeepAspectRatio => UsedAspectRatio > 0;
        private double UsedAspectRatio => RoundedCrop ? 1 : AspectRatio;
        
        private Size MinCroppedSize
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
                var realMinSelectSize = _imageTransform.TransformBounds(new Rect(new Point(), MinCroppedSize));
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
                else
                {
                    return new Size(realMinSelectSize.Width, realMinSelectSize.Height);
                }
            }
        }

        protected override void OnApplyTemplate()
        {
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

            if (_maskAreaPath != null)
            {
                _maskAreaPath.Data = _maskAreaGeometryGroup;
            }

            if (_topButton != null)
            {
                _topButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _topButton.Tag = DragPoint.Top;
                _topButton.ManipulationDelta += DragButton_ManipulationDelta;
                _topButton.ManipulationCompleted += DragButton_ManipulationCompleted;
            }

            if (_bottomButton != null)
            {
                _bottomButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _bottomButton.Tag = DragPoint.Bottom;
                _bottomButton.ManipulationDelta += DragButton_ManipulationDelta;
                _bottomButton.ManipulationCompleted += DragButton_ManipulationCompleted;
            }

            if (_leftButton != null)
            {
                _leftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _leftButton.Tag = DragPoint.Left;
                _leftButton.ManipulationDelta += DragButton_ManipulationDelta;
                _leftButton.ManipulationCompleted += DragButton_ManipulationCompleted;
            }

            if (_rigthButton != null)
            {
                _rigthButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _rigthButton.Tag = DragPoint.Right;
                _rigthButton.ManipulationDelta += DragButton_ManipulationDelta;
                _rigthButton.ManipulationCompleted += DragButton_ManipulationCompleted;
            }

            if (_upperLeftButton != null)
            {
                _upperLeftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _upperLeftButton.Tag = DragPoint.UpperLeft;
                _upperLeftButton.ManipulationDelta += DragButton_ManipulationDelta;
                _upperLeftButton.ManipulationCompleted += DragButton_ManipulationCompleted;
            }

            if (_upperRightButton != null)
            {
                _upperRightButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _upperRightButton.Tag = DragPoint.UpperRight;
                _upperRightButton.ManipulationDelta += DragButton_ManipulationDelta;
                _upperRightButton.ManipulationCompleted += DragButton_ManipulationCompleted;
            }

            if (_lowerLeftButton != null)
            {
                _lowerLeftButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _lowerLeftButton.Tag = DragPoint.LowerLeft;
                _lowerLeftButton.ManipulationDelta += DragButton_ManipulationDelta;
                _lowerLeftButton.ManipulationCompleted += DragButton_ManipulationCompleted;
            }

            if (_lowerRigthButton != null)
            {
                _lowerRigthButton.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _lowerRigthButton.Tag = DragPoint.LowerRight;
                _lowerRigthButton.ManipulationDelta += DragButton_ManipulationDelta;
                _lowerRigthButton.ManipulationCompleted += DragButton_ManipulationCompleted;
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
            return await SourceImage.GetCroppedBitmapAsync(_currentClippedRect);
        }

        public async Task SaveCroppedBitmapAsync(StorageFile imageFile, Guid encoderId)
        {
            if (SourceImage == null)
                return;
            var croppedBitmap = await SourceImage.GetCroppedBitmapAsync(_currentClippedRect);
            await croppedBitmap.RenderToFile(imageFile, encoderId);
        }

        #region Constants

        private const string LayoutGridName = "PART_LayoutGrid";
        private const string ImageCanvasPartName = "PART_ImageCanvas";
        private const string SourceImagePartName = "PART_SourceImage";
        private const string MaskAreaPathPartName = "PART_MaskAreaPath";
        private const string TopButtonPartName = "PART_TopButton";
        private const string BottomButtonPartName = "PART_BottomButton";
        private const string LeftButtonPartName = "PART_LeftButton";
        private const string RightButtonPartName = "PART_RightButton";
        private const string UpperLeftButtonPartName = "PART_UpperLeftButton";
        private const string UpperRightButtonPartName = "PART_UpperRightButton";
        private const string LowerLeftButtonPartName = "PART_LowerLeftButton";
        private const string LowerRightButtonPartName = "PART_LowerRightButton";

        #endregion

        #region private property Fields

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
        private readonly GeometryGroup _maskAreaGeometryGroup = new GeometryGroup {FillRule = FillRule.EvenOdd};
        private readonly CompositeTransform _imageTransform = new CompositeTransform();
        private Rect _currentClippedRect = Rect.Empty;
        private Rect _restrictedSelectRect = Rect.Empty;
        private Rect _restrictedClipRect = Rect.Empty;
        private double _startX = 0d;
        private double _startY = 0d;
        private double _endX = 20d;
        private double _endY = 20d;

        #endregion

        #region Events

        private void DragButton_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var inverseImageTransform = _imageTransform.Inverse;
            if (inverseImageTransform != null)
            {
                var selectedRect = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
                var clippedRect = inverseImageTransform.TransformBounds(selectedRect);
                if (clippedRect.Width > MinCroppedSize.Width && clippedRect.Height > MinCroppedSize.Height)
                {
                    clippedRect.Intersect(_restrictedClipRect);
                    _currentClippedRect = clippedRect;
                }
                UpdateImageLayout();
            }
        }

        private void DragButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var dragButtom = (FrameworkElement)sender;
            var dragButtomPosition = new Point(Canvas.GetLeft(dragButtom), Canvas.GetTop(dragButtom));
            var currentPointerPosition = new Point(dragButtomPosition.X + e.Position.X + e.Delta.Translation.X,
                dragButtomPosition.Y + e.Position.Y + e.Delta.Translation.Y);
            var safePosition = _restrictedSelectRect.GetSafePoint(currentPointerPosition);
            var safeDiffPoint = new Point(safePosition.X - dragButtomPosition.X, safePosition.Y - dragButtomPosition.Y);
            var tag = dragButtom.Tag;
            if (tag != null && Enum.TryParse(tag.ToString(), false, out DragPoint dragPoint))
            {
                UpdateClipRectWithAspectRatio(dragPoint, safeDiffPoint);
            }

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
                    movedRect.Intersect(_restrictedClipRect);
                    _currentClippedRect = movedRect;
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

        #endregion

        #region UpdateCropperLayout

        private void InitImageLayout()
        {
            _restrictedClipRect = new Rect(0, 0, SourceImage.PixelWidth, SourceImage.PixelHeight);
            var maxSelectedRect = _restrictedClipRect;
            _currentClippedRect = KeepAspectRatio ? maxSelectedRect.GetUniformRect(UsedAspectRatio) : maxSelectedRect;
            UpdateImageLayout();
            UpdateDragButtonVisibility();
        }

        private void UpdateImageLayout()
        {
            var uniformSelectedRect = CanvasRect.GetUniformRect(_currentClippedRect.Width / _currentClippedRect.Height);
            UpdateImageLayoutWithViewport(uniformSelectedRect, _currentClippedRect);
        }

        private void UpdateImageLayoutWithViewport(Rect viewport, Rect viewportImgRect)
        {
            var imageScale = viewport.Width / viewportImgRect.Width;
            _imageTransform.ScaleX = _imageTransform.ScaleY = imageScale;
            _imageTransform.TranslateX = viewport.X - viewportImgRect.X * imageScale;
            _imageTransform.TranslateY = viewport.Y - viewportImgRect.Y * imageScale;
            var selectedRect = _imageTransform.TransformBounds(_currentClippedRect);
            _restrictedSelectRect = _imageTransform.TransformBounds(_restrictedClipRect);
            var startPoint = _restrictedSelectRect.GetSafePoint(new Point(selectedRect.X, selectedRect.Y));
            var endPoint = _restrictedSelectRect.GetSafePoint(new Point(selectedRect.X + selectedRect.Width, selectedRect.Y + selectedRect.Height));
            UpdateSelectedRect(startPoint, endPoint);
        }

        private void UpdateClipRectWithAspectRatio(DragPoint dragPoint, Point diffPos)
        {
            double radian = 0d, diffPointRadian = 0d, effectiveLength = 0d;
            if (KeepAspectRatio)
            {
                radian = Math.Atan(UsedAspectRatio);
                diffPointRadian = Math.Atan(diffPos.X / diffPos.Y);
            }

            var startPoint = new Point(_startX, _startY);
            var endPoint = new Point(_endX, _endY);
            switch (dragPoint)
            {
                case DragPoint.Top:
                    startPoint.Y += diffPos.Y;
                    if (KeepAspectRatio)
                    {
                        var changeX = diffPos.Y * UsedAspectRatio;
                        startPoint.X += changeX / 2;
                        endPoint.X -= changeX / 2;
                    }

                    break;
                case DragPoint.Bottom:
                    endPoint.Y += diffPos.Y;
                    if (KeepAspectRatio)
                    {
                        var changeX = diffPos.Y * UsedAspectRatio;
                        startPoint.X -= changeX / 2;
                        endPoint.X += changeX / 2;
                    }

                    break;
                case DragPoint.Left:
                    startPoint.X += diffPos.X;
                    if (KeepAspectRatio)
                    {
                        var changeY = diffPos.X / UsedAspectRatio;
                        startPoint.Y += changeY / 2;
                        endPoint.Y -= changeY / 2;
                    }

                    break;
                case DragPoint.Right:
                    endPoint.X += diffPos.X;
                    if (KeepAspectRatio)
                    {
                        var changeY = diffPos.X / UsedAspectRatio;
                        startPoint.Y -= changeY / 2;
                        endPoint.Y += changeY / 2;
                    }

                    break;
                case DragPoint.UpperLeft:
                    if (KeepAspectRatio)
                    {
                        effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        diffPos.X = effectiveLength * Math.Sin(radian);
                        diffPos.Y = effectiveLength * Math.Cos(radian);
                    }

                    startPoint.X += diffPos.X;
                    startPoint.Y += diffPos.Y;
                    break;
                case DragPoint.UpperRight:
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
                case DragPoint.LowerLeft:
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
                case DragPoint.LowerRight:
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
            if ((!KeepAspectRatio) && (!RectExtensions.IsSafeRect(startPoint, endPoint)))
            {
                switch (dragPoint)
                {
                    case DragPoint.Top:
                    case DragPoint.Bottom:
                    case DragPoint.Left:
                    case DragPoint.Right:
                        break;
                    case DragPoint.UpperLeft:
                        if (startPoint.X > endPoint.X)
                        {
                            startPoint.X = endPoint.X - MinSelectSize.Width;
                        }
                        if (startPoint.Y > endPoint.Y)
                        {
                            startPoint.Y = endPoint.Y - MinSelectSize.Height;
                        }
                        break;
                    case DragPoint.UpperRight:
                        if (startPoint.X > endPoint.X)
                        {
                            endPoint.X = startPoint.X + MinSelectSize.Width;
                        }
                        if (startPoint.Y > endPoint.Y)
                        {
                            startPoint.Y = endPoint.Y - MinSelectSize.Height;
                        }
                        break;
                    case DragPoint.LowerLeft:
                        if (startPoint.X > endPoint.X)
                        {
                            startPoint.X = endPoint.X - MinSelectSize.Width;
                        }
                        if (startPoint.Y > endPoint.Y)
                        {
                            endPoint.Y = startPoint.Y + MinSelectSize.Height;
                        }
                        break;
                    case DragPoint.LowerRight:
                        if (startPoint.X > endPoint.X)
                        {
                            endPoint.X = startPoint.X + MinSelectSize.Width;
                        }
                        if (startPoint.Y > endPoint.Y)
                        {
                            endPoint.Y = startPoint.Y + MinSelectSize.Height;
                        }
                        break;
                }
            }
            if (RectExtensions.IsSafeRect(startPoint, endPoint)
                && _restrictedSelectRect.IsSafePoint(startPoint)
                && _restrictedSelectRect.IsSafePoint(endPoint))
            {
                var selectedRect = new Rect(startPoint, endPoint);
                selectedRect.Union(CanvasRect);
                if (selectedRect.X < CanvasRect.X || selectedRect.Y < CanvasRect.Y || selectedRect.Width > CanvasRect.Width ||
                    selectedRect.Height > CanvasRect.Height)
                {
                    var inverseImageTransform = _imageTransform.Inverse;
                    if (inverseImageTransform != null)
                    {
                        var clippedRect = inverseImageTransform.TransformBounds(
                            new Rect(startPoint, endPoint));
                        clippedRect.Intersect(_restrictedClipRect);
                        _currentClippedRect = clippedRect;
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
                        _restrictedSelectRect.X+_restrictedSelectRect.Width - centerX,
                        centerX - _restrictedSelectRect.X,
                        _restrictedSelectRect.Y + _restrictedSelectRect.Height - centerY,
                        centerY - _restrictedSelectRect.Y
                    };
                    var restrictedLength = marginArray.Min() * 2;
                    var maxSelectLength = Math.Max(_endX - _startX, _endY - _startY);
                    var maxLength = Math.Min(restrictedLength, maxSelectLength);
                    var viewRect = new Rect(centerX - maxLength / 2, centerY - maxLength / 2, maxLength, maxLength);
                    var uniformSelectedRect = viewRect.GetUniformRect(UsedAspectRatio);
                    _currentClippedRect = inverseImageTransform.TransformBounds(uniformSelectedRect);
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