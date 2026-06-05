# Admin User Management - Feature Flows

Sequence diagrams for admin user-management operations.

---

## 1. Create User Flow

Admin-created accounts are created as active accounts immediately. The system sets
`EmailConfirmed = true` and emails the initial credentials to the user. This flow
does not create OTP codes and does not use Redis.

```mermaid
sequenceDiagram
    actor Admin
    participant UI as AdminController
    participant UMS as UserManagementService
    participant UM as UserManager
    participant RM as RoleManager
    participant Email as EmailService
    participant User as New User

    Admin->>UI: POST /admin/users/create {name, email, password, role}
    UI->>UMS: CreateUserAsync(dto)
    UMS->>UM: FindByEmailAsync(email)
    UM-->>UMS: null (unique)
    UMS->>UM: FindByNameAsync(email)
    UM-->>UMS: null (unique)
    UMS->>RM: RoleExistsAsync(role)
    RM-->>UMS: true
    UMS->>UM: CreateAsync(user with EmailConfirmed=true, IsActive=true)
    UMS->>UM: AddToRoleAsync(user, role)
    UMS->>UM: AddClaimsAsync(user, id/email/name/role)
    UMS->>Email: SendAdminCreatedCredentialsAsync(email, name, plainPassword)
    Email-->>User: Email with login email + password
    UMS-->>UI: (true, null)
    UI-->>Admin: JSON { success: true }

    alt credentials email fails
        UMS->>UM: DeleteAsync(user)
        UMS-->>UI: (false, "Failed to send account credentials email")
        UI-->>Admin: JSON { success: false, error: "..." }
    end
```

---

## 2. Update User Flow

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
            UMS->>UM: Set EmailConfirmed=false for new email
            UMS->>EVS: InitiateEmailUpdateVerificationAsync(newEmail, name, userId)
            EVS->>Email: SendEmailUpdateVerifyAsync(newEmail, name, code)
            Email-->>User: Verify New Email
        end
        UMS->>UM: UpdateAsync(user) - name + role + UpdatedAt
        UMS-->>UI: (true, null)
        UI-->>Admin: JSON { success: true }
    end
```

---

## 3. Disable / Reactivate User Flow

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
        Email-->>User: Account Disabled notice
        UMS-->>UI: (true, null)
        UI-->>Admin: JSON { success: true }
    end

    Note over Admin,User: Reactivate mirrors this flow with IsActive = true, no email sent
```

---

## 4. Soft-Delete User Flow

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
        Email-->>User: Account Deletion notice
        UMS-->>UI: (true, null)
        UI-->>Admin: JSON { success: true }
        Note over UI: Row greyed out in table (DeletedAt != null)
    end
```

---

## Redis Key Schema

Create-user no longer uses Redis. Redis remains only for self-registration OTP and
email-update verification.

| Key | TTL | Contents | Flow |
|-----|-----|----------|------|
| `otp:{email}` | 3 min | `{code, verifyAttempts}` | Self-register, email update |
| `otp:send_count:{email}` | 1 hr | Rate limit counter (max 5) | OTP sending |
| `pending_reg:{email}` | 30 min | `{fullName, bcryptHash}` | Self-register |
| `pending_email_update:{newEmail}` | 30 min | `{userId, fullName}` | Email update |

---

## Optimistic Concurrency Protection

All state-mutating operations except Create and Soft Delete require the client to
send the current `UpdatedAt` value. Before saving, the service re-reads the entity
and compares:

```csharp
if (user.UpdatedAt != dto.UpdatedAt)
    return (false, "modified by another administrator. Please refresh.");
```

This prevents two admins from silently overwriting each other's changes.
