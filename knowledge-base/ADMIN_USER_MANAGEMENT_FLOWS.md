# Admin User Management — Feature Flows

Sequence diagrams for all 5 admin operations.

---

## 1. Create User Flow

```mermaid
sequenceDiagram
    actor Admin
    participant UI as AdminController
    participant UMS as UserManagementService
    participant EVS as EmailVerificationService
    participant Redis
    participant Email as EmailService
    participant User as New User

    Admin->>UI: POST /admin/users/create {name, email, password, role}
    UI->>UMS: CreateUserAsync(dto)
    UMS->>UI: FindByEmailAsync → null (unique)
    UMS->>UI: RoleExistsAsync → true
    UMS->>EVS: InitiateAdminVerificationAsync(email, name, role, plainPwd)
    EVS->>Redis: SET pending_admin_reg:{email} {name, bcryptHash, role, plainPwd} TTL=30min
    EVS->>Redis: SET otp:{email} {code, attempts=0} TTL=3min
    EVS->>Email: SendAdminCreatedVerifyAsync(email, name, code)
    Email-->>User: 📧 "Activate Your Account" email with OTP
    EVS-->>UMS: (true, null)
    UMS-->>UI: (true, null)
    UI-->>Admin: JSON { success: true }
```

---

## 2. Email Verification → Account Activation

```mermaid
sequenceDiagram
    actor User
    participant AC as AccountController
    participant EVS as EmailVerificationService
    participant Redis
    participant UM as UserManager
    participant Email as EmailService

    User->>AC: POST /verify-email {email, code}
    AC->>EVS: VerifyCodeAsync(email, code)
    EVS->>Redis: GET otp:{email} → compare code
    EVS-->>AC: (true, null)
    AC->>EVS: GetPendingAdminRegistrationAsync(email)
    EVS->>Redis: GET pending_admin_reg:{email}
    Redis-->>EVS: {name, bcryptHash, role, plainPwd}
    AC->>UM: CreateUserAsync(name, bcryptHash)
    AC->>UM: AddToRoleAsync(user, role)
    AC->>Email: SendWelcomeWithPasswordAsync(email, name, plainPwd)
    Email-->>User: 📧 Welcome + password email
    AC->>EVS: CleanupAsync(email)
    EVS->>Redis: DEL otp:{email}, pending_admin_reg:{email}
    AC-->>User: Redirect to Login
```

---

## 3. Update User Flow

```mermaid
sequenceDiagram
    actor Admin
    participant UI as AdminController
    participant UMS as UserManagementService
    participant UM as UserManager
    participant EVS as EmailVerificationService
    participant Email as EmailService
    actor User

    Admin->>UI: PUT /admin/users/{id} {name, email, role, updatedAt}
    UI->>UMS: UpdateUserAsync(dto)
    UMS->>UM: FindByIdAsync(userId)
    UM-->>UMS: user

    alt updatedAt mismatch (concurrency conflict)
        UMS-->>UI: (false, "modified by another administrator")
        UI-->>Admin: JSON { success: false, error: "..." }
    else updatedAt matches
        alt email changed
            UMS->>EVS: InitiateEmailUpdateVerificationAsync(newEmail, name, userId)
            EVS->>Email: SendEmailUpdateVerifyAsync(newEmail, name, code)
            Email-->>User: 📧 Verify New Email
        end
        UMS->>UM: UpdateAsync(user) — name + role + UpdatedAt
        UMS-->>UI: (true, null)
        UI-->>Admin: JSON { success: true }
    end
```

---

## 4. Disable / Reactivate User Flow

```mermaid
sequenceDiagram
    actor Admin
    participant UI as AdminController
    participant UMS as UserManagementService
    participant UM as UserManager
    participant Email as EmailService
    actor User

    Admin->>UI: POST /admin/users/{id}/disable {updatedAt}
    UI->>UMS: DisableUserAsync(userId, updatedAt)
    UMS->>UM: FindByIdAsync(userId)

    alt updatedAt mismatch
        UMS-->>Admin: (false, "concurrency error")
    else updatedAt matches
        UMS->>UM: user.IsActive = false, user.UpdatedAt = now
        UMS->>UM: UpdateAsync(user)
        UMS->>Email: SendAccountDisabledAsync(email, name, contactEmail)
        Email-->>User: 📧 Account Disabled notice
        UMS-->>UI: (true, null)
        UI-->>Admin: JSON { success: true }
    end

    Note over Admin,User: Reactivate mirrors this flow<br/>with IsActive = true, no email sent
```

---

## 5. Soft-Delete User Flow

```mermaid
sequenceDiagram
    actor Admin
    participant UI as AdminController
    participant UMS as UserManagementService
    participant UM as UserManager
    participant Email as EmailService
    actor User

    Admin->>UI: DELETE /admin/users/{id}
    UI->>UMS: SoftDeleteUserAsync(userId)
    UMS->>UM: FindByIdAsync(userId)

    alt already deleted
        UMS-->>Admin: (false, "already deleted")
    else
        UMS->>UM: user.DeletedAt = now, user.UpdatedAt = now
        UMS->>UM: UpdateAsync(user)
        UMS->>Email: SendAccountDeletedAsync(email, name, contactEmail)
        Email-->>User: 📧 Account Deletion notice
        UMS-->>UI: (true, null)
        UI-->>Admin: JSON { success: true }
        Note over UI: Row greyed out in table (DeletedAt != null)
    end
```

---

## Redis Key Schema

| Key | TTL | Contents | Flow |
|-----|-----|----------|------|
| `otp:{email}` | 3 min | `{code, verifyAttempts}` | All flows |
| `otp:send_count:{email}` | 1 hr | Rate limit counter (max 5) | All flows |
| `pending_reg:{email}` | 30 min | `{fullName, bcryptHash}` | Self-register |
| `pending_admin_reg:{email}` | 30 min | `{fullName, bcryptHash, role, plainPassword}` | Create user |
| `pending_email_update:{newEmail}` | 30 min | `{userId, fullName}` | Email update |

---

## Optimistic Concurrency Protection

All state-mutating operations (Update, Disable, Reactivate) require the client to send the current `UpdatedAt` value. Before saving, the service re-reads the entity and compares:

```
if (user.UpdatedAt != dto.UpdatedAt)
    return (false, "modified by another administrator. Please refresh.");
```

This prevents two admins from silently overwriting each other's changes.
