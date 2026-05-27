namespace PresentationLayer.Defaults;

public static class AuthenticationDefaults
{
    public const string LoginPath = "/login";

    public const string LogoutPath = "/logout";

    public const string RegistrationPath = "/register";

    public const string AccessDeniedPath = "/access-denied";

    public const string ReturnUrlParamName = "return-url";

    public const string FallbackReturnUrl = "/";
}
