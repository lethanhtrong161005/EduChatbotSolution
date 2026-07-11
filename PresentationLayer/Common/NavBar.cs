namespace Presentation.Common;

public static class NavBar
{
    public const string Home = "/";
    public const string Chat = "/chat";
    public const string Documents = "/documents";
    public const string Admin = "/admin";
    public const string RBL = "/rbl";
    public const string Plans = "/plans";
    public const string Profile = "/account/profile";

    public static string HomeNavClass(HttpContext httpContext) => PageNavClass(httpContext, Home);
    public static string ChatNavClass(HttpContext httpContext) => PageNavClass(httpContext, Chat);
    public static string DocumentsNavClass(HttpContext httpContext) => PageNavClass(httpContext, Documents);
    public static string AdminNavClass(HttpContext httpContext) => PageNavClass(httpContext, Admin);
    public static string RblNavClass(HttpContext httpContext) => PageNavClass(httpContext, RBL);
    public static string PlansNavClass(HttpContext httpContext) => PageNavClass(httpContext, Plans);
    public static string ProfileNavClass(HttpContext httpContext) => PageNavClass(httpContext, Profile);

    public static string PageNavClass(HttpContext httpContext, string startingSegment)
        => httpContext.Request.Path.StartsWithSegments(startingSegment) ? "active" : "";
}
