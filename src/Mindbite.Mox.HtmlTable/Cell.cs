using System.Collections.Generic;

namespace Mindbite.Mox.HtmlTable
{
    public class Cell : ICell
    {
        public int ColIndex { get; set; }
        public string? Value { get; set; }
        public int MainCellColIndex { get; set; } = -1;
        public CellOptions Options { get; set; }

        public Cell() : this(null)
        { }

        public Cell(string? value) : this(value, new())
        { }

        public Cell(string? value, CellOptions options)
        {
            Value = value;
            Options = options;
        }

        public Cell(int colIndex, string? value, CellOptions options, int mainCellColIndex = -1)
        {
            ColIndex = colIndex;
            Value = value;
            MainCellColIndex = mainCellColIndex;
            Options = options;
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}

