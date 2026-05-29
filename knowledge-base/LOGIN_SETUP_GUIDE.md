# EduChatAI - Login Feature Setup Guide

## 📋 Overview

This guide explains the complete login feature implementation for EduChatAI including session-based authentication, UI/UX design with Bootstrap 5, and responsive design.

---

## 🚀 Implementation Summary

### Architecture Components

#### 1. **Data Access Layer (DataAccessLayer)**
- **Entities:**
  - `User.cs` - User account model with email, password hash, roles
  - `Role.cs` - System roles (Admin, Lecturer, Student)
  - `UserRole.cs` - Many-to-many relationship between users and roles

- **Repository:**
  - `IUserRepository.cs` - Interface for user data operations
  - `UserRepository.cs` - Implementation with async CRUD operations

- **DbContext:**
  - `ApplicationDbContext.cs` - Configured with pgvector and user tables

#### 2. **Business Layer (BusinessLayer)**
- **DTOs:**
  - `LoginRequestDto.cs` - Email, password, remember-me flag
  - `AuthResponseDto.cs` - Authentication result with user info and roles
  - `RegisterRequestDto.cs` - Registration form data

- **Services:**
  - `IAuthService.cs` - Authentication interface
  - `AuthService.cs` - Login logic, password verification/hashing with BCrypt
  - `DatabaseInitializationService.cs` - Database seeding and migration

#### 3. **Presentation Layer (PresentationLayer)**
- **Controllers:**
  - `AccountController.cs` - Login/logout endpoints with session management

- **Views:**
  - `Views/Account/Login.cshtml` - Bootstrap 5 responsive login UI with animations

- **Styles:**
  - `wwwroot/css/colors.css` - Global color scheme and design system

---

## 🔐 Test Credentials

Three default users are automatically created on first run:

### Admin User
- **Email:** `admin@educhatai.com`
- **Password:** `Admin@123456`
- **Role:** Admin (Full system access)

### Lecturer User
- **Email:** `lecturer@educhatai.com`
- **Password:** `Lecturer@123456`
- **Role:** Lecturer

### Student User
- **Email:** `student@educhatai.com`
- **Password:** `Student@123456`
- **Role:** Student

---

## 📦 Required NuGet Packages

Ensure the following packages are installed:

```
DataAccessLayer:
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- pgvector

BusinessLayer:
- BCrypt.Net-Next (v4.0.3+)

PresentationLayer:
- Microsoft.AspNetCore.Session
```

---

## 🗄️ Database Setup

### PostgreSQL Connection String
Located in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=educhatbot_db;Username=edu_admin;Password=EduChatbotPass2026!;Include Error Detail=true"
  }
}
```

### Database Initialization
- On first application run, the `DatabaseInitializationService` automatically:
  1. Applies EF Core migrations
  2. Creates `users`, `roles`, `user_roles` tables
  3. Seeds default roles (Admin, Lecturer, Student)
  4. Creates test user accounts

---

## 🎨 UI/UX Design Features

### Login Page Template Features
- **Left Sidebar (Desktop):** Brand identity with feature highlights
- **Right Form Panel:** Clean login form with input validation
- **Responsive Design:** Works seamlessly on mobile, tablet, and desktop
- **Bootstrap 5:** Professional, modern styling framework
- **Loading Animation:** Spinning loader during form submission
- **Floating Bubbles:** Animated background effects
- **Form Validation:** Client-side and server-side validation
- **Error Handling:** Clear error messages for failed login attempts

### Color Scheme (`colors.css`)
Global design system with CSS custom properties:

```css
:root {
    --color-primary: #2ecc71;           /* Green - Main brand */
    --color-primary-dark: #27ae60;      /* Dark green - Hover */
    --color-secondary: #3498db;         /* Blue - Accent */
    --color-error: #e74c3c;             /* Red - Errors */
    --font-family-base: System fonts    /* Optimized typography */
    /* ... and more */
}
```

---

## 🔄 Session Management

### Session Configuration (Program.cs)
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // 30 min timeout
    options.Cookie.HttpOnly = true;                  // Prevent JS access
    options.Cookie.IsEssential = true;               // Required cookie
    options.Cookie.Name = "EduChatAI.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
});
```

