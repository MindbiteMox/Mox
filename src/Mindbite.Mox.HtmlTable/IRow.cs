using Microsoft.AspNetCore.Html;
using System.Collections.Generic;

namespace Mindbite.Mox.HtmlTable
{
    public interface IRow
    {
        public ICell? this[int colIndex] { get; set; }

        public int Index { get; init; }

        public RowOptions Options { get; set; }

        public IList<ICell?> Values { get; }

        public HtmlString ToHtmlString();
    }
}
