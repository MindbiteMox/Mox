using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Mindbite.Mox.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.EntityFrameworkCore;

namespace Mindbite.Mox.UI
{
    public interface IDataTable
    {
        int PageCount { get; }
        int Page { get; }
        string SortColumn { get; }
        string SortDirection { get; }
        bool Sortable { get; }
        string CssClass { get; }
        HtmlString EmptyMessage { get; }

        IEnumerable<IDataTableColumn> Columns { get; }
        IEnumerable<IDataTableButton> Buttons { get; }

        Task<IEnumerable<dynamic>> GetRowDataAsync();
        string GetRowLink(object rowData);
        string GetRowId(object rowData);
        bool IsGrouped { get; }
        string GetGroupValue(object o);
    }

    public interface IDataTableColumn
    {
        string Title { get; }
        int Width { get; }
        string FieldName { get; }
        object GetValue(object o);
        string CssClass { get; }
        ColumnAlign Align { get; }
        Func<object, object, HtmlString> Renderer { get; }
        string GetNextSortDirection(IDataTable table);
    }

    public interface IDataTableButton
    {
        string Title { get; }
        string CssClass { get; }
        string GetAction(object rowData);
    }

    public enum ColumnAlign
    {
        Left, Right, Center
    }

    public enum SortDirection
    {
        Ascending, Descending
    }

    public class DataTableSort
    {
        public string DataTableSortColumn { get; set; }
        public string DataTableSortDirection { get; set; }
        public int? DataTablePage { get; set; }
    }

    public class DataTableButtonFactory<T>
    {
        public List<IDataTableButton> Buttons { get; private set; }

        public DataTableButtonFactory()
        {
            this.Buttons = new List<IDataTableButton>();
        }

        public DataTableButton Add(Func<T, string> action)
        {
            var newField = new DataTableButton(action);
            this.Buttons.Add(newField);
            return newField;
        }

        public class DataTableButton : IDataTableButton
        {
            private string _title;
            private string _cssClass;
            private Func<T, string> _actionFunc;
            string IDataTableButton.Title => this._title;
            string IDataTableButton.CssClass => this._cssClass;

            public DataTableButton(Func<T, string> action)
            {
                this._actionFunc = action;
            }

            public DataTableButton Title(string title)
            {
                this._title = title;
                return this;
            }

            public DataTableButton CssClass(string cssClass)
            {
                this._cssClass = cssClass;
                return this;
            }

            string IDataTableButton.GetAction(object rowData)
            {
                return this._actionFunc((T)rowData);
            }
        }
    }

    public class DataTableFieldFactory<T>
    {
        public List<IDataTableColumn> Columns { get; private set; }

        public DataTableFieldFactory()
        {
            this.Columns = new List<IDataTableColumn>();
        }

        public class DataTableColumn<TProperty> : IDataTableColumn
        {
            private Expression<Func<T, TProperty>> _field;
            private Func<T, TProperty> _compiledField;
            private string _title;
            private int _width;
            private string _fieldName;
            private string _cssClass;
            private Func<object, object, HtmlString> _renderer;
            private ColumnAlign _align = ColumnAlign.Left;

            string IDataTableColumn.Title => this._title;
            int IDataTableColumn.Width => this._width;
            public string FieldName => this._fieldName;
            string IDataTableColumn.CssClass => this._cssClass;
            Func<object, object, HtmlString> IDataTableColumn.Renderer => this._renderer;
            ColumnAlign IDataTableColumn.Align => this._align;

            object IDataTableColumn.GetValue(object o)
            {
                if (this._renderer != null)
                    return this._renderer(this._compiledField((T)o), o);
                return this._compiledField((T)o);
            }

            public DataTableColumn(Expression<Func<T, TProperty>> field)
            {
                this._field = field;
                this._fieldName = Utils.Dynamics.GetFullPropertyName(field);
                this._compiledField = field.Compile();
            }

            public DataTableColumn<TProperty> Title(string title)
            {
                this._title = title;
                return this;
            }

            public DataTableColumn<TProperty> Width(int width)
            {
                this._width = width;
                return this;
            }

