using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using ImageCropper.UWP.Helpers;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace ImageCropper.UWP
{
    [TemplatePart(Name = LayoutGridName, Type = typeof(Grid))]
    [TemplatePart(Name = ImageCanvasPartName, Type = typeof(Canvas))]
    [TemplatePart(Name = SourceImagePartName, Type = typeof(Image))]
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
        public bool KeepAspectRatio => AspectRatio > 0;
        public double MinLength { get; set; } = 40;

        private Size MinSelectSize
        {
            get
            {
                var aspectRatio = KeepAspectRatio ? AspectRatio : 1;
                var size = new Size(MinLength, MinLength);
                if (aspectRatio >= 1)
                    size.Width = size.Height * aspectRatio;
                else
                    size.Height = size.Width / aspectRatio;
                return size;
            }
        }

        protected override void OnApplyTemplate()
        {
            _layoutGrid = GetTemplateChild(LayoutGridName) as Grid;
            _imageCanvas = GetTemplateChild(ImageCanvasPartName) as Canvas;
            _sourceImage = GetTemplateChild(SourceImagePartName) as Image;

            _upperLeftButton = GetTemplateChild(UpperLeftButtonPartName) as Button;
            _upperRightButton = GetTemplateChild(UpperRightButtonPartName) as Button;
            _lowerLeftButton = GetTemplateChild(LowerLeftButtonPartName) as Button;
            _lowerRigthButton = GetTemplateChild(LowerRightButtonPartName) as Button;

            HookUpEvents();
        }

        private void HookUpEvents()
        {
            if (_layoutGrid != null)
                _layoutGrid.SizeChanged += LayoutGrid_SizeChanged;
            if (_sourceImage != null)
            {
                _sourceImage.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _sourceImage.ManipulationDelta += SourceImage_ManipulationDelta;
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
            if (SourceImage == null || ImageTransform == null)
                return null;
            return await SourceImage.GetCroppedBitmapAsync(_currentClipRect);
        }

        #region Constants

        private const string LayoutGridName = "PART_LayoutGrid";
        private const string ImageCanvasPartName = "PART_ImageCanvas";
        private const string SourceImagePartName = "PART_SourceImage";
        private const string UpperLeftButtonPartName = "PART_TopLeftCorner";
        private const string UpperRightButtonPartName = "PART_TopRightCorner";
        private const string LowerLeftButtonPartName = "PART_BottomLeftCorner";
        private const string LowerRightButtonPartName = "PART_BottomRightCorner";

        #endregion

        #region Fields

        private Grid _layoutGrid;
        private Canvas _imageCanvas;
        private Image _sourceImage;
        private Button _upperLeftButton;
        private Button _upperRightButton;
        private Button _lowerLeftButton;
        private Button _lowerRigthButton;
        private readonly GeometryGroup _maskArea = new GeometryGroup {FillRule = FillRule.EvenOdd};
        private bool _changeByCode;
        private Rect _currentClipRect = Rect.Empty;
        private Rect _limitedRect = Rect.Empty;
        private Rect _maxClipRect = Rect.Empty;

        #endregion

        #region Events

        private void DragButton_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var inverseImageTransform = ImageTransform.Inverse;
            if (inverseImageTransform != null)
            {
                _currentClipRect =
                    inverseImageTransform.TransformBounds(new Rect(new Point(X1, Y1), new Point(X2, Y2)));
                UpdateImageLayout();
            }
        }

        private void DragButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var element = (FrameworkElement) sender;
            var pos = e.Delta.Translation;
            var tag = element.Tag;
            if (tag != null && Enum.TryParse(tag.ToString(), false, out DragPoint dragPoint))
                UpdateClipRectWithAspectRatio(dragPoint, pos);
        }

        private void SourceImage_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var diffPos = e.Delta.Translation;
            var inverseImageTransform = ImageTransform.Inverse;
            if (inverseImageTransform != null)
            {
                var startPoint = new Point(X1 - diffPos.X, Y1 - diffPos.Y);
                var endPoint = new Point(X2 - diffPos.X, Y2 - diffPos.Y);
                if (_limitedRect.Contains(startPoint) && _limitedRect.Contains(endPoint))
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
            if (SourceImage == null || ImageTransform == null)
                return;
            UpdateImageLayout();
            UpdateMaskArea();
        }

        #endregion

        #region UpdateCropperLayout

        private void InitImageLayout()
        {
            if (ImageTransform == null)
                ImageTransform = new CompositeTransform();
            _maxClipRect = new Rect(0, 0, SourceImage.PixelWidth, SourceImage.PixelHeight);
            _currentClipRect = KeepAspectRatio ? _maxClipRect.GetUniformRect(AspectRatio) : _maxClipRect;
            UpdateImageLayout();
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
            ImageTransform.ScaleX = ImageTransform.ScaleY = imageScale;
            ImageTransform.TranslateX = viewport.X - viewportImgRect.X * imageScale;
            ImageTransform.TranslateY = viewport.Y - viewportImgRect.Y * imageScale;
            var selectedRect = ImageTransform.TransformBounds(_currentClipRect);
            _limitedRect = ImageTransform.TransformBounds(_maxClipRect);
            _changeByCode = true;
            X1 = selectedRect.X;
            Y1 = selectedRect.Y;
            X2 = selectedRect.X + selectedRect.Width;
            Y2 = selectedRect.Y + selectedRect.Height;
            _changeByCode = false;
        }

        private void UpdateClipRectWithAspectRatio(DragPoint dragPoint, Point diffPos)
        {
            if (KeepAspectRatio)
            {
                if (Math.Abs(diffPos.X / diffPos.Y) > AspectRatio)
                {
                    if (dragPoint == DragPoint.UpperLeft || dragPoint == DragPoint.LowerRight)
                        diffPos.Y = diffPos.X / AspectRatio;
                    else
                        diffPos.Y = -diffPos.X / AspectRatio;
                }
                else
                {
                    if (dragPoint == DragPoint.UpperLeft || dragPoint == DragPoint.LowerRight)
                        diffPos.X = diffPos.Y * AspectRatio;
                    else
                        diffPos.X = -diffPos.Y * AspectRatio;
                }
            }

            var startPoint = new Point(X1, Y1);
            var endPoint = new Point(X2, Y2);
            switch (dragPoint)
            {
                case DragPoint.UpperLeft:
                    startPoint.X += diffPos.X;
                    startPoint.Y += diffPos.Y;
                    break;
                case DragPoint.UpperRight:
                    endPoint.X += diffPos.X;
                    startPoint.Y += diffPos.Y;
                    break;
                case DragPoint.LowerLeft:
                    startPoint.X += diffPos.X;
                    endPoint.Y += diffPos.Y;
                    break;
                case DragPoint.LowerRight:
                    endPoint.X += diffPos.X;
                    endPoint.Y += diffPos.Y;
                    break;
            }

            if (_limitedRect.Contains(startPoint) && _limitedRect.Contains(endPoint))
            {
                var canvasRect = new Rect(0, 0, CanvasWidth, CanvasHeight);
                var newRect = new Rect(startPoint, endPoint);
                canvasRect.Union(newRect);
                if (canvasRect.X < 0 || canvasRect.Y < 0 || canvasRect.Width > CanvasWidth ||
                    canvasRect.Height > CanvasHeight)
                {
                    var inverseImageTransform = ImageTransform.Inverse;
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
                    X1 = startPoint.X;
                    Y1 = startPoint.Y;
                    X2 = endPoint.X;
                    Y2 = endPoint.Y;
                }
            }
        }

        private void UpdateMaskArea()
        {
            _maskArea.Children.Clear();
            _maskArea.Children.Add(new RectangleGeometry
            {
                Rect = new Rect(-_layoutGrid.Padding.Left, -_layoutGrid.Padding.Top, _layoutGrid.ActualWidth,
                    _layoutGrid.ActualHeight)
            });
            _maskArea.Children.Add(new RectangleGeometry {Rect = new Rect(new Point(X1, Y1), new Point(X2, Y2))});
            MaskArea = _maskArea;
            _layoutGrid.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, _layoutGrid.ActualWidth,
                    _layoutGrid.ActualHeight)
            };
        }

        #endregion

        #region DependencyProperty Fields

        private static void OnSourceImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            target.InitImageLayout();
        }

        private static void OnAspectRatioChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            if (target.KeepAspectRatio)
            {
                var inverseImageTransform = target.ImageTransform.Inverse;
                if (inverseImageTransform != null)
                {
                    var selectedRect = new Rect(new Point(target.X1, target.Y1), new Point(target.X2, target.Y2));
                    var uniformSelectedRect = selectedRect.GetUniformRect(target.AspectRatio);
                    target._currentClipRect = inverseImageTransform.TransformBounds(uniformSelectedRect);
                    target.UpdateImageLayout();
                }
            }
        }

        private static void OnSelectedRectChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            if (!target._changeByCode)
            {
                target._changeByCode = true;
                if (target.X2 - target.X1 < target.MinSelectSize.Width ||
                    target.Y2 - target.Y1 < target.MinSelectSize.Height)
                    d.SetValue(e.Property, e.OldValue);
                target._changeByCode = false;
            }

            target.UpdateMaskArea();
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

        public double X1
        {
            get => (double) GetValue(X1Property);
            set => SetValue(X1Property, value);
        }


        public double Y1
        {
            get => (double) GetValue(Y1Property);
            set => SetValue(Y1Property, value);
        }

        public double X2
        {
            get => (double) GetValue(X2Property);
            set => SetValue(X2Property, value);
        }

        public double Y2
        {
            get => (double) GetValue(Y2Property);
            set => SetValue(Y2Property, value);
        }

        public GeometryGroup MaskArea
        {
            get => (GeometryGroup) GetValue(MaskAreaProperty);
            set => SetValue(MaskAreaProperty, value);
        }


        public CompositeTransform ImageTransform
        {
            get => (CompositeTransform) GetValue(ImageTransformProperty);
            set => SetValue(ImageTransformProperty, value);
        }

        // Using a DependencyProperty as the backing store for AspectRatio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register("AspectRatio", typeof(double), typeof(ImageCropper),
                new PropertyMetadata(-1d, OnAspectRatioChanged));

        // Using a DependencyProperty as the backing store for X1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty X1Property =
            DependencyProperty.Register("X1", typeof(double), typeof(ImageCropper),
                new PropertyMetadata(0d, OnSelectedRectChanged));

        // Using a DependencyProperty as the backing store for Y1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Y1Property =
            DependencyProperty.Register("Y1", typeof(double), typeof(ImageCropper),
                new PropertyMetadata(0d, OnSelectedRectChanged));

        // Using a DependencyProperty as the backing store for X2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty X2Property =
            DependencyProperty.Register("X2", typeof(double), typeof(ImageCropper),
                new PropertyMetadata(20d, OnSelectedRectChanged));

        // Using a DependencyProperty as the backing store for Y2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Y2Property =
            DependencyProperty.Register("Y2", typeof(double), typeof(ImageCropper),
                new PropertyMetadata(20d, OnSelectedRectChanged));

        // Using a DependencyProperty as the backing store for MaskArea.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaskAreaProperty =
            DependencyProperty.Register("MaskArea", typeof(GeometryGroup), typeof(ImageCropper),
                new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for SourceImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceImageProperty =
            DependencyProperty.Register("SourceImage", typeof(WriteableBitmap), typeof(ImageCropper),
                new PropertyMetadata(null, OnSourceImageChanged));

        // Using a DependencyProperty as the backing store for ImageTransform.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageTransformProperty =
            DependencyProperty.Register("ImageTransform", typeof(CompositeTransform), typeof(ImageCropper),
                new PropertyMetadata(default(CompositeTransform)));

        #endregion
    }
}