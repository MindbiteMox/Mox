using System.Collections.Generic;

namespace Mindbite.Mox.HtmlTable
{
    public abstract class OptionsBase
    {
        public OptionsBase()
        {
            TextAlign = TextAlign.Left;
        }

        public string? Css { get; set; }
        public TextAlign TextAlign { get; set; }

        public bool IsBold { get; set; }
    }

    public abstract class OptionsForSetBase : OptionsBase
    {
        public bool IsHead { get; set; }
        public bool IsSticky { get; set; }
        /// <summary>
        /// For Rows, add a class with top: [X]px; 
        /// For Cols, add a class with left: [x]px;
        /// If you have multiple sticky rows the top row will have top: 0px; and then the next row needs top: [height_of_top_row]px;
        /// If you have multiple sticky cols the leftmost col will have left: 0px; and then the next col needs left: [width_of_leftmost_col]px;
        /// </summary>
        public string? StickyCssClass { get; set; }
    }

    public class CellOptions : OptionsBase
    {
        public CellOptions() : base()
        {
            ColSpan = 1;
        }
        public int ColSpan { get; set; }
        public bool HasBorderTop { get; set; }
        public bool HasBorderBottom { get; set; }
        public bool HasBorderLeft { get; set; }
        public bool HasBorderRight { get; set; }
    }

    public class RowOptions : OptionsForSetBase
    {
        public RowOptions() : base()
        {
            RowsToToggle = new List<int>();
            StartToggled = true;
        }
        public bool HasRowToggler { get; set; }
        public bool StartToggled { get; set; }
        public bool HasBorderTop { get; set; }
        public bool HasBorderBottom { get; set; }

        public IList<int>? RowsToToggle { get; set; }
    }

    public class ColOptions : OptionsForSetBase
    {
        public ColOptions() : base()
        { }

        public bool HasBorderLeft { get; set; }
        public bool HasBorderRight { get; set; }
    }

    public class HtmlTableOptions : OptionsBase
    {
        public HtmlTableOptions() : base()
        { }

        /// <summary>
        /// Add a unique identifier if you use more than one table on the same page
        /// </summary>
        public string UniqueId { get; set; } = "MoxHtmlTable";
        public string? Title { get; set; }
        public bool HasChart { get; set; }
        public string? ChartTitle { get; set; }
        /// <summary>
        /// Add a <canvas></canvas> with this Id where you want to show the chart
        /// </summary>
        public string? ChartCanvasId { get; set; }

    }
}
