using System.Collections.Generic;

namespace Mindbite.Mox.HtmlTable
{
    public interface ICell
    {
        public int ColIndex { get; set; }
        public string? Value { get; set; }
        public int MainCellColIndex { get; set; }
        public CellOptions Options { get; set; }
    }
}
