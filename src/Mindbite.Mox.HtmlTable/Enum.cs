using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mindbite.Mox.HtmlTable
{
    public enum TextAlign
    {
        [Display(Name = "data-table-text-left")]
        Left,
        [Display(Name = "data-table-text-center")]
        Center,
        [Display(Name = "data-table-text-right")]
        Right
    }
}
