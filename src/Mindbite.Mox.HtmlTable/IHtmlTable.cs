using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mindbite.Mox.HtmlTable
{
    public interface IHtmlTable
    {
        public IList<IRow> Rows { get; }
        public IList<ICol> Cols { get; }
        public HtmlTableOptions Options { get; set; }

        public IRow AddRow(RowOptions? options = null, params ICell?[] values);

        public ICol AddColumn(ColOptions? options = null, params ICell?[] values);

        public Task<byte[]> GetExcelBytes(string sheetName = "Blad1");

        public static ICell NewCell(string? value, CellOptions? options)
        {
            Cell cell;
            if (options == null)
            {
                cell = new(value);
            }
            else
            {
                cell = new(value, options);
            }
            return cell;
        }   
    }
}
