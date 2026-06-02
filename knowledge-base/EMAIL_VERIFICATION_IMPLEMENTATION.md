# Email Verification Feature — Implementation Guide

## Overview
Email ownership is verified before account creation. After filling out the registration form, the user receives a 6-digit OTP via Gmail and must enter it on a verification page before their account is created in the database.

---

## Flow Diagram

```
[Register Page] → POST /register
        │
        ├─ Email already exists? → show error on Register page
        │
        ├─ Rate limit hit (≥5 sends/hour)? → show error on Register page
        │
        ├─ Generate 6-digit OTP
        ├─ BCrypt-hash password
        ├─ Store in Redis:
        │     otp:{email}            TTL 3 min   { code, verifyAttempts }
        │     pending_reg:{email}    TTL 30 min  { fullName, bcryptHash }
        │     otp:send_count:{email} TTL 1 hour  (integer counter)
        ├─ Send email via Gmail SMTP
        └─ Redirect → GET /verify-email?email=...

[Verify Email Page]
        │
        ├─ User enters 6 digits
        │
        └─ POST /verify-email
                │
                ├─ OTP expired? → show "request new code" error
                ├─ Wrong code (< 5 attempts)? → show remaining attempts
                ├─ Wrong code (≥ 5 attempts)? → delete OTP key, show error
                ├─ Correct code:
                │     ├─ Read pending_reg:{email} from Redis
                │     ├─ CreateVerifiedAccountAsync (email, fullName, bcryptHash)
                │     ├─ Cleanup Redis keys
                │     └─ Redirect → /login with success message
                │
                └─ Resend Code (AJAX POST /verify-email/resend)
                        ├─ Rate limit check (same 5/hour counter)
                        ├─ New OTP generated + email sent
                        └─ Returns JSON { success, error, remainingSeconds }
```

---

## Redis Key Schema

| Key | Type | Value | TTL |
|-----|------|-------|-----|
| `otp:{email}` | String (JSON) | `{ "Code": "123456", "VerifyAttempts": 0 }` | 3 minutes |
| `otp:send_count:{email}` | String (int) | `"3"` | 1 hour (fixed from first send) |
| `pending_reg:{email}` | String (JSON) | `{ "FullName": "...", "BcryptHash": "$2a$..." }` | 30 minutes |

All keys are lowercase on the email segment.

---

## Rate Limiting Rules

| Limit | Key | Behaviour |
|-------|-----|-----------|
| 5 send/resend requests per email per hour | `otp:send_count:{email}` | Error shown; TTL displayed in minutes remaining |
| 5 wrong code entries per OTP | `verifyAttempts` field | OTP deleted after 5th wrong entry |

---

## New Files

| File | Purpose |
|------|---------|
| `Domain/Contracts/IEmailService.cs` | SMTP contract |
| `Domain/Contracts/IEmailVerificationService.cs` | OTP + pending-reg contract |
| `BusinessLayer/Services/EmailService.cs` | MailKit Gmail SMTP implementation |
| `BusinessLayer/Services/EmailVerificationService.cs` | Redis OTP + rate-limit logic |
| `PresentationLayer/Models/VerifyEmailVm.cs` | View model for verify page |
| `PresentationLayer/Views/Account/VerifyEmail.cshtml` | Verification UI |
| `PresentationLayer/wwwroot/css/account-verify-email.css` | OTP box, timer, resend styles |
| `PresentationLayer/wwwroot/js/account-verify-email.js` | Countdown timer, OTP UX, AJAX resend |

---

## Modified Files

| File | Change |
|------|--------|
| `Domain/Contracts/IAuthService.cs` | Added `EmailExistsAsync`, `CreateVerifiedAccountAsync` |
| `BusinessLayer/Services/AuthService.cs` | Implemented the two new methods |
| `BusinessLayer/Business.csproj` | Added `MailKit 4.17.0`, `StackExchange.Redis 2.13.17` |
| `PresentationLayer/Defaults/AuthenticationSettings.cs` | Added `VerifyEmailPath`, `ResendCodePath` |
| `PresentationLayer/Controllers/AccountController.cs` | Modified Register POST; added VerifyEmail GET/POST and ResendCode |
| `PresentationLayer/Program.cs` | Registered Redis singleton, EmailService, EmailVerificationService |
| `PresentationLayer/appsettings.json` | Added `Email:*` and `Redis:ConnectionString` sections |
| `docker-compose.yml` | Added `redis:7-alpine` service; web depends_on redis |
| `knowledge-base/send-verify-code-template.html` | Updated to project green (#2ecc71), `{{CODE}}` placeholder, 3-minute expiry |

---

## SMTP Configuration

| Setting | Value |
|---------|-------|
| Host | smtp.gmail.com |
| Port | 587 (STARTTLS) |
| Sender email | phuc220204edu@gmail.com |
| App password | stored in `Email:AppPassword` (appsettings.json) |

The template path is resolved relative to `AppContext.BaseDirectory` at runtime. In Docker the working directory must include the `knowledge-base/` folder (already present in the repo root mounted into the container).

---

## Redis Docker Setup

```bash
# Start Redis only (for local dev)
docker compose up redis -d

# Verify connectivity
docker exec educhatbot_redis redis-cli ping   # → PONG

# Inspect live keys during testing
docker exec educhatbot_redis redis-cli keys "otp:*"
docker exec educhatbot_redis redis-cli keys "pending_reg:*"
docker exec educhatbot_redis redis-cli TTL "otp:user@example.com"
```

Production docker-compose injects `Redis__ConnectionString=redis:6379` so the web container connects to the named Redis service inside the bridge network.

---

## Manual Test Checklist

1. Navigate to `/register` → fill form with a **new** email → submit
2. Confirm redirect to `/verify-email?email=...` with countdown starting at **3:00**
3. Check inbox — email should show the 6-digit code with green branding
4. Enter the correct code → confirm redirect to `/login` with success banner
5. **Wrong code**: enter wrong code 5× → confirm "too many attempts" message
6. **Expired code**: wait 3 minutes → enter any code → confirm "code expired" message
7. **Resend**: click Resend before countdown expires (should be disabled) → click after countdown → confirm new code in inbox and timer restarts
8. **Rate limit**: resend 5× within 1 hour → confirm blocked message with wait time
9. **Duplicate email**: register with an existing email → confirm error shown on Register page without sending any email