            public DataTableColumn<TProperty> CssClass(string cssClass)
            {
                this._cssClass = cssClass;
                return this;
            }

            public DataTableColumn<TProperty> Render(Func<TProperty, HtmlString> renderFunc)
            {
                this._renderer = (object x, object o) => renderFunc((TProperty)x);
                return this;
            }

            public DataTableColumn<TProperty> Render(Func<TProperty, T, HtmlString> renderFunc)
            {
                this._renderer = (object x, object o) => renderFunc((TProperty)x, (T)o);
                return this;
            }

            public DataTableColumn<TProperty> Align(ColumnAlign align)
            {
                this._align = align;
                return this;
            }

            public string GetNextSortDirection(IDataTable table)
            {
                if (table.SortColumn == this.FieldName)
                {
                    if(table.SortDirection.ToLower() == "ascending")
                        return "descending";
                }
                return "ascending";
            }
        }

        public DataTableColumn<TProperty> Add<TProperty>(Expression<Func<T, TProperty>> field)
        {
            var newField = new DataTableColumn<TProperty>(field);
            this.Columns.Add(newField);
            return newField;
        }
    }

    public static class DataTableBuilder
    {
        public class QueryableDataTable<T> : IDataTable
        {
            private IQueryable<T> _dataSource;
            private string _sortColumn;
            private string _sortDirection;
            private int _page;
            private int _pageSize;
            private List<IDataTableColumn> _columns;
            private Func<T, string> _rowLink;
            private Func<T, object> _rowId;
            private int? _pageCount;
            private List<IDataTableButton> _buttons;
            private bool _sortable;
            private string _cssClass;
            private string _groupMember;
            private Func<T, object> _groupMemberFunc;
            private Func<object, HtmlString> _groupMemberRender;
            private HtmlString _emptyMessage;

            public int PageCount => this._pageCount ?? (this._pageCount = ((this._dataSource.Count() - 1) / this._pageSize) + 1).Value;

            int IDataTable.Page => this._page;
            string IDataTable.SortColumn => this._sortColumn;
            string IDataTable.SortDirection => this._sortDirection;
            bool IDataTable.Sortable => this._sortable;
            string IDataTable.CssClass => this._cssClass;
            HtmlString IDataTable.EmptyMessage => this._emptyMessage;


            IEnumerable<IDataTableColumn> IDataTable.Columns => this._columns;
            IEnumerable<IDataTableButton> IDataTable.Buttons => this._buttons;

            bool IDataTable.IsGrouped => this._groupMemberFunc != null;

            string IDataTable.GetGroupValue(object o)
            {
                object value = this._groupMemberFunc((T)o);
                if(this._groupMemberRender != null)
                {
                    return this._groupMemberRender(value).Value;
                }
                return value.ToString();
            }

            internal QueryableDataTable()
            {
                this._pageSize = 35;
                this._page = 1;
                this._sortable = true;
                this._cssClass = "mox-datatable";
                this._columns = new List<IDataTableColumn>();
                this._buttons = new List<IDataTableButton>();
            }

            private IQueryable<dynamic> GetRowData()
            {
                if (this._dataSource == null)
                    throw new ArgumentNullException($"{nameof(this._dataSource)} cannot be null.");

                if (this._sortable && this._sortColumn == null)
                    throw new ArgumentNullException($"{nameof(this._sortColumn)} cannot be null.");

                if (this._sortable && this._sortDirection == null)
                    throw new ArgumentNullException($"{nameof(this._sortDirection)} cannot be null.");

                var pageData = this._dataSource;

                if (this._sortable)
                {
                    if (this._sortDirection.ToLower() == "ascending")
                    {
                        if (this._groupMember == null)
                        {
                            pageData = pageData.OrderBy(this._sortColumn);
                        }
                        else
                        {
                            pageData = pageData.OrderBy(this._groupMember).ThenBy(this._sortColumn);
                        }
                    }
                    else
                    {
                        if (this._groupMember == null)
                        {
                            pageData = pageData.OrderByDescending(this._sortColumn);
                        }
                        else
                        {
                            pageData = pageData.OrderBy(this._groupMember).ThenByDescending(this._sortColumn);
                        }
                    }
                }

                var result = pageData.Skip((Math.Min(this.PageCount, this._page) - 1) * this._pageSize).Take(this._pageSize);

                return (IQueryable<dynamic>)result;
            }

