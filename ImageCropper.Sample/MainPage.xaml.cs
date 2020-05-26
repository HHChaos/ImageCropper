using ImageCropper.UWP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace ImageCropper.Sample
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/test.jpg"));
            await ImageCropper.LoadImageFromFile(file);
            var itemSource = new List<AspectRatioConfig>
                {
                    new AspectRatioConfig
                    {
                        Name = "Custom",
                        AspectRatio = -1
                    },
                    new AspectRatioConfig
                    {
                        Name = "Square",
                        AspectRatio = 1
                    },
                    new AspectRatioConfig
                    {
                        Name = "Landscape(16:9)",
                        AspectRatio = 16d / 9d
                    },
                    new AspectRatioConfig
                    {
                        Name = "Portrait(9:16)",
                        AspectRatio = 9d / 16d
                    },
                    new AspectRatioConfig
                    {
                        Name = "4:3",
                        AspectRatio = 4d / 3d
                    },
                    new AspectRatioConfig
                    {
                        Name = "3:2",
                        AspectRatio = 3d / 2d
                    }
                };
            AspectRatioComboBox.ItemsSource = itemSource;
            AspectRatioComboBox.DisplayMemberPath = "Name";
            AspectRatioComboBox.SelectedValuePath = "AspectRatio";
            AspectRatioComboBox.SelectedIndex = 0;
            AspectRatioComboBox.SelectionChanged += this.AspectRatioComboBox_SelectionChanged;
        }

        private void AspectRatioComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var aspectRatio = (double)AspectRatioComboBox.SelectedValue;
            ImageCropper.AspectRatio = aspectRatio;
        }

        private async void PickImgButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
                await ImageCropper.LoadImageFromFile(file);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = "Crop_Image"
            };
            picker.FileTypeChoices.Add("Png Picture", new List<string> { ".png", ".jpg" });
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                if (file.Name.Contains(".png"))
                {
                    await ImageCropper.SaveAsync(file, BitmapFileFormat.Png);
                }
                else
                {
                    await ImageCropper.SaveAsync(file, BitmapFileFormat.Jpeg);
                }
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ImageCropper.Reset();
        }

        private void TrySetRegionButton_Click(object sender, RoutedEventArgs e)
        {
            var text = RegionTextBox.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                var points = text.Replace(" ", "").Split(',');
                if (points.Length == 4)
                {
                    var x = Int32.Parse(points[0]);
                    var y = Int32.Parse(points[1]);
                    var width = Int32.Parse(points[2]);
                    var height = Int32.Parse(points[3]);
                    var ret = ImageCropper.TrySetCroppedRegion(new Rect(x, y, width, height));
                }
            }
        }
    }
}
