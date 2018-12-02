using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ImageCropper.UWP
{
    public partial class ImageCropper
    {
        public double MinCroppedPixelLength { get; set; } = 40;
        public double MinSelectedLength { get; set; } = 40;

        #region DependencyProperty Fields

        private static void OnSourceImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            if (e.NewValue is WriteableBitmap bitmap)
            {
                if (bitmap.PixelWidth > target.MinCropSize.Width && bitmap.PixelHeight > target.MinCropSize.Height)
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
            var target = (ImageCropper)d;
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
            get => (WriteableBitmap)GetValue(SourceImageProperty);
            set => SetValue(SourceImageProperty, value);
        }

        /// <summary>
        ///     Image aspect ratio，the default value is -1.
        /// </summary>
        public double AspectRatio
        {
            get => (double)GetValue(AspectRatioProperty);
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
