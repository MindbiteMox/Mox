using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images.Attributes
{
    public enum ImageSizeMode
    {
        KeepAspectRatio, Cover, Contain
    }

    public enum ImageAlphaMode
    {
        Retain, ClearToWhite, ClearToBlack
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ImageSizeAttribute : Attribute
    {
        public string Name { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public ImageSizeMode SizeMode { get; set; } = ImageSizeMode.Cover;
        public ImageAlphaMode AlphaMode { get; set; } = ImageAlphaMode.Retain;

        public ImageSizeAttribute(string name, int width, int height, ImageSizeMode sizeMode, ImageAlphaMode alphaMode)
        {
            this.Name = name;
            this.Width = width > 0 ? width : default;
            this.Height = height > 0 ? height : default;
            this.SizeMode = sizeMode;
            this.AlphaMode = alphaMode;
        }
    }
}
