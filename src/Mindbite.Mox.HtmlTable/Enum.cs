using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mindbite.Mox.HtmlTable
{
    public enum TextAlign
    {
        [Display(Name = "mox-html-table-text-left")]
        Left,
        [Display(Name = "mox-html-table-text-center")]
        Center,
        [Display(Name = "mox-html-table-text-right")]
        Right
    }
}
