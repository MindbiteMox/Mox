using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DemoApp.Areas.FormTest.ViewModels
{
    public class TestForm
    {
        [MoxRequired]
        public string? Name { get; set; }

        [MoxFormFieldType(Render.EditorOnly)]
        public Mindbite.Mox.Images.ViewModels.EditorTemplates.MultiFile Files { get; set; } = new Images.ViewModels.EditorTemplates.MultiFile { FileTypeFullName = typeof(Images.Internal.Data._DummyFile).FullName };
        [MoxFormFieldType(Render.EditorOnly)]
        public Mindbite.Mox.Images.ViewModels.EditorTemplates.MultiImage Images { get; set; } = new Images.ViewModels.EditorTemplates.MultiImage { ImageTypeFullName = typeof(Images.Internal.Data._DummyImage).FullName };
    }
}
