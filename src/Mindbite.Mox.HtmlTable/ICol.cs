using System.Collections.Generic;

namespace Mindbite.Mox.HtmlTable
{
    public interface ICol
    {
        public ICell? this[int rowIndex] { get; set; }
        public int Index { get; init; }
        public ColOptions Options { get; set; }

        public IList<ICell?> Values { get; }
    }
}
