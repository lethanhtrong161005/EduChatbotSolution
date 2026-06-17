namespace Presentation.Common;

public static class NavBar
{
    public const string Home = "/";
    public const string Chat = "/chat";
    public const string Documents = "/documents";
    public const string Accounts = "/acc";
    public const string RBL = "/rbl";
    public const string Plans = "/plans";

    public static string HomeNavClass(HttpContext httpContext) => PageNavClass(httpContext, Home);
    public static string ChatNavClass(HttpContext httpContext) => PageNavClass(httpContext, Chat);
    public static string DocumentsNavClass(HttpContext httpContext) => PageNavClass(httpContext, Documents);
    public static string AccountsNavClass(HttpContext httpContext) => PageNavClass(httpContext, Accounts);
    public static string RblNavClass(HttpContext httpContext) => PageNavClass(httpContext, RBL);
    public static string PlansNavClass(HttpContext httpContext) => PageNavClass(httpContext, Plans);

    public static string PageNavClass(HttpContext httpContext, string startingSegment)
        => httpContext.Request.Path.StartsWithSegments(startingSegment) ? "active" : "";
}
