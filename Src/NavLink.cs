using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Spinneret
{
    public class NavLink
    {
        private string _section;
        private string _text;
        private string _href;
        public NavLink(string section, string text, string href)
        {
            _section = section;
            _text = text;
            _href = href;
        }
        public string Section { get { return _section; } }
        public string Text { get { return _text; } }
        public string Href { get { return _href; } }
    }
}
