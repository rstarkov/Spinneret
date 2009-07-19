using System;
using System.Collections.Generic;
using System.Linq;
using RT.TagSoup;
using RT.TagSoup.HtmlTags;

namespace RT.Spinneret
{
    public abstract class SnowWhiteLayout : SpinneretLayout
    {
        protected readonly SpinneretInterface Interface;

        public SnowWhiteLayout(SpinneretInterface iface)
        {
            Interface = iface;
        }

        private Tag makePage(SpinneretPage page, string title, object content)
        {
            var head = new HEAD(
                new TITLE(FormatHtmlTitle(title)),
                new LINK() { rel = "stylesheet", type = "text/css", href = GetCssLink() }
            );

            var body = new BODY() { class_ = page.FullScreen ? "sw-full-screen" : null }._(
                new TABLE() { class_ = "sw-layout" }._(new TR() { class_ = "sw-layout" }._(
                    new TD() { class_ = "sw-layout-leftpane" }._(
                        new P(new A(GetHomeLinkBody()) { href = "/" }) { class_ = "sw-topleft-title" },
                        GetNavPanelTop(page),
                        GetNavLinks(page),
                        GetNavPanelBottom(page)
                    ),
                    new TD() { class_ = "sw-layout-mainpane" }._(
                        new DIV() { class_ = "sw-fullscreen-link" }._(new A("Full screen") { href = page.Request.SameUrlExceptSet("FullScreen", "true") }),
                        new H1(title),
                        content
                    )
                ))
            );

            return new HTML(head, body);
        }

        public override Tag GetPageHtml(SpinneretPage page)
        {
            return makePage(page, page.GetTitle(), page.GetContent());
        }

        public override Tag GetErrorHtml(SpinneretPage page, string message)
        {
            return makePage(page, "Error", new object[]
            {
                new H1("Error") { class_ = "sw-error" },
                new DIV(message) { class_ = "sw-error" }
            });
        }

        public override Tag GetExceptionHtml(SpinneretPage page, Exception exception)
        {
            List<object> result = new List<object>();
            while (exception != null)
            {
                result.Add(new H3(exception.GetType().FullName));
                result.Add(new P(exception.Message));
                result.Add(new UL() { class_ = "sw-exception" }._(exception.StackTrace.Split('\n').Select(x => (object) new LI(x))));
                exception = exception.InnerException;
            }
            return makePage(page, "Exception", result);
        }

        protected abstract object GetHomeLinkBody();

        protected virtual string GetCssLink()
        {
            return "/Static/SnowWhite.css";
        }

        protected virtual string FormatHtmlTitle(string title)
        {
            return title;
        }

        protected virtual object GetNavPanelTop(SpinneretPage page) { return null; }

        protected virtual object GetNavPanelBottom(SpinneretPage page) { return null; }

        protected virtual object GetNavLinks(SpinneretPage page)
        {
            return getNavLinks(page);
        }

        private IEnumerable<object> getNavLinks(SpinneretPage page)
        {
            IList<NavLink> temp;
            List<NavLink> links = new List<NavLink>();
            links.AddRange(Interface.NavLinksPages);
            temp = Interface.NavLinksUser;
            if (temp != null) links.AddRange(temp);
            temp = page.NavLinks;
            if (temp != null) links.AddRange(temp);

            bool[] done = new bool[links.Count];

            while (true)
            {
                bool anyUndone = false;
                string curSection = null;
                UL list = null;

                for (int i = 0; i < links.Count; i++)
                {
                    if (curSection == null && !done[i])
                    {
                        curSection = links[i].Section;
                        yield return new H2(curSection);
                        list = new UL();
                    }

                    if (curSection != null)
                    {
                        if (links[i].Section != curSection)
                            anyUndone = true;
                        else
                            list.Add(new LI(new A(links[i].Text) { href = links[i].Href }));
                    }
                }

                yield return list;
                if (!anyUndone) break;
            }
        }
    }
}
