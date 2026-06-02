namespace Presentation.Defaults;

public static class AuthenticationSettings
{
    public const string LoginPath = @"/login";

    public const string LogoutPath = @"/logout";

    public const string RegistrationPath = @"/register";

    public const string AccessDeniedPath = @"/access-denied";

    public const string ReturnUrlParamName = @"return-url";

    public const string FallbackReturnUrl = @"/home/index";

    public const string GoogleLoginPath = @"/login/google";

    public const string GoogleCallbackPath = @"/login/oauth2/code/google";

    public const string GoogleCallbackAction = @"/login/google/callback";

    public const string VerifyEmailPath = @"/verify-email";

    public const string ResendCodePath = @"/verify-email/resend";
}
