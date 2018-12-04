using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCropper.UWP
{
    public partial class ImageCropper
    {
        /// <summary>
        /// Key of the root layout container.
        /// </summary>
        private const string LayoutGridName = "PART_LayoutGrid";
        /// <summary>
        /// Key of the Canvas that contains the image.
        /// </summary>
        private const string ImageCanvasPartName = "PART_ImageCanvas";
        /// <summary>
        /// Key of the Image Control inside the ImageCropper Control
        /// </summary>
        private const string SourceImagePartName = "PART_SourceImage";
        /// <summary>
        /// Key of the mask layer.
        /// </summary>
        private const string MaskAreaPathPartName = "PART_MaskAreaPath";
        /// <summary>
        /// Key of the UI Element that on the top.
        /// </summary>
        private const string TopButtonPartName = "PART_TopButton";
        /// <summary>
        /// Key of the UI Element on the bottom.
        /// </summary>
        private const string BottomButtonPartName = "PART_BottomButton";
        /// <summary>
        /// Key of the UI Element on the left.
        /// </summary>
        private const string LeftButtonPartName = "PART_LeftButton";
        /// <summary>
        /// Key of the UI Element on the right.
        /// </summary>
        private const string RightButtonPartName = "PART_RightButton";
        /// <summary>
        /// Key of the UI Element that on the upper left.
        /// </summary>
        private const string UpperLeftButtonPartName = "PART_UpperLeftButton";
        /// <summary>
        /// Key of the UI Element that on the upper right.
        /// </summary>
        private const string UpperRightButtonPartName = "PART_UpperRightButton";
        /// <summary>
        /// Key of the UI Element that on the lower left.
        /// </summary>
        private const string LowerLeftButtonPartName = "PART_LowerLeftButton";
        /// <summary>
        /// Key of the UI Element that on the lower right.
        /// </summary>
        private const string LowerRightButtonPartName = "PART_LowerRightButton";
    }
}
