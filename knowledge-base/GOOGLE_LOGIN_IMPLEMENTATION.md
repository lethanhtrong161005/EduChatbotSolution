# Google OAuth Login — Design & Implementation Guide

## 1. Overview

This document describes the complete design and implementation of Google OAuth 2.0 login/register
for the **EduChatAI** platform. The feature integrates Google's identity provider with ASP.NET
Core Identity so users can authenticate using their Google account instead of (or in addition to)
a local email/password.

---

## 2. Technology Stack

| Component | Library / Version |
|---|---|
| OAuth middleware | `Microsoft.AspNetCore.Authentication.Google` 10.0.8 |
| Identity store | ASP.NET Core Identity + `IdentityDbContext` |
| Database | PostgreSQL via Npgsql + EF Core |
| External login table | `user_logins` (`IdentityUserLogin<Guid>`) — already in schema |

---

## 3. Architecture Diagram

```
Browser
  │
  ├─ GET /login/google  ─────────────────────────────────────────────────────────►  AccountController.GoogleLogin()
  │                                                                                         │
  │                                                                              ConfigureExternalAuthenticationProperties()
  │                                                                                         │
  │◄──────────────────────────────── 302 Redirect to accounts.google.com ─────────────────┘
  │
  ├─ [User consents on Google]
  │
  ├─ GET /login/oauth2/code/google  ──►  Google OAuth Middleware  (CallbackPath)
  │                                              │
  │                                   Validates token, sets ExternalLoginInfo
  │                                              │
  │                              302 Redirect to /login/google/callback
  │
  ├─ GET /login/google/callback  ────────────────────────────────────────────────►  AccountController.GoogleCallback()
  │                                                                                         │
  │                                                                         GetExternalLoginInfoAsync()
  │                                                                                         │
  │                                                                         AuthService.HandleGoogleLoginAsync()
  │                                                                                         │
  │                                                                         SignInWithClaimsAsync()
  │                                                                                         │
  │◄──────────────────────────────── 302 Redirect to returnUrl ─────────────────────────────
```

---

## 4. Google Cloud Console Configuration

- **Project**: `educhatai-498113`
- **Client ID**: `202561402091-b5cmu6k66143g8ms1kfmth8e1u50plo9.apps.googleusercontent.com`
- **Authorized redirect URIs** (must match `CallbackPath` in `Program.cs`):
  - `http://localhost:5158/login/oauth2/code/google`

> **Important**: If the app port changes, add the new URI in Google Cloud Console →
> APIs & Services → Credentials → OAuth 2.0 Client IDs.

---

## 5. Files Modified / Created

| File | Change |
|---|---|
| `PresentationLayer/Presentation.csproj` | Added `Microsoft.AspNetCore.Authentication.Google` 10.0.8 |
| `PresentationLayer/appsettings.json` | Added `Authentication:Google:ClientId/ClientSecret` |
| `PresentationLayer/Program.cs` | Registered `AddAuthentication().AddGoogle(...)` |
| `PresentationLayer/Defaults/AuthenticationSettings.cs` | Added `GoogleLoginPath`, `GoogleCallbackPath`, `GoogleCallbackAction` |
| `PresentationLayer/Controllers/AccountController.cs` | Added `GoogleLogin` and `GoogleCallback` actions |
| `PresentationLayer/Views/Account/Login.cshtml` | Added active Google sign-in button |
| `PresentationLayer/Views/Account/Register.cshtml` | Enabled Google sign-up button |
| `PresentationLayer/wwwroot/css/account-auth.css` | Added `.btn-social--google` styles |
| `Domain/Contracts/IAuthService.cs` | Added `HandleGoogleLoginAsync(ExternalLoginInfo)` |
| `BusinessLayer/Services/AuthService.cs` | Implemented `HandleGoogleLoginAsync` + `BuildLoginResultAsync` helper |

---

## 6. Database — No Migration Required

The `user_logins` table (`IdentityUserLogin<Guid>`) already exists in the schema from the
`InitialCreate` migration. It stores external OAuth provider entries:

| Column | Description |
|---|---|
| `login_provider` | `"Google"` |
| `provider_key` | Google's unique user ID (sub claim) |
| `provider_display_name` | `"Google"` |
| `user_id` | FK → `users.id` |

---

## 7. Business Logic — Three Login Scenarios

### Scenario A: User has logged in with Google before
1. `_userManager.FindByLoginAsync("Google", providerKey)` → returns existing user.
2. Load claims → sign in.

### Scenario B: User registered with email/password, now logs in with Google (same email)
1. `FindByLoginAsync` returns `null` (no Google link yet).
2. `FindByEmailAsync(email)` returns the existing local account.
3. **Link** Google to the existing account via `_userManager.AddLoginAsync(user, info)`.
4. If `EmailConfirmed == false`, set it to `true` and update (Google guarantees verified emails).
5. Load claims → sign in.
> The user now has both a password and a Google login linked to the same account.

### Scenario C: Brand-new user — no local account at all
1. Both `FindByLoginAsync` and `FindByEmailAsync` return `null`.
2. Create a new `ApplicationUser`:
   - `EmailConfirmed = true` (Google guarantees this)
   - `FullName` from Google's `ClaimTypes.Name`, or falls back to email prefix
   - No password set (`PasswordHash` is `null`)
3. `_userManager.CreateAsync(newUser)` → no password argument (external-only account).
4. `AddLoginAsync` → add Google entry to `user_logins`.
5. `AddToRoleAsync("Student")` + `AddClaimsAsync(...)` → same claims as email registration.
6. Sign in.

---

## 8. Email Confirmation Bypass

The project has `opts.SignIn.RequireConfirmedEmail = true` in Identity options. Google OAuth users
bypass the email confirmation step because:
- Google only issues tokens for verified Gmail accounts.
- `EmailConfirmed = true` is set explicitly in `HandleGoogleLoginAsync` for all three scenarios.

---

## 9. Security Notes

- Client credentials are stored in `appsettings.json`. For production, move them to
  **environment variables** or **Azure Key Vault**:
  ```
  Authentication__Google__ClientId=...
  Authentication__Google__ClientSecret=...
  ```
- `CallbackPath` is fixed to `/login/oauth2/code/google` to match the Google Console registration.
- `SameSite=Lax` on the application cookie is compatible with OAuth redirects.
- All OAuth state validation (CSRF on the OAuth flow) is handled automatically by the middleware.

---

## 10. Routing Summary

| Route | Method | Action | Description |
|---|---|---|---|
| `/login/google` | GET | `GoogleLogin` | Redirect browser to Google |
| `/login/oauth2/code/google` | GET | *(middleware)* | Receive Google callback |
| `/login/google/callback` | GET | `GoogleCallback` | Process result, sign in |
