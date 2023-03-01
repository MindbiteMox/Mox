using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Mindbite.Mox.Extensions;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.HtmlTable
{
    public class HtmlTable : IHtmlTable
    {
        public IList<IRow> Rows { get; init; } = new List<IRow>();
        public IList<ICol> Cols { get; init; } = new List<ICol>();

        private readonly IList<IList<ICell?>> _valueMatrix = new List<IList<ICell?>>();

        public HtmlTableOptions Options { get; set; }

        public HtmlTable() : this (new())
        { }

        public HtmlTable(HtmlTableOptions options)
        {
            Options = options;
        }

        public IRow AddRow(RowOptions? options = null, params ICell?[] values)
        {
            int valueColCount = values.Sum(x => x?.Options.ColSpan ?? 1);
            if (valueColCount > Cols.Count())
            {
                int colCount = Cols.Count();
                for (int i = 0; i < valueColCount - colCount; i++)
                {
                    AddColumn();
                }
            }
            else if (valueColCount < Cols.Count())
            {
                for (int i = 0; i < Cols.Count() - valueColCount; i++)
                {
                    values = values.Append(null).ToArray();
                }
            }

            Row row = new(_valueMatrix, Rows.Count(), Cols, options ?? new());
            Rows.Add(row);

            List<ICell?> cells = new List<ICell?>();
            foreach (var value in values) 
            {
                Cell cell = new Cell(cells.Count(), value?.Value, value?.Options ?? new());
                cells.Add(cell);
                if (cell.Options.ColSpan > 1)
                {
                    cells.AddRange(Enumerable.Range(1, cell.Options.ColSpan - 1).Select(i => new Cell(cell.ColIndex + i, null, new(), mainCellColIndex: cell.ColIndex)));
                }
            }

            _valueMatrix.Add(cells);

            return row;
        }

        public ICol AddColumn(ColOptions? options = null, params ICell?[] values)
        {
            if (values.Any(x => (x?.Options.ColSpan ?? 1) > 1))
            {
                // TODO build support for this
                throw new ArgumentException("Use AddRow if you want to add cells with colspan", nameof(values));
            }

            if (values.Length > Rows.Count())
            {
                int rowCount = Rows.Count();
                for (int i = 0; i < values.Length - rowCount; i++)
                {
                    AddRow();
                }
            }

            Col col = new(_valueMatrix, Cols.Count(), options ?? new());
            Cols.Add(col);

            foreach (Row row in Rows)
            {
                _valueMatrix[row.Index].Add(values.Length > row.Index ? values[row.Index] : null);
            }

            return col;
        }

        public async Task<byte[]> GetExcelBytes(string sheetName = "Blad1")
        {
            try
            {
                var workbook = new XSSFWorkbook();
                var sheet = (XSSFSheet)workbook.CreateSheet(sheetName);

                XSSFCellStyle fontStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                XSSFFont font = fontStyle.GetFont();
                XSSFFont boldFont = (XSSFFont)workbook.CreateFont();
                boldFont.CloneStyleFrom(font);
                boldFont.IsBold = true;
                
                foreach (IRow row in this.Rows)
                {
                    WriteRow(sheet, row, workbook, boldFont);
                }

                for (int i = 0; i < sheet.GetRow(0).LastCellNum; i++)
                {
                    sheet.AutoSizeColumn(i);
                    sheet.SetColumnWidth(i, Convert.ToInt32(sheet.GetColumnWidth(i) * 1.2));
                }

                byte[] fileContent;

                using (MemoryStream ms = new MemoryStream())
                {
                    workbook.Write(ms);
                    fileContent = ms.ToArray();
                }

                return fileContent;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        private void WriteRow(XSSFSheet sheet, IRow row, XSSFWorkbook workbook, XSSFFont boldFont)
        {
            var r = sheet.CreateRow(sheet.PhysicalNumberOfRows);

            int colIndex = 0;

            int mergeStart = 0;

            foreach (ICell? value in row.Values)
            {
                if (value != null && value.MainCellColIndex != -1)
                {
                    r.CreateCell(colIndex);
                    colIndex++;
                    continue;
                }

                var cell = r.CreateCell(colIndex);

                if (value != null && mergeStart != colIndex)
                {
                    var cra = new NPOI.SS.Util.CellRangeAddress(sheet.PhysicalNumberOfRows - 1, sheet.PhysicalNumberOfRows - 1, mergeStart - 1, colIndex - 1);
                    sheet.AddMergedRegion(cra);
                }

                bool isPercent = false;
                // figure out data type
                if (value?.Value != null)
                {
                    if (value.Value.Contains("%") && decimal.TryParse(value.Value.Replace("%", ""), out decimal percentValue))
                    {
                        // percent
                        cell.SetCellValue(Convert.ToDouble(percentValue) / 100);
                        isPercent = true;
                    }
                    else if (decimal.TryParse(value.Value, out decimal decimalValue))
                    {
                        // decimal
                        cell.SetCellValue(Convert.ToDouble(decimalValue));
                    }
                    else
                    {
                        // string
                        cell.SetCellValue(value.Value);
                    }
                }
                else
                {
                    cell.SetCellValue(string.Empty);
                }

                cell.CellStyle = GetCellStyle(workbook, value, isPercent, row.Options.IsHead || Cols[colIndex].Options.IsHead, 
                    (value?.Options.HasBorderTop ?? false) || row.Options.HasBorderTop, (value?.Options.HasBorderBottom ?? false) || row.Options.HasBorderBottom, (value?.Options.HasBorderLeft ?? false) || Cols[colIndex].Options.HasBorderLeft, (value?.Options.HasBorderRight ?? false) || Cols[colIndex].Options.HasBorderRight,
                    (value != null && value.Options.IsBold) || row.Options.IsBold || Cols[colIndex].Options.IsBold,
                    value?.Options.TextAlign ?? TextAlign.Left, boldFont);

                colIndex++;
                mergeStart = colIndex;
            }
        }

        private NPOI.SS.UserModel.HorizontalAlignment GetAlignment(TextAlign? textAlign)
        {
            return textAlign switch
            {
                TextAlign.Right => NPOI.SS.UserModel.HorizontalAlignment.Right,
                TextAlign.Center => NPOI.SS.UserModel.HorizontalAlignment.Center,
                TextAlign.Left => NPOI.SS.UserModel.HorizontalAlignment.Left,
                _ => NPOI.SS.UserModel.HorizontalAlignment.Left
            };
        }

        private XSSFCellStyle GetCellStyle(XSSFWorkbook workbook, ICell? cell, bool isPercent, bool isHead, bool hasBorderTop, bool hasBorderBottom, bool hasBorderLeft, bool hasBorderRight, bool isBold, TextAlign textAlign, XSSFFont boldFont)
        {
            XSSFCellStyle cellStyle = (XSSFCellStyle)workbook.CreateCellStyle();

            cellStyle.Alignment = GetAlignment(textAlign);

            if (isPercent)
            {
                cellStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00%");
            }

            if (isHead || isBold)
            {
                cellStyle.SetFont(boldFont);
            }

            if (hasBorderTop)
            {
                cellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            }

            if (hasBorderBottom)
            {
                cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            }

            if (hasBorderLeft)
            {
                cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            }

            if (hasBorderRight)
            {
                cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            }

            return cellStyle;
        }
    }
}
