using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Spinneret
{
    public class NavLink
    {
        public string Section;
        public string Text;
        public string Href;

        public NavLink() { }

        public NavLink(string section, string text, string href)
        {
            Section = section;
            Text = text;
            Href = href;
        }
    }
}
