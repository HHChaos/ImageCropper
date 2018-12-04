using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ImageCropper.UWP
{
    public partial class ImageCropper
    {
        /// <summary>
        /// Gets or sets the minimum cropped length(in pixel).
        /// </summary>
        public double MinCroppedPixelLength { get; set; } = 40;
        /// <summary>
        /// Gets or sets the minimum selectable length.
        /// </summary>
        public double MinSelectedLength { get; set; } = 40;

        #region DependencyProperty Fields

        private static void OnSourceImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            if (e.NewValue is WriteableBitmap bitmap)
            {
                if (bitmap.PixelWidth > target.MinCropSize.Width && bitmap.PixelHeight > target.MinCropSize.Height)
                    target.InitImageLayout();
                else
                    throw new ArgumentException("The resolution of the image is too small!");
            }
        }

        private static void OnAspectRatioChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            target.UpdateAspectRatio();
        }

        private static void OnCircularCropChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            target.UpdateAspectRatio();
            target.UpdateControlButtonVisibility();
            target.UpdateMaskArea();
        }

        private static void OnIsSecondaryControlButtonVisibleChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper) d;
            target.UpdateControlButtonVisibility();
        }

        /// <summary>
        ///  Gets or sets the source of the cropped image.
        /// </summary>
        public WriteableBitmap SourceImage
        {
            get => (WriteableBitmap) GetValue(SourceImageProperty);
            set => SetValue(SourceImageProperty, value);
        }

        /// <summary>
        /// Gets or sets the aspect ratio of the cropped image，the default value is -1.
        /// </summary>
        public double AspectRatio
        {
            get => (double) GetValue(AspectRatioProperty);
            set => SetValue(AspectRatioProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to use a circular ImageCropper.
        /// </summary>
        public bool CircularCrop
        {
            get => (bool) GetValue(CircularCropProperty);
            set => SetValue(CircularCropProperty, value);
        }

        /// <summary>
        /// Gets or sets the mask on the cropped image.
        /// </summary>
        public Brush Mask
        {
            get => (Brush) GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        /// <summary>
        /// Gets or sets a value for the style to use for the primary control buttons of the ImageCropper.
        /// </summary>
        public Style PrimaryControlButtonStyle
        {
            get => (Style) GetValue(PrimaryControlButtonStyleProperty);
            set => SetValue(PrimaryControlButtonStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value for the style to use for the secondary control buttons of the ImageCropper.
        /// </summary>
        public Style SecondaryControlButtonStyle
        {
            get => (Style) GetValue(SecondaryControlButtonStyleProperty);
            set => SetValue(SecondaryControlButtonStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of secondary control buttons.
        /// </summary>
        public bool IsSecondaryControlButtonVisible
        {
            get => (bool) GetValue(IsSecondaryControlButtonVisibleProperty);
            set => SetValue(IsSecondaryControlButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AspectRatio"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register(nameof(AspectRatio), typeof(double), typeof(ImageCropper),
                new PropertyMetadata(-1d, OnAspectRatioChanged));

        /// <summary>
        /// Identifies the <see cref="SourceImage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceImageProperty =
            DependencyProperty.Register(nameof(SourceImage), typeof(WriteableBitmap), typeof(ImageCropper),
                new PropertyMetadata(null, OnSourceImageChanged));

        /// <summary>
        /// Identifies the <see cref="CircularCrop"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CircularCropProperty =
            DependencyProperty.Register(nameof(CircularCrop), typeof(bool), typeof(ImageCropper),
                new PropertyMetadata(false, OnCircularCropChanged));

        /// <summary>
        /// Identifies the <see cref="Mask"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(Brush), typeof(ImageCropper),
                new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Identifies the <see cref="PrimaryControlButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrimaryControlButtonStyleProperty =
            DependencyProperty.Register(nameof(PrimaryControlButtonStyle), typeof(Style), typeof(ImageCropper),
                new PropertyMetadata(default(Style)));

        /// <summary>
        /// Identifies the <see cref="SecondaryControlButtonStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryControlButtonStyleProperty =
            DependencyProperty.Register(nameof(SecondaryControlButtonStyle), typeof(Style), typeof(ImageCropper),
                new PropertyMetadata(default(Style)));

        /// <summary>
        /// Identifies the <see cref="IsSecondaryControlButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSecondaryControlButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsSecondaryControlButtonVisible), typeof(bool),
                typeof(ImageCropper),
                new PropertyMetadata(true, OnIsSecondaryControlButtonVisibleChanged));

        #endregion
    }
}