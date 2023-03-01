using System.Collections.Generic;
using System.Linq;

namespace Mindbite.Mox.HtmlTable
{
    internal class Col : ICol
    {
        private readonly IList<IList<ICell?>> _valueMatrix;
        public ColOptions Options { get; set; }
        public int Index { get; init; }

        public Col(IList<IList<ICell?>> valueMatrix, int index) : this(valueMatrix, index, new())
        { }

        public Col(IList<IList<ICell?>> valueMatrix, int index, ColOptions options)
        {
            _valueMatrix = valueMatrix;
            Index = index;
            Options = options;
        }

        public ICell? this[int rowIndex]
        {
            get { return _valueMatrix[rowIndex][Index]; }
            set { _valueMatrix[rowIndex][Index] = value; }
        }

        public IList<ICell?> Values => _valueMatrix.Select(x => x[Index]).ToList();
    }
}