### Session Keys
- `UserId` - User GUID stored in session
- `FullName` - User's full name
- `UserRoles` - Comma-separated role names
- `RememberMe` - Optional remember-me flag

---

## 🔐 Password Security

### BCrypt Implementation
- Algorithm: BCrypt (industry-standard)
- Work Factor: 12 rounds
- Automatic salt generation
- Secure password comparison to prevent timing attacks

### Password Hashing Example
```csharp
// Hashing
string hash = _authService.HashPassword("myPassword");

// Verification
bool isValid = _authService.VerifyPassword("myPassword", hash);
```

---

## 📱 Responsive Breakpoints

The login page is optimized for:

- **Mobile (< 576px):** Single column layout, full-width form
- **Tablet (576px - 991px):** Stacked layout, sidebar below
- **Desktop (≥ 992px):** Side-by-side layout with full sidebar

---

## 🚦 Navigation Flow

### Application Startup
1. Application starts
2. User is redirected to `/Account/Login` (default route)
3. User enters email and password
4. On successful authentication:
   - Session is created
   - User is redirected to `/Home/Index`
5. On failed authentication:
   - Error message is displayed
   - Form remains on login page

### Logout
- User clicks logout
- Session is cleared
- User is redirected to `/Account/Login`

---

## 📝 Code Structure & Standards

### AI Coding Standard Compliance
All code follows the `AI_CODING_STANDARD.md`:

- ✅ XML documentation on all public members
- ✅ Modern C# 12/13 syntax with nullable types
- ✅ File-scoped namespaces
- ✅ Default string initialization (`= string.Empty`)
- ✅ Async/await for all I/O operations
- ✅ Input validation with ArgumentNullException
- ✅ Clear, business-focused comments

### Example Class Structure
```csharp
/// <summary>
/// Handles user authentication operations.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        // Implementation with validation and error handling
    }
}
```

---

## 🧪 Testing the Login Feature

### Step 1: Build & Run
```bash
dotnet build
dotnet run
```

### Step 2: Navigate to Application
```
https://localhost:7000/  (or your configured port)
```

### Step 3: Login with Test Credentials
- Use any of the test user accounts above
- Try "Remember Me" checkbox
- Test with invalid credentials to see error handling

### Step 4: Verify Session
- Check browser DevTools → Application → Cookies
- Should see `EduChatAI.Session` cookie
- Session expires after 30 minutes of inactivity

---

## 🎯 Key Features Implemented

✅ **Session-Based Authentication**
- Secure session creation on login
- 30-minute timeout
- HTTPOnly cookies

✅ **Password Security**
- BCrypt hashing (12 rounds)
- No plaintext passwords stored
- Secure comparison

✅ **Responsive UI**
- Bootstrap 5 framework
- Mobile-first design
- Tablet and desktop optimized

✅ **Loading Animations**
- Spinner animation on submit
- Smooth transitions
- Floating bubble effects

✅ **Global Color Scheme**
- CSS custom properties
- Consistent across application
- Dark mode ready

✅ **Error Handling**
- Clear error messages
- User-friendly feedback
- Server-side validation

✅ **Database Seeding**
- Automatic on first run
- Test users created
- Roles initialized

---

## 🔗 Related Files

| File | Purpose |
|------|---------|
| `DataAccessLayer/Entities/User.cs` | User model |
| `BusinessLayer/Services/AuthService.cs` | Authentication logic |
| `PresentationLayer/Controllers/AccountController.cs` | Login endpoints |
| `PresentationLayer/Views/Account/Login.cshtml` | UI/UX template |
| `PresentationLayer/wwwroot/css/colors.css` | Global color scheme |
| `PresentationLayer/Program.cs` | DI & middleware config |

---

## 🚀 Next Steps

After login is working:

1. **Add Registration Feature** - Implement sign-up page
2. **Email Verification** - Send verification emails
3. **Password Reset** - Implement forgot password flow
4. **Two-Factor Authentication** - Add 2FA for security
5. **OAuth Integration** - Add social login (Google, Facebook)
6. **Protected Routes** - Add authorization middleware for roles
7. **User Dashboard** - Create home page for authenticated users

---

## 📧 Support

For issues or questions about the login implementation, refer to:
- `AI_CODING_STANDARD.md` - Code standards
- `README.md` - Project architecture
- `database-script.sql` - Database schema
