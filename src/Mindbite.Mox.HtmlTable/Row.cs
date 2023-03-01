using Microsoft.AspNetCore.Html;
using Mindbite.Mox.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mindbite.Mox.HtmlTable
{
    internal class Row : IRow
    {
        private readonly IList<ICol> _cols;
        private readonly IList<IList<ICell?>> _valueMatrix;

        public RowOptions Options { get; set; }

        public int Index { get; init; }

        public IList<ICell?> Values => _valueMatrix[Index];

        public Row(IList<IList<ICell?>> valueMatrix, int index, IList<ICol> cols) : this(valueMatrix, index, cols, new())
        { }

        public Row(IList<IList<ICell?>> valueMatrix, int index, IList<ICol> cols, RowOptions options)
        {
            _valueMatrix = valueMatrix;
            _cols = cols;
            Index = index;
            Options = options;
        }

        public ICell? this[int colIndex]
        {
            get { return _valueMatrix[Index][colIndex]; }
            set { _valueMatrix[Index][colIndex] = value; }
        }

        public HtmlString ToHtmlString()
        {
            int colIndex = 0;
            try
            {
                string tag = $"<tr id=\"Row_{Index}\" class=\"{Options.Css} {(Options.IsSticky ? $"data-table-sticky-row data-table-sticky-row-zindex {Options.StickyCssClass}" : "")}\">";

                foreach (ICell? value in Values)
                {
                    if (value != null && value.MainCellColIndex != -1)
                    {
                        continue;
                    }
                    tag += $"<{GetTagTdOrTh(Options.IsHead, _cols[colIndex].Options.IsHead)} " +
                        $"{GetColSpan(value)} " +
                        $"{GetClass(value, _cols[colIndex].Options)}>" +
                        $"{GetToggler(colIndex)} {value}</{GetTagTdOrTh(Options.IsHead, _cols[colIndex].Options.IsHead)}>";
                    colIndex++;
                }
                tag += "</tr>";

                return new HtmlString(tag.Replace("   ", " ").Replace("  ", " ").Replace("\" ", "\"").Replace(" \"", "\""));
            }
            catch (Exception ex)
            {
                throw new Exception("valuescount: " + Values.Count().ToString() + "colcount: " + _cols.Count().ToString() + ". colindex: " + colIndex.ToString() + "\r\n\r\n" + ex.ToString());
            }
        }

        private object GetToggler(int colIndex)
        {
            return colIndex == 0 && Options.HasRowToggler && Options.RowsToToggle != null && Options.RowsToToggle.Any() ? (Options.StartToggled ? $"<i onclick=\"toggleRows(this, false, {string.Join(',', Options.RowsToToggle.Select(x => x.ToString()))})\" class=\"fas fa-minus\"></i>" : $"<i onclick=\"toggleRows(this, true, {string.Join(',', Options.RowsToToggle.Select(x => x.ToString()))})\" class=\"fas fa-plus\"></i>") : "";
        }

        private object GetClass(ICell? value, ColOptions colOptions)
        {
            return $"class=\"{Options.Css} " +
                $"{(value != null ? value.Options.TextAlign.GetDescription() : "")} " +
                $"{(value != null && value.Options.HasBorderTop || Options.HasBorderTop ? "data-table-border-top" : "")} " +
                $"{(value != null && value.Options.HasBorderBottom || Options.HasBorderBottom ? "data-table-border-bottom" : "")} " +
                $"{(value != null && value.Options.HasBorderLeft || colOptions.HasBorderLeft ? "data-table-border-left" : "")} " +
                $"{(value != null && value.Options.HasBorderRight || colOptions.HasBorderRight ? "data-table-border-right" : "")} " +
                $"{(value != null && value.Options.IsBold || Options.IsBold || colOptions.IsBold ? "data-table-font-bold" : "")} " +
                $"{(Options.IsHead ? "data-table-head-row" : "")} " +
                $"{(colOptions.IsHead ? "data-table-head-col" : "")} " +
                $"{(Options.IsSticky ? $"data-table-sticky-row {Options.StickyCssClass}" : "")} " +
                $"{(colOptions.IsSticky ? $"data-table-sticky-col {colOptions.StickyCssClass}" : "")}\" ";
        }

        private object GetColSpan(ICell? value)
        {
            return value != null && value.Options.ColSpan > 1 ? $"colspan=\"{value.Options.ColSpan}\" " : "";
        }

        private object GetTagTdOrTh(bool isCellHead, bool isColHead)
        {
            return isCellHead || isColHead ? "th" : "td";
        }
    }
}
