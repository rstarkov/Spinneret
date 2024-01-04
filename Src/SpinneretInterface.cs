using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Forms;

namespace RT.Spinneret;

/// <summary>Implements a web interface to an application.</summary>
public class SpinneretInterface
{
    /// <summary>Gets the <see cref="HttpServer"/> instance used by the web interface.</summary>
    public HttpServer Server { get; private set; }

    /// <summary>Gets the <see cref="UrlResolver"/> instance used by the web interface.</summary>
    public UrlResolver Resolver { get; private set; }

    /// <summary>Gets/sets a default layout to be used for rendering pages. Individual pages can override this.</summary>
    public SpinneretLayout Layout { get; set; }

    /// <summary>Gets the collection of navigation links generated automatically by registering pages.</summary>
    public IList<NavLink> NavLinksPages { get { return _navLinksPages.AsReadOnly(); } }
    private List<NavLink> _navLinksPages = new List<NavLink>();

    /// <summary>Gets the collection of navigation links defined by the user.</summary>
    public virtual IEnumerable<NavLink> NavLinksUser { get { yield break; } }

    /// <summary>Creates and initialises a new web interface.</summary>
    public SpinneretInterface()
    {
    }

    /// <summary>
    ///     Attempts to start a server. If the server is already running, stops it first. Does not throw any exceptions if the
    ///     server cannot be started, but shows a warning to the user that the server could not be started. The outcome can be
    ///     determined via <see cref="ServerRunning"/>.</summary>
    public void StartServer(HttpServerOptions hsOptions, FileSystemOptions fsOptions = null)
    {
        if (ServerRunning)
            StopServer();

        try
        {
            Resolver = new UrlResolver();
            Server = new HttpServer(hsOptions) { Handler = Resolver.Handle };
            Server.StartListening();
            _navLinksPages.Clear();
            RegisterHandlers(fsOptions);
        }
        catch
        {
            try { Server.StopListening(true); }
            catch { }
            Server = null;
            DlgMessage.ShowWarning("The server could not be started. Try a different port (current port is {0}).".Fmt(hsOptions.Endpoints.JoinString()));
        }
    }

    /// <summary>If the server is running, stops it. Otherwise does nothing.</summary>
    public void StopServer()
    {
        if (Server == null)
            return;
        Server.StopListening(true);
        Server = null;
    }

    /// <summary>Gets server running status - true if the server is listening for requests, false otherwise.</summary>
    public bool ServerRunning
    {
        get { return Server != null && Server.IsListening; }
    }

    /// <summary>
    ///     Gets the server port. Provided for convenience - the port is also available via <see cref="Server.Options.Port"/>.</summary>
    public int ServerPort
    {
        get { return Server.Options.Endpoints["main"].Port; }
    }

    /// <summary>
    ///     Registers all http request handlers to be served by the web interface. Override to register own handlers (via <see
    ///     cref="Resolver.Add"/>) or pages (via <see cref="RegisterPage"/>), but remember to call the base method to register
    ///     some shared handlers.</summary>
    public virtual void RegisterHandlers(FileSystemOptions fsOptions)
    {
        if (fsOptions == null)
            throw new ArgumentException("FileSystemOptions can't be null");

        var fileHandler = new FileSystemHandler(PathUtil.AppPathCombine("Static"), fsOptions);
        Resolver.Add(new UrlMapping(fileHandler.Handle, path: "/Static"));
    }

    /// <summary>
    ///     Registers a <see cref="SpinneretPage"/> to be accessible through the web interface.</summary>
    /// <param name="baseUrl">
    ///     The URL on which the page will be accessible. If the path ends with "/", the subpaths will not be handled;
    ///     otherwise the path and all subpaths will use the page.</param>
    /// <param name="navLinkSection">
    ///     The section to use for the navigation link for this page.</param>
    /// <param name="navLinkText">
    ///     The text to use on the navigation link for this page.</param>
    /// <param name="pageMaker">
    ///     A function taking a request and returning a new page instance for that request.</param>
    public void RegisterPage(string baseUrl, string navLinkSection, string navLinkText, Func<HttpRequest, SpinneretPage> pageMaker)
    {
        Throw.IfArgumentNull(navLinkText, "navLinkText");
        Throw.IfArgumentNull(navLinkSection, "navLinkSection");
        RegisterPage(baseUrl, pageMaker);
        _navLinksPages.Add(new NavLink(navLinkSection, navLinkText, baseUrl));
    }

    /// <summary>
    ///     Registers a <see cref="SpinneretPage"/> to be accessible through the web interface.</summary>
    /// <param name="baseUrl">
    ///     The URL on which the page will be accessible. If the path ends with "/", the subpaths will not be handled;
    ///     otherwise the path and all subpaths will use the page.</param>
    /// <param name="pageMaker">
    ///     A function taking a request and returning a new page instance for that request.</param>
    public void RegisterPage(string baseUrl, Func<HttpRequest, SpinneretPage> pageMaker)
    {
        Resolver.Add(new UrlMapping(path: baseUrl, specificPath: baseUrl.EndsWith("/"),
            handler: request => handler_Page(request, pageMaker)));
    }

    /// <summary>
    ///     Default implementation only allows localhost to access all pages. Override to implement different logic, or
    ///     permanently return true to allow access unconditionally.</summary>
    public virtual bool ValidateAccessRights(HttpRequest request)
    {
        return System.Net.IPAddress.IsLoopback(request.ClientIPAddress);
    }

    private HttpResponse handler_Page(HttpRequest request, Func<HttpRequest, SpinneretPage> pageMaker)
    {
        if (!ValidateAccessRights(request))
            throw new HttpException(HttpStatusCode._403_Forbidden);

        var page = pageMaker(request);
        var response = page.ParseArgs();
        if (response != null) return response;

        try
        {
            return HttpResponse.Html(page.Layout.GetPageHtml(page));
        }
        catch (PageErrorException exc)
        {
            return HttpResponse.Html(page.Layout.GetErrorHtml(page, exc.Message), exc.Status);
        }
        catch (Exception e)
        {
            return HttpResponse.Html(page.Layout.GetExceptionHtml(page, e), HttpStatusCode._500_InternalServerError);
        }
    }
}

public static class Extensions
{
}
