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
    public sealed class ImageCropper : Control
    {
        public ImageCropper()
        {
            DefaultStyleKey = typeof(ImageCropper);
        }

        private double CanvasWidth => _imageCanvas.ActualWidth;
        private double CanvasHeight => _imageCanvas.ActualHeight;
        public bool KeepAspectRatio => UsedAspectRatio > 0;
        private double UsedAspectRatio => RoundedCrop ? 1 : AspectRatio;
        public double MinLength { get; set; } = 40;

        private Size MinSelectSize
        {
            get
            {
                var aspectRatio = KeepAspectRatio ? UsedAspectRatio : 1;
                var size = new Size(MinLength, MinLength);
                if (aspectRatio >= 1)
                    size.Width = size.Height * aspectRatio;
                else
                    size.Height = size.Width / aspectRatio;
                return size;
            }
        }

        private Size MinClipSize => MinSelectSize;

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
            if (_layoutGrid != null)
                _layoutGrid.SizeChanged += LayoutGrid_SizeChanged;
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
            return await SourceImage.GetCroppedBitmapAsync(_currentClipRect);
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

        #region Fields
        
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
        private Rect _currentClipRect = Rect.Empty;
        private Rect _limitedRect = Rect.Empty;
        private Rect _maxClipRect = Rect.Empty;
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
                var newClipRect = inverseImageTransform.TransformBounds(selectedRect);
                if (newClipRect.Width > MinClipSize.Width && newClipRect.Height > MinClipSize.Height)
                {
                    _currentClipRect = newClipRect;
                }
                UpdateImageLayout();
            }
        }

        private void DragButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var dragButtom = (FrameworkElement) sender;
            var dragButtomPosition = new Point(Canvas.GetLeft(dragButtom), Canvas.GetTop(dragButtom));
            var currentPointerPosition = new Point(dragButtomPosition.X + e.Position.X + e.Delta.Translation.X,
                Canvas.GetTop(dragButtom) + e.Position.Y + e.Delta.Translation.Y);
            var safePosition = _limitedRect.GetSafePoint(currentPointerPosition);
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
                if (_limitedRect.IsSafePoint(startPoint) && _limitedRect.IsSafePoint(endPoint))
                {
                    var selectedRect = new Rect(startPoint, endPoint);
                    if (selectedRect.Width < MinSelectSize.Width || selectedRect.Height < MinSelectSize.Height)
                        return;
                    var movedRect = inverseImageTransform.TransformBounds(selectedRect);
                    movedRect.Intersect(_maxClipRect);
                    _currentClipRect = movedRect;
                    UpdateImageLayout();
                }
            }
        }

        private void LayoutGrid_SizeChanged(object sender, SizeChangedEventArgs e)
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
            _maxClipRect = new Rect(0, 0, SourceImage.PixelWidth, SourceImage.PixelHeight);
            var maxSelectedRect = new Rect(1, 1, SourceImage.PixelWidth - 2, SourceImage.PixelHeight - 2);
            _currentClipRect = KeepAspectRatio ? maxSelectedRect.GetUniformRect(UsedAspectRatio) : maxSelectedRect;
            UpdateImageLayout();
            UpdateDragButtonVisibility();
        }

        private void UpdateImageLayout()
        {
            var canvasRect = new Rect(0, 0, CanvasWidth, CanvasHeight);
            var uniformSelectedRect = canvasRect.GetUniformRect(_currentClipRect.Width / _currentClipRect.Height);
            UpdateImageLayoutWithViewport(uniformSelectedRect, _currentClipRect);
        }

        private void UpdateImageLayoutWithViewport(Rect viewport, Rect viewportImgRect)
        {
            var imageScale = viewport.Width / viewportImgRect.Width;
            _imageTransform.ScaleX = _imageTransform.ScaleY = imageScale;
            _imageTransform.TranslateX = viewport.X - viewportImgRect.X * imageScale;
            _imageTransform.TranslateY = viewport.Y - viewportImgRect.Y * imageScale;
            var selectedRect = _imageTransform.TransformBounds(_currentClipRect);
            _limitedRect = _imageTransform.TransformBounds(_maxClipRect);
            var startPoint = _limitedRect.GetSafePoint(new Point(selectedRect.X, selectedRect.Y));
            var endPoint = _limitedRect.GetSafePoint(new Point(selectedRect.X + selectedRect.Width, selectedRect.Y + selectedRect.Height));
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
                && _limitedRect.IsSafePoint(startPoint)
                && _limitedRect.IsSafePoint(endPoint))
            {
                var canvasRect = new Rect(0, 0, CanvasWidth, CanvasHeight);
                var newRect = new Rect(startPoint, endPoint);
                canvasRect.Union(newRect);
                if (canvasRect.X < 0 || canvasRect.Y < 0 || canvasRect.Width > CanvasWidth ||
                    canvasRect.Height > CanvasHeight)
                {
                    var inverseImageTransform = _imageTransform.Inverse;
                    if (inverseImageTransform != null)
                    {
                        var movedRect = inverseImageTransform.TransformBounds(
                            new Rect(startPoint, endPoint));
                        movedRect.Intersect(_maxClipRect);
                        _currentClipRect = movedRect;
                        var oriCanvasRect = new Rect(0, 0, CanvasWidth, CanvasHeight);
                        var viewportRect = oriCanvasRect.GetUniformRect(canvasRect.Width / canvasRect.Height);
                        var viewportImgRect = inverseImageTransform.TransformBounds(canvasRect);
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
                    var selectedRect = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
                    var uniformSelectedRect = selectedRect.GetUniformRect(UsedAspectRatio);
                    _currentClipRect = inverseImageTransform.TransformBounds(uniformSelectedRect);
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

        #region DependencyProperty Fields

        private static void OnSourceImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            if (e.NewValue is WriteableBitmap bitmap)
            {
                if (bitmap.PixelWidth > target.MinClipSize.Width && bitmap.PixelHeight > target.MinClipSize.Height)
                {
                    target.InitImageLayout();
                }
                else
                {
                    throw new ArgumentException("Image size is too small!");
                }
            }
        }

        private static void OnAspectRatioChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            target.UpdateAspectRatio();
        }

        private static void OnRoundedCropChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            target.UpdateAspectRatio();
            target.UpdateDragButtonVisibility();
        }

        public WriteableBitmap SourceImage
        {
            get => (WriteableBitmap) GetValue(SourceImageProperty);
            set => SetValue(SourceImageProperty, value);
        }

        /// <summary>
        ///     Image aspect ratio，the default value is -1.
        /// </summary>
        public double AspectRatio
        {
            get => (double) GetValue(AspectRatioProperty);
            set => SetValue(AspectRatioProperty, value);
        }

        public bool RoundedCrop
        {
            get => (bool)GetValue(RoundedCropProperty);
            set => SetValue(RoundedCropProperty, value);
        }

        public Brush MarkFill
        {
            get => (Brush)GetValue(MarkFillProperty);
            set => SetValue(MarkFillProperty, value);
        }

        /// <summary>
        /// Gets or sets a value for the style to use for the DragButton of the ImageCropper.
        /// </summary>
        public Style DragButtonStyle
        {
            get => (Style)GetValue(DragButtonStyleProperty);
            set => SetValue(DragButtonStyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for AspectRatio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register(nameof(AspectRatio), typeof(double), typeof(ImageCropper),
                new PropertyMetadata(-1d, OnAspectRatioChanged));

        // Using a DependencyProperty as the backing store for SourceImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceImageProperty =
            DependencyProperty.Register(nameof(SourceImage), typeof(WriteableBitmap), typeof(ImageCropper),
                new PropertyMetadata(null, OnSourceImageChanged));

        // Using a DependencyProperty as the backing store for RoundedCrop.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RoundedCropProperty =
            DependencyProperty.Register(nameof(RoundedCrop), typeof(bool), typeof(ImageCropper),
                new PropertyMetadata(false, OnRoundedCropChanged));

        // Using a DependencyProperty as the backing store for MarkFill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkFillProperty =
            DependencyProperty.Register(nameof(MarkFill), typeof(Brush), typeof(ImageCropper),
                new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty DragButtonStyleProperty =
            DependencyProperty.Register(nameof(DragButtonStyle), typeof(Style), typeof(ImageCropper),
                new PropertyMetadata(default(Style)));
        #endregion
    }
}