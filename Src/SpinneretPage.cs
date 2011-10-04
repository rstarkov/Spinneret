using System.Collections.Generic;
using System.Linq;
using RT.Servers;
using RT.Util;

namespace RT.Spinneret
{
    /// <summary>
    /// The base class used for web page implementation in Spinneret.
    /// </summary>
    public abstract class SpinneretPage
    {
        /// <summary>
        /// Stores the request that this page is the response to.
        /// </summary>
        public readonly HttpRequest Request;

        /// <summary>
        /// Stores the web interface instance by which this request was created.
        /// </summary>
        protected readonly SpinneretInterface Interface;

        /// <summary>
        /// True if the page is being rendered full-screen.
        /// </summary>
        public bool FullScreen { get; set; }

        /// <summary>
        /// Lists the names of all url arguments which are to be automatically turned into cookies.
        /// Should be initialised by descendants if they wish to make use of this feature.
        /// </summary>
        protected string[] CookiableArgs;

        /// <summary>
        /// Holds a collection of navigation links specific to this page. These are usually merged
        /// with all the other navlinks when a page is rendered. Derived classes may add items or
        /// create a new collection if necessary.
        /// </summary>
        protected List<NavLink> NavLinksWritable = new List<NavLink>();

        /// <summary>
        /// Gets a read-only collection of navigation links specific to this page.
        /// </summary>
        public IList<NavLink> NavLinks { get { return NavLinksWritable.AsReadOnly(); } }

        /// <summary>
        /// Constructs a page, storing a reference to the request that has caused it.
        /// </summary>
        protected SpinneretPage(HttpRequest request, SpinneretInterface @interface)
        {
            Request = request;
            Interface = @interface;
        }

        /// <summary>
        /// Invoked before the title/body functions to give the page an opportunity to parse & validate
        /// the arguments received via the URL querystring or the cookies. Make sure to call the base
        /// when overriding this!
        /// </summary>
        /// <returns>
        /// "null" to indicate successful parsing. An <see cref="HttpResponse"/> to indicate an error -
        /// in which case the returned HttpResponse will be served to the user instead of the page.
        /// </returns>
        public virtual HttpResponse ParseArgs()
        {
            if (CookiableArgs != null)
            {
                List<Cookie> cookies = null;
                foreach (var arg in CookiableArgs)
                {
                    if (Request.Get.ContainsKey(arg))
                    {
                        if (cookies == null) cookies = new List<Cookie>();
                        var argval = Request.Get[arg].Value;
                        var mustBe = ValidateCookiable(arg, argval);
                        if (mustBe != null) throw new ValidationException(arg, argval, mustBe);
                        cookies.Add(new Cookie() { Name = arg, Value = argval, Path = "/" });
                    }
                }
                if (cookies != null)
                    return HttpResponse.Empty(HttpStatusCode._302_Found, new HttpResponseHeaders()
                    {
                        SetCookie = cookies,
                        Location = Request.SameUrlWhere(key => !CookiableArgs.Contains(key))
                    });
            }

            FullScreen = Request.GetValidated("FullScreen", false);
            return null;
        }

        /// <summary>
        /// Override to validate the value provided for a cookiable argument of the specified name. Must return null
        /// if validated successfully, or a message to be displayed to the user. If not overridden, will always
        /// validate any value as acceptable.
        /// </summary>
        /// <remarks>
        /// Grammatically, the returned message should fit in place of "[...]" in the sentence "The value must be [...]".
        /// This method is called to validate both the arguments from the URL and actual cookie values received from the client,
        /// however in the case of actual cookies the cookie is cleared instead of displaying an error to the user.
        /// </remarks>
        protected virtual string ValidateCookiable(string name, string value)
        {
            return null;
        }

        /// <summary>
        /// Gets the layout to be used by this page. Default implementation returns the layout specified in
        /// the web interface. Override to supply a custom layout for a page.
        /// </summary>
        public virtual SpinneretLayout Layout { get { return Interface.Layout; } }

        /// <summary>
        /// When overridden, should return the title of this page.
        /// </summary>
        public abstract string GetTitle();

        /// <summary>
        /// When overridden, should return the content of this page. Anything compatible with TagSoup is accepted
        /// as the return type, incl. strings, IEnumerables, Tags.
        /// </summary>
        public abstract object GetContent();
    }

    /// <summary>
    /// This exception can be thrown by page generation code to indicate an error that is caused by
    /// and can be rectified by the user. For any technical errors a different exception should be thrown.
    /// </summary>
    public class PageErrorException : RTException
    {
        public HttpStatusCode Status { get; private set; }

        public PageErrorException(string message, HttpStatusCode status)
            : base(message)
        {
            Status = status;
        }
    }
}
