using System.Collections.Generic;
using RT.TagSoup.HtmlTags;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.Spinneret
{
    public class HtmlPrinter
    {
        private Stack<HtmlTag> _stack = new Stack<HtmlTag>();

        public HtmlPrinter(HtmlTag printInto)
        {
            _stack.Push(printInto);
        }

        public void AddTag(object value)
        {
            _stack.Peek().Add(value);
        }

        public void AddTag(object value1, object value2)
        {
            _stack.Peek().Add(value1);
            _stack.Peek().Add(value2);
        }

        public void AddTag(object value1, object value2, object value3, params object[] values)
        {
            _stack.Peek().Add(value1);
            _stack.Peek().Add(value2);
            _stack.Peek().Add(value3);
            foreach (var value in values)
                _stack.Peek().Add(value);
        }

        public void OpenTag(HtmlTag value)
        {
            _stack.Peek().Add(value);
            _stack.Push(value);
        }

        public void CloseTag()
        {
            _stack.Pop();
        }

        public HtmlTag GetHtml()
        {
            var result = _stack.Pop();
            if (_stack.Count != 0)
                throw new RTException("HtmlPrinter found {0} unclosed tags.".Fmt(_stack.Count));
            return result;
        }
    }
}
