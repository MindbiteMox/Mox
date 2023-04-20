using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Mindbite.Mox.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Mindbite.Mox.UI
{
    public class DataTableRenderer
    {
        private readonly MoxHtmlExtensionCollection _htmlExtensions;

        public DataTableRenderer(MoxHtmlExtensionCollection htmlExtensions)
        {
            this._htmlExtensions = htmlExtensions;
        }

        public async Task<IHtmlContent> RenderAsync(IDataTable dataTable, Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary viewData = null)
        {
            return await this._htmlExtensions.HtmlHelper.PartialAsync("Mox/UI/DataTable", dataTable, viewData ?? this._htmlExtensions.HtmlHelper.ViewData);
        }
    }

    public interface IDataTable
    {
        int PageCount { get; }
        int Page { get; }
        string SortColumn { get; }
        string SortDirection { get; }
        bool Sortable { get; }
        string CssClass { get; }
        string GetRowCssClass(object row);
        HtmlString EmptyMessage { get; }
        public bool EnableSelection { get; }

        IEnumerable<IDataTableColumn> Columns { get; }
        IEnumerable<IDataTableButton> Buttons { get; }

        Task<IEnumerable<dynamic>> GetRowDataAsync();
        string GetRowLink(object rowData);
        string GetRowId(object rowData);
        bool IsGrouped { get; }
        string GetGroupValue(object o);

        DataTableActionResult GetActionResult(Controller controller, string viewName = null);
    }

    public struct RowValue
    {
        public object Rendered;
        public object Raw;
    }

    public interface IDataTableColumn
    {
        string Title { get; }
        int Width { get; }
        string FieldName { get; }
        object GetValue(object o);
        RowValue GetValue2(object o);
        string GetCssClass(object row, object property);
        ColumnAlign Align { get; }
        Func<object, object, HtmlString> Renderer { get; }
        string GetNextSortDirection(IDataTable table);
    }

    public interface IDataTableButton
    {
        string Title(object rowData);
        string CssClass(object rowData);
        string GetAction(object rowData);
        bool OpenInNewTab(object rowData);
        HtmlString? Text(object rowData);
        bool Show(object rowData);
        Func<object, HtmlString>? Renderer { get; }
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

        public DataTableButton DeleteButton(Func<T, string> action)
        {
            return this.Add(action).CssClass("delete").Title("Radera");
        }

        public DataTableButton EditButton(Func<T, string> action)
        {
            return this.Add(action).CssClass("edit").Title("Redigera");
        }

        public class DataTableButton : IDataTableButton
        {
            private Func<T, string> _titleFunc = _ => "";
            private Func<T, string> _cssClassFunc = _ => "";
            private Func<T, bool> _openInNewTabFunc = _ => false;
            private Func<T, HtmlString?> _textFunc = _ => null;
            private Func<T, string> _actionFunc = _ => "";
            private Func<T, bool> _showFunc = _ => true;
            private Func<T, HtmlString>? _renderer;

            string IDataTableButton.Title(object row) => this._titleFunc((T)row);
            string IDataTableButton.CssClass(object row) => this._cssClassFunc((T)row);
            bool IDataTableButton.OpenInNewTab(object row) => this._openInNewTabFunc((T)row);
            HtmlString? IDataTableButton.Text(object row) => this._textFunc((T)row);
            bool IDataTableButton.Show(object row) => this._showFunc((T)row);
            Func<object, HtmlString>? IDataTableButton.Renderer => this._renderer == null ? null : o => this._renderer((T)o);

            public DataTableButton(Func<T, string> action)
            {
                this._actionFunc = action;
            }

            public DataTableButton Title(string title)
            {
                this._titleFunc = _ => title;
                return this;
            }

            public DataTableButton Title(Func<T, string> titleFunc)
            {
                this._titleFunc = titleFunc;
                return this;
            }

            public DataTableButton CssClass(string cssClass)
            {
                this._cssClassFunc = _ => cssClass;
                return this;
            }

            public DataTableButton CssClass(Func<T, string> cssClassFunc)
            {
                this._cssClassFunc = cssClassFunc;
                return this;
            }

            public DataTableButton OpenInNewTab(bool newTab)
            {
                this._openInNewTabFunc = _ => newTab;
                return this;
            }

            public DataTableButton OpenInNewTab(Func<T, bool> newTabFunc)
            {
                this._openInNewTabFunc = newTabFunc;
                return this;
            }

            public DataTableButton Text(string text)
            {
                this._textFunc = _ => new HtmlString(System.Web.HttpUtility.HtmlEncode(text));
                return this;
            }

            public DataTableButton Text(HtmlString text)
            {
                this._textFunc = _ => text;
                return this;
            }

            public DataTableButton Text(Func<T, HtmlString> textFunc)
            {
                this._textFunc = textFunc;
                return this;
            }

            public DataTableButton Show(bool show)
            {
                this._showFunc = _ => show;
                return this;
            }

            public DataTableButton Show(Func<T, bool> showFunc)
            {
                this._showFunc = showFunc;
                return this;
            }

            public DataTableButton Render(Func<T, HtmlString> renderer)
            {
                this._renderer = renderer;
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
            private Func<TProperty, T, string> _cssClass;
            private Func<object, object, HtmlString> _renderer;
            private ColumnAlign _align = ColumnAlign.Left;

            string IDataTableColumn.Title => this._title;
            int IDataTableColumn.Width => this._width;
            public string FieldName => this._fieldName;
            Func<object, object, HtmlString> IDataTableColumn.Renderer => this._renderer;
            ColumnAlign IDataTableColumn.Align => this._align;

            object IDataTableColumn.GetValue(object o)
            {
                if (this._renderer != null)
                    return this._renderer(this._compiledField((T)o), o);
                return this._compiledField((T)o);
            }

            RowValue IDataTableColumn.GetValue2(object o)
            {
                var value = this._compiledField((T)o);
                if (this._renderer != null)
                    return new RowValue { Rendered = this._renderer(value, o), Raw = value };
                return new RowValue { Rendered = value, Raw = value };
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
                this.CssClass(_ => cssClass);
                return this;
            }

            public DataTableColumn<TProperty> CssClass(Func<TProperty, string> cssClass)
            {
                this.CssClass((x, _) => cssClass(x));
                return this;
            }

            public DataTableColumn<TProperty> CssClass(Func<TProperty, T, string> cssClass)
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

            public string GetCssClass(object row, object property)
            {
                return this._cssClass != null ? this._cssClass((TProperty)property, (T)row) : null;
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
            private Expression<Func<T, object>> _rowId;
            private int? _pageCount;
            private List<IDataTableButton> _buttons;
            private bool _sortable;
            private string _cssClass;
            private Func<T, string> _rowCssClass;
            private string _groupMember;
            private Func<T, object> _groupMemberFunc;
            private Func<object, HtmlString> _groupMemberRender;
            private HtmlString _emptyMessage;
            private bool _enableSelection;

            private Lazy<Func<T, object>> _rowIdFunc => new(() => this._rowId.Compile());

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

            bool IDataTable.EnableSelection => this._enableSelection;

            string IDataTable.GetGroupValue(object o)
            {
                object value = this._groupMemberFunc((T)o);
                if(this._groupMemberRender != null)
                {
                    return this._groupMemberRender(value).Value ?? "";
                }
                return value?.ToString() ?? "";
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
                return this._rowIdFunc.Value((T)rowData).ToString();
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
                    this._sortColumn = Utils.Dynamics.GetFullPropertyName(defaultColumn);
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

            public QueryableDataTable<T> RowId(Expression<Func<T, object>> rowId)
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

            public QueryableDataTable<T> RowCssClass(Func<T, string> cssClass)
            {
                this._rowCssClass = cssClass;
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

            public QueryableDataTable<T> EnableSelection(bool enableSelection)
            {
                this._enableSelection = enableSelection;
                return this;
            }

            public QueryableDataTable<T> GroupBy<TProperty>(Expression<Func<T, TProperty>> groupFunc, Func<TProperty, HtmlString> render)
            {
                this._groupMemberRender = (object o) => render((TProperty)o);
                return this.GroupBy(groupFunc);
            }
            
            string IDataTable.GetRowCssClass(object row)
            {
                return this._rowCssClass != null ? this._rowCssClass((T)row) : null;
            }

            DataTableActionResult IDataTable.GetActionResult(Controller controller, string viewName) => this.GetActionResult(controller, viewName);

            public DataTableActionResult<T> GetActionResult(Controller controller, string viewName = null)
            {
                return new DataTableActionResult<T>
                {
                    ViewName = viewName ?? "Mox/UI/DataTable",
                    DataTable = this,
                    Controller = controller
                };
            }

            public async Task<IEnumerable<int>> GetAllRowIdsAsync()
            {
                return await this._dataSource.Select(this._rowId).Cast<int>().ToListAsync();
            }
        }
        
        public static QueryableDataTable<T> Create<T>(IQueryable<T> dataSource)
        {
            return new QueryableDataTable<T>().DataSource(dataSource);
        }
    }

    public abstract class DataTableActionResult : ActionResult
    {
        protected IDataTable _dataTable;
        public IDataTable DataTable { get => _dataTable; set => _dataTable = value; }
        public string ViewName { get; set; }
        public Controller Controller { get; set; }
    }

    public class DataTableActionResult<T> : DataTableActionResult
    {
        public new DataTableBuilder.QueryableDataTable<T> DataTable { get => (DataTableBuilder.QueryableDataTable<T>)this._dataTable; set => _dataTable = value; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (((IDataTable)this.DataTable).EnableSelection)
            {
                context.HttpContext.Response.Headers["X-Mox-DataTable-Selection"] = "1";
            }

            if(context.HttpContext.Request.Method == "POST")
            {
                var selection = await JsonSerializerExtensions.DeserializeAnonymousObjectAsync(context.HttpContext.Request.BodyReader.AsStream(), new
                {
                    selectedIds = Array.Empty<int>()
                });

                var allSelectedIds = selection.selectedIds.ToHashSet();
                var allIds = (await this.DataTable.GetAllRowIdsAsync()).ToHashSet();

                context.HttpContext.Response.Headers["X-Mox-DataTable-AllSelected"] = allIds.All(x => allSelectedIds.Contains(x)) ? "1" : "0";
            }

            this.Controller.ViewData.Model = this.DataTable;
            this.Controller.ViewData["ActionResult"] = this;

            var viewResult = new ViewResult
            {
                ViewName = ViewName,
                ViewData = this.Controller.ViewData,
                TempData = this.Controller.TempData
            };
            await viewResult.ExecuteResultAsync(context);
        }
    }
}