            public async Task<IEnumerable<dynamic>> GetRowDataAsync()
            {
                var data = this.GetRowData();
                if (!(data is IAsyncEnumerable<object>))
                    return data.ToList();
                return await data.ToListAsync();
            }

            public string GetRowLink(object rowData)
            {
                if (this._rowLink == null)
                    return null;
                return this._rowLink((T)rowData);
            }

            public string GetRowId(object rowData)
            {
                if (this._rowId == null)
                    return null;
                return this._rowId((T)rowData).ToString();
            }

            public QueryableDataTable<T> DataSource(IQueryable<T> dataSource)
            {
                this._dataSource = dataSource;
                return this;
            }

            [Obsolete("Use the other sort function with typed default parameters")]
            public QueryableDataTable<T> Sort(string column, string direction)
            {
                this._sortColumn = column;
                this._sortDirection = direction;
                return this;
            }

            public QueryableDataTable<T> Sort<TProperty>(Expression<Func<T, TProperty>> defaultColumn, SortDirection defaultDirection, string column = null, string direction = null)
            {
                this._sortColumn = column ?? Utils.Dynamics.GetFullPropertyName(defaultColumn);
                this._sortDirection = direction ?? defaultDirection.ToString();

                try
                {
                    Utils.Dynamics.GetLambdaExpression(typeof(T), this._sortColumn.Split('.'));
                }
                catch
                {
                    if (!string.IsNullOrWhiteSpace(column))
                    {
                        this._sortColumn = Utils.Dynamics.GetFullPropertyName(defaultColumn); // User error
                    }
                    else
                    {
                        throw; // Programmer error
                    }
                }

                return this;
            }

            public QueryableDataTable<T> Page(int? page)
            {
                this._page = page ?? 1;
                return this;
            }

            public QueryableDataTable<T> PageSize(int pageSize)
            {
                this._pageSize = pageSize;
                return this;
            }

            public QueryableDataTable<T> Columns(Action<DataTableFieldFactory<T>> columns)
            {
                var factory = new DataTableFieldFactory<T>();
                columns(factory);
                this._columns = factory.Columns;
                return this;
            }

            public QueryableDataTable<T> Buttons(Action<DataTableButtonFactory<T>> buttons)
            {
                var factory = new DataTableButtonFactory<T>();
                buttons(factory);
                this._buttons = factory.Buttons;
                return this;
            }

            public QueryableDataTable<T> RowLink(Func<T, string> rowLink)
            {
                this._rowLink = rowLink;
                return this;
            }

            public QueryableDataTable<T> RowId(Func<T, object> rowId)
            {
                this._rowId = rowId;
                return this;
            }

            public QueryableDataTable<T> Sortable(bool sortable)
            {
                this._sortable = sortable;
                return this;
            }

            public QueryableDataTable<T> CssClass(string cssClass)
            {
                this._cssClass = cssClass;
                return this;
            }

            public QueryableDataTable<T> EmptyMessage(HtmlString emptyMessage)
            {
                this._emptyMessage = emptyMessage;
                return this;
            }

            public QueryableDataTable<T> GroupBy<TProperty>(Expression<Func<T, TProperty>> groupFunc)
            {
                var compiledFunc = groupFunc.Compile();
                this._groupMemberFunc = t => compiledFunc(t);
                this._groupMember = Utils.Dynamics.GetFullPropertyName(groupFunc);
                return this;
            }

            public QueryableDataTable<T> GroupBy<TProperty>(Expression<Func<T, TProperty>> groupFunc, Func<TProperty, HtmlString> render)
            {
                this._groupMemberRender = (object o) => render((TProperty)o);
                return this.GroupBy(groupFunc);
            }
        }
        
        public static QueryableDataTable<T> Create<T>(IQueryable<T> dataSource)
        {
            return new QueryableDataTable<T>().DataSource(dataSource);
        }
    }
}
