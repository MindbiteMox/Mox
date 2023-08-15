using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Mindbite.Mox.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.DemoApp.Areas.FormTest.ViewModels
{
    public enum TestFormBulkActions
    {
        Delete
    }

    [MoxFormBulkActions(typeof(TestFormBulkActions), "RunBulk")]
    public class TestForm
    {
        [MoxRequired]
        public string? Name { get; set; }

        [MoxFormFieldType(Render.CheckBoxList)]
        [MoxFormDataSource(nameof(GetItems))]
        public int[] CheckboxItems { get; set; } = Array.Empty<int>();

        [MoxFormFieldType(Render.Radio)]
        [MoxFormDataSource(nameof(GetItems))]
        public int? Radio { get; set; }

        [MoxFormFieldType(Render.EditorOnly)]
        public Mindbite.Mox.Images.ViewModels.EditorTemplates.MultiFile Files { get; set; } = new Images.ViewModels.EditorTemplates.MultiFile { FileTypeFullName = typeof(Images.Internal.Data._DummyFile).FullName };
        [MoxFormFieldType(Render.EditorOnly)]
        public Mindbite.Mox.Images.ViewModels.EditorTemplates.MultiImage Images { get; set; } = new Images.ViewModels.EditorTemplates.MultiImage { ImageTypeFullName = typeof(Images.Internal.Data._DummyImage).FullName };

        public static async Task<IEnumerable<SelectListItem>> GetItems(HttpContext context)
        {
            return new[]
            {
                new SelectListItem("Test 1", "1"),
                new SelectListItem("Test 2", "2"),
                new SelectListItem("Test 3", "3")
            };
        }
    }
}
