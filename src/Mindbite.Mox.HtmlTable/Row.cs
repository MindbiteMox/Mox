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

        private HtmlTable Table { get; init; }

        public RowOptions Options { get; set; }

        public int Index { get; init; }

        public IList<ICell?> Values => _valueMatrix[Index];

        public Row(IList<IList<ICell?>> valueMatrix, int index, IList<ICol> cols, HtmlTable table) : this(valueMatrix, index, cols, new(), table)
        { }

        public Row(IList<IList<ICell?>> valueMatrix, int index, IList<ICol> cols, RowOptions options, HtmlTable table)
        {
            _valueMatrix = valueMatrix;
            _cols = cols;
            Index = index;
            Options = options;
            Table = table;
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
                string tag = $"<tr id=\"Row_{Index}\" class=\"{Options.Css} {(Options.IsSticky ? $"mox-html-table-sticky-row mox-html-table-sticky-row-zindex {Options.StickyCssClass}" : "")}\">";

                foreach (ICell? value in Values)
                {

                    if (value != null && value.MainCellColIndex != -1)
                    {
                        colIndex++;
                        continue;
                    }
                    tag += $"<{GetTagTdOrTh(Options.IsHead, _cols[colIndex].Options.IsHead)} data-col-index=\"{colIndex}\"" +
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
            return colIndex == 0 && Options.HasRowToggler && Options.RowsToToggle != null && Options.RowsToToggle.Any() ? (Options.StartToggled ? $"<i onclick=\"HtmlTable.Table.toggleRows(this,'{Table.Options.UniqueId}',false,{string.Join(',', Options.RowsToToggle.Select(x => x.ToString()))})\" class=\"fas fa-minus\"></i>" : $"<i onclick=\"HtmlTable.Table.toggleRows(this,'{Table.Options.UniqueId}',true,{string.Join(',', Options.RowsToToggle.Select(x => x.ToString()))})\" class=\"fas fa-plus\"></i>") : "";
        }

        private object GetClass(ICell? value, ColOptions colOptions)
        {
            return $"class=\"{Options.Css} " +
                $"{(value != null ? value.Options.TextAlign.GetDescription() : "")} " +
                $"{(value != null && value.Options.HasBorderTop || Options.HasBorderTop ? "mox-html-table-border-top" : "")} " +
                $"{(value != null && value.Options.HasBorderBottom || Options.HasBorderBottom ? "mox-html-table-border-bottom" : "")} " +
                $"{(value != null && value.Options.HasBorderLeft || colOptions.HasBorderLeft ? "mox-html-table-border-left" : "")} " +
                $"{(value != null && value.Options.HasBorderRight || colOptions.HasBorderRight ? "mox-html-table-border-right" : "")} " +
                $"{(value != null && value.Options.IsBold || Options.IsBold || colOptions.IsBold ? "mox-html-table-font-bold" : "")} " +
                $"{(Options.IsHead ? "mox-html-table-head-row" : "")} " +
                $"{(colOptions.IsHead ? "mox-html-table-head-col" : "")} " +
                $"{(Options.IsSticky ? $"mox-html-table-sticky-row {Options.StickyCssClass}" : "")} " +
                $"{(colOptions.IsSticky ? $"mox-html-table-sticky-col {colOptions.StickyCssClass}" : "")}\" ";
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
