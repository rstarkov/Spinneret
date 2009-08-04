using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using RT.Servers;
using RT.TagSoup;
using RT.TagSoup.HtmlTags;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.Spinneret
{
    public class ReportTable
    {
        protected List<Row> _rows = new List<Row>();
        protected List<Col> _cols = new List<Col>();

        public List<Row> Rows
        {
            get { return _rows; }
        }

        public List<Col> Cols
        {
            get { return _cols; }
        }

        public Col AddCol(string title)
        {
            var col = new Col(title);
            _cols.Add(col);
            return col;
        }

        public Col AddCol(string title, string cssclass)
        {
            var col = new Col(title, cssclass);
            _cols.Add(col);
            return col;
        }

        public Row AddRow()
        {
            var row = new Row();
            _rows.Add(row);
            return row;
        }

        public Row AddRow(string cssclass)
        {
            var row = new Row(cssclass);
            _rows.Add(row);
            return row;
        }

        public Row AddRow(string cssclass, params object[] values)
        {
            var row = new Row(cssclass);
            _rows.Add(row);
            int i = 0;
            foreach (var col in _cols)
            {
                if (i >= values.Length)
                    break;
                else
                    row[col] = new Val(values[i]);
                i++;
            }
            return row;
        }

        public virtual object GetHtml()
        {
            if (_rows.Count == 0)
                return new P("There are no items to show.") { class_ = "rt-info" };

            List<Tag> rows = new List<Tag>();

            int rownum = 0;
            int nextHeader = 0;
            int lastDepth = int.MaxValue;
            foreach (var row in _rows)
            {
                if (nextHeader <= rownum && (row.Depth < lastDepth || row.Depth < 0))
                {
                    rows.Add(new TR() { class_ = "rt-row-header" }._(Cols.Select(col =>
                        new TD(col.Title) { class_ = MakeCssClass(col.CssClass) })));
                    nextHeader = rownum + 30;
                }

                string rowcss = MakeCssClass(
                    row.CssClass,
                    rownum % 2 == 0 ? "rt-row-even" : "rt-row-odd",
                    row.Depth >= 0 ? " rt-row-depth-" + row.Depth : null);

                rows.Add(new TR() { class_ = rowcss }._(_cols.Select(col =>
                {
                    var val = row[col];
                    if (val == null)
                        return new TD() { class_ = MakeCssClass(col.CssClass) };
                    else
                        return new TD(val.Content) { class_ = MakeCssClass(col.CssClass, val.CssClass) };
                })));

                lastDepth = row.Depth;
                rownum++;
            }

            return new TABLE(rows) { class_ = "rt-table" };
        }

        public string MakeCssClass(params string[] classes)
        {
            if (classes.Length == 0)
                return null;
            StringBuilder sb = new StringBuilder();
            foreach (var cls in classes)
                if (cls != null)
                {
                    sb.Append(cls);
                    sb.Append(' ');
                }
            if (sb.Length == 0)
                return null;
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public class Col
        {
            public Col(string title)
            {
                Title = title;
            }

            public Col(string title, string cssclass)
                : this(title)
            {
                CssClass = cssclass;
            }

            public string Title { get; set; }
            public string CssClass { get; set; }
        }

        public class Row
        {
            private Dictionary<Col, Val> _cells = new Dictionary<Col, Val>();

            public Row()
            {
                Depth = -1;
            }

            public Row(string cssclass)
                : this()
            {
                CssClass = cssclass;
            }

            public Val this[Col column]
            {
                get { return _cells.Get(column, null); }
                set { _cells[column] = value; }
            }

            public int Depth { get; set; }
            public string CssClass { get; set; }

            public Val SetValue(Col column, object content)
            {
                var value = new Val(content, null);
                _cells[column] = value;
                return value;
            }

            public Val SetValue(Col column, object content, string cssclass)
            {
                var value = new Val(content, cssclass);
                _cells[column] = value;
                return value;
            }
        }

        public class Val
        {
            public Val(object content)
            {
                Content = content;
                CssClass = null;
            }

            public Val(object content, string cssclass)
            {
                Content = content;
                CssClass = cssclass;
            }

            public object Content { get; set; }
            public string CssClass { get; set; }
        }

        public static string CssClassNumber(decimal number)
        {
            return number == 0m ? "rt-num-zero" : number > 0m ? "rt-num-pos" : "rt-num-neg";
        }
    }

    public interface IReportQueryableValue
    {
        string GetHtml();
    }

    public class ReportTableQueryable<T>
    {
        private HttpRequest _request;
        private IQueryable _items;
        private Dictionary<string, object> _externals;
        private bool _initialised = false;
        private List<string> _cols = new List<string>();
        Dictionary<string, object> _memberInfos = new Dictionary<string, object>();
        Dictionary<string, string> _select = null;
        private string _colsHtml;
        private string _formHtml;

        private double _tmr_init;
        private double _tmr_movefirst;
        private double _tmr_table;
        private double _tmr_gethtml;

        public string UrlPrefix = "";
        public string DefaultCols = "";
        public string DefaultWhere = "";
        public string DefaultOrderBy = "";

        /// <summary>
        /// Instantiates a report table.
        /// </summary>
        /// <param name="request">The request that this table is generated in response to. Used for constructing
        /// URLs for form action and possibly other items.</param>
        /// <param name="items">The collection of items to be made queryable.</param>
        public ReportTableQueryable(HttpRequest request, IQueryable<T> items)
        {
            _request = request;
            _items = items;
        }

        /// <summary>
        /// Instantiates a report table.
        /// </summary>
        /// <param name="request">The request that this table is generated in response to. Used for constructing
        /// URLs for form action and possibly other items.</param>
        /// <param name="items">The collection of items to be made queryable.</param>
        /// <param name="externals">A collection of extra objects to be available under the specified names.
        /// Supported types: delegates returning a value; typeof(&lt;a-static-type&gt;).</param>
        public ReportTableQueryable(HttpRequest request, IQueryable<T> items, Dictionary<string, object> externals)
            : this(request, items)
        {
            _externals = externals;
        }

        private void initialise()
        {
            if (_initialised)
                return;

            Ut.Tic();
            var _type = typeof(T);
            foreach (var field in _type.GetFields()) _memberInfos[field.Name] = field;
            foreach (var prop in _type.GetProperties()) _memberInfos[prop.Name] = prop;

            var cols_name = UrlPrefix + "cols";
            var where_name = UrlPrefix + "where";
            var orderby_name = UrlPrefix + "orderby";
            var cols_val = _request.Get.ContainsKey(cols_name) ? _request.Get[cols_name].Value : DefaultCols;
            var where_val = _request.Get.ContainsKey(where_name) ? _request.Get[where_name].Value : DefaultWhere;
            var orderby_val = _request.Get.ContainsKey(orderby_name) ? _request.Get[orderby_name].Value : DefaultOrderBy;
            var action = _request.SameUrlWhere(key => key != cols_name && key != where_name && key != orderby_name);

            foreach (var col_def in cols_val.Split(';').Select(str => str.Trim()))
            {
                if (!col_def.Contains('='))
                {
                    if (_memberInfos.ContainsKey(col_def))
                        _cols.Add(col_def);
                    else
                        throw new Exception("Field/property \"{0}\" not available in objects of type \"{1}\"".Fmt(col_def, typeof(T).Name));
                }
                else
                {
                    if (_select == null)
                        _select = new Dictionary<string, string>();
                    var eqpos = col_def.IndexOf('=');
                    var colname = col_def.Substring(0, eqpos).Trim();
                    var colexpr = col_def.Substring(eqpos + 1).Trim();
                    _select.Add(colname.EndsWith("*") ? colname.Substring(0, colname.Length - 1) : colname, colexpr);
                    if (!colname.EndsWith("*"))
                        _cols.Add(colname);
                }
            }

            Dictionary<string, object> externals = new Dictionary<string, object>();
            externals["str"] = (Expression<Func<object, string>>) (x => x == null ? null : x.ToString());
            externals["stre"] = (Expression<Func<object, string>>) (x => x == null ? "" : x.ToString());
            externals["bool"] = (Expression<Func<object, bool?>>) (x => RConvert.ExactToNullable.Bool(x));
            externals["int"] = (Expression<Func<object, int?>>) (x => RConvert.ExactToNullable.Int(x));
            externals["decimal"] = (Expression<Func<object, decimal?>>) (x => RConvert.ExactToNullable.Decimal(x));
            externals["double"] = (Expression<Func<object, double?>>) (x => RConvert.ExactToNullable.Double(x));
            externals["datetime"] = (Expression<Func<object, DateTime?>>) (x => RConvert.ExactToNullable.DateTime(x));
            if (_externals != null)
                foreach (var kvp in _externals)
                    externals.Add(kvp.Key, kvp.Value);

            if (_select != null)
            {
                var selstr = _memberInfos.Keys.Where(key => !_select.ContainsKey(key)).Concat(_select.Select(kvp => kvp.Value + " as " + kvp.Key)).JoinString(", ");
                _items = _items.Select("new(" + selstr + ")", externals);
            }

            _items = _items.Where(where_val, externals).OrderBy(orderby_val.Replace(';', ','), externals);

            _colsHtml = "<TR class='rt-row-header'>" + _cols.Select(col => "<TD>" + col + "</TD>").JoinString() + "</TR>";
            _formHtml =
                new FORM() { method = method.get, action = action }._(new TABLE() { class_ = "rt-noborder", style = "width:100%" }._(
                    new TR(new TD("Columns:"), new TD() { style = "width:100%" }._(new INPUT() { name = cols_name, type = itype.text, value = cols_val, style = "width:100%", maxlength = 99999 })),
                    new TR(new TD("Filter:"), new TD() { style = "width:100%" }._(new INPUT() { name = where_name, type = itype.text, value = where_val, style = "width:100%", maxlength = 99999 })),
                    new TR(new TD("Order by:"), new TD() { style = "width:100%" }._(new INPUT() { name = orderby_name, type = itype.text, value = orderby_val, style = "width:100%", maxlength = 99999 })),
                    new TR(new TD() { style = "vertical-align:middle" }._(new BUTTON("Apply") { type = btype.submit }), new TD(new STRONG("Available columns: "), _memberInfos.Keys.Order().JoinString(", ")) { style = "font-size:85%" })
                )).ToString();

            _initialised = true;
            _tmr_init = Ut.Toc();
        }

        public Tag GetHtml()
        {
            initialise();

            return new RAWHTML(getHtml());
        }

        public IEnumerable<string> getHtml()
        {
            Ut.Tic();
            var enumerator = getRowsHtml().GetEnumerator();
            var any = enumerator.MoveNext();
            _tmr_movefirst = Ut.Toc();

            yield return _formHtml;
            yield return new P(new HR()).ToString();

            if (!any)
                yield return (new P("There are no items to show.") { class_ = "rt-info" }).ToString();
            else
            {
                int rownum = -1;
                int nextHeader = 0;

                yield return "<TABLE class = 'rt-table'>";
                do
                {
                    rownum++;
                    if (nextHeader <= rownum)
                    {
                        yield return _colsHtml;
                        nextHeader = rownum + 30;
                    }

                    yield return enumerator.Current;
                } while (enumerator.MoveNext());
                yield return "</TABLE>";
                yield return (new P("{0} matches found.".Fmt(rownum + 1))).ToString();
            }

            _tmr_table = Ut.Toc() - _tmr_movefirst;
            yield return "<P style='font-size:70%'>init: {0:0.00}s, movefirst: {1:0.00}s, table: {2:0.00}s, gethtml: {3:0.00}s</P>".Fmt(_tmr_init, _tmr_movefirst, _tmr_table, _tmr_gethtml);
        }

        private IEnumerable<string> getRowsHtml()
        {
            Dictionary<string, PropertyInfo> propInfos = new Dictionary<string, PropertyInfo>();

            int rownum = -1;
            var items_enum = _items.GetEnumerator();
            while (items_enum.MoveNext())
            {
                rownum++;
                var item = items_enum.Current;

                StringBuilder rowstr = new StringBuilder(1024);
                rowstr.AppendFormat("<TR class = '{0}'>", rownum % 2 == 0 ? "rt-row-even" : "rt-row-odd");
                foreach (var col in _cols)
                {
                    object value;

                    if (_select == null)
                    {
                        object member = _memberInfos[col];
                        if (member is FieldInfo) value = (member as FieldInfo).GetValue(item);
                        else value = (member as PropertyInfo).GetValue(item, null);
                    }
                    else
                    {
                        if (!propInfos.ContainsKey(col))
                            propInfos[col] = item.GetType().GetProperty(col);
                        value = propInfos[col].GetValue(item, null);
                    }

                    if (value == null)
                        rowstr.Append("<TD><SPAN class='rt-null'>null</SPAN></TD>");
                    else if (value is IReportQueryableValue)
                    {
                        _tmr_gethtml -= Ut.Toc();
                        rowstr.Append("<TD>").Append((value as IReportQueryableValue).GetHtml()).Append("</TD>");
                        _tmr_gethtml += Ut.Toc();
                    }
                    else
                        rowstr.Append("<TD>").Append(value.ToString()).Append("</TD>");
                }
                rowstr.Append("</TR>");

                yield return rowstr.ToString();
            }
        }
    }
}
