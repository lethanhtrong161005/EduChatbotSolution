# EduChatAI - Global Color Scheme & Design System Guide

## 📄 Overview

This guide explains how to use the global color scheme defined in `colors.css` throughout the EduChatAI web application.

The color scheme is built using **CSS Custom Properties (CSS Variables)** for consistent branding, easy maintenance, and light/dark mode support.

---

## 🎨 Color Palette

### Primary Colors (Brand Identity)
```css
--color-primary: #2ecc71;           /* Green - Main brand color */
--color-primary-dark: #27ae60;      /* Dark green - Hover states */
--color-primary-light: #58d68d;     /* Light green - Backgrounds */
```

### Secondary Colors (Accents)
```css
--color-secondary: #3498db;         /* Blue - Accent color */
--color-secondary-dark: #2980b9;    /* Dark blue - Interactions */
--color-secondary-light: #5dade2;   /* Light blue - Backgrounds */
```

### Semantic Colors
```css
--color-success: #2ecc71;           /* Success - Green */
--color-warning: #f39c12;           /* Warning - Orange */
--color-error: #e74c3c;             /* Error - Red */
--color-info: #3498db;              /* Info - Blue */
```

### Neutral Colors (Grayscale)
```css
--color-neutral-50: #f9fafb;        /* Almost white */
--color-neutral-100: #f3f4f6;       /* Very light gray */
--color-neutral-200: #e5e7eb;       /* Light gray */
--color-neutral-300: #d1d5db;       /* Gray */
--color-neutral-400: #9ca3af;       /* Medium gray */
--color-neutral-500: #6b7280;       /* Darker gray */
--color-neutral-600: #4b5563;       /* Dark gray */
--color-neutral-700: #374151;       /* Very dark gray */
--color-neutral-800: #1f2937;       /* Almost black */
--color-neutral-900: #111827;       /* Black */
```

---

## 🎯 Usage Examples

### In HTML/CSS

#### Using Color Variables
```html
<!-- Button with primary color -->
<button class="btn btn-primary">Click Me</button>

<!-- Text with secondary color -->
<p class="text-secondary">This is secondary text</p>

<!-- Background with neutral color -->
<div class="bg-light">Light background content</div>
```

#### Direct CSS Variables
```css
.custom-element {
    background-color: var(--color-primary);
    color: var(--color-text-light);
    border: 2px solid var(--color-border-light);
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}
```

### In Razor Views (ASP.NET Core)

```html
@{
    ViewData["Title"] = "Dashboard";
}

<div class="container">
    <div class="card">
        <div class="card-header bg-primary">
            <h1 class="text-light">Welcome</h1>
        </div>
        <div class="card-body">
            <p class="text-muted">Your content here</p>
            <button class="btn btn-primary">Action</button>
        </div>
    </div>
</div>

<!-- Success Alert -->
<div class="alert alert-success">
    <i class="fas fa-check-circle"></i>
    Operation completed successfully!
</div>

<!-- Error Alert -->
<div class="alert alert-error">
    <i class="fas fa-exclamation-circle"></i>
    An error occurred!
</div>
```

---

## 📐 Typography

### Font Family
```css
--font-family-base: System fonts optimized for readability
--font-family-mono: Monaco, Menlo, Ubuntu Mono (for code)
```

### Font Sizes
```css
--font-size-xs: 0.75rem;     /* 12px - Small labels */
--font-size-sm: 0.875rem;    /* 14px - Small text */
--font-size-base: 1rem;      /* 16px - Body text */
--font-size-lg: 1.125rem;    /* 18px - Larger text */
--font-size-xl: 1.25rem;     /* 20px - Section titles */
--font-size-2xl: 1.5rem;     /* 24px - Small headings */
--font-size-3xl: 1.875rem;   /* 30px - Medium headings */
--font-size-4xl: 2.25rem;    /* 36px - Large headings */
```

### Font Weights
```css
--font-weight-light: 300;       /* Light text */
--font-weight-normal: 400;      /* Regular text */
--font-weight-medium: 500;      /* Medium emphasis */
--font-weight-semibold: 600;    /* Strong emphasis */
--font-weight-bold: 700;        /* Bold text */
```

### Usage Example
```html
<h1 style="font-size: var(--font-size-4xl); font-weight: var(--font-weight-bold);">
    Main Title
</h1>

<p style="font-size: var(--font-size-base); color: var(--color-text-secondary);">
    Secondary paragraph text
</p>

<small style="font-size: var(--font-size-sm); font-weight: var(--font-weight-light);">
    Small helper text
</small>
```

---

## 📏 Spacing System

### Spacing Scale
```css
--spacing-1: 0.25rem;   /* 4px */
--spacing-2: 0.5rem;    /* 8px */
--spacing-3: 0.75rem;   /* 12px */
--spacing-4: 1rem;      /* 16px - Base unit */
--spacing-5: 1.25rem;   /* 20px */
--spacing-6: 1.5rem;    /* 24px */
--spacing-8: 2rem;      /* 32px */
--spacing-10: 2.5rem;   /* 40px */
--spacing-12: 3rem;     /* 48px */
--spacing-16: 4rem;     /* 64px */
```

### Usage Example
```css
.card {
    padding: var(--spacing-6);
    margin-bottom: var(--spacing-4);
    border-radius: var(--radius-lg);
}

.button {
    padding: var(--spacing-3) var(--spacing-4);
    margin-right: var(--spacing-2);
}
```

---

## 🔄 Border Radius

### Radius Scale
```css
--radius-sm: 0.375rem;      /* 6px - Subtle */
--radius-md: 0.5rem;        /* 8px - Standard */
--radius-lg: 0.75rem;       /* 12px - Medium */
--radius-xl: 1rem;          /* 16px - Large */
--radius-2xl: 1.5rem;       /* 24px - Extra large */
--radius-full: 9999px;      /* Fully rounded */
```

### Usage Example
```css
.button {
    border-radius: var(--radius-md);
}

.card {
    border-radius: var(--radius-lg);
}

.avatar {
    border-radius: var(--radius-full);
}
```

---

## 💫 Shadows & Effects

### Shadow Levels
```css
--shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
--shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
--shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
--shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
```

### Usage Example
```css
.card {
    box-shadow: var(--shadow-md);
}

.button:hover {
    box-shadow: var(--shadow-lg);
}

.modal {
    box-shadow: var(--shadow-xl);
}
```

---

## ⏱️ Transitions

### Transition Durations
```css
--transition-fast: 150ms ease-in-out;
--transition-base: 200ms ease-in-out;
--transition-slow: 300ms ease-in-out;
```

### Usage Example
```css
.button {
    background-color: var(--color-primary);
    transition: all var(--transition-base);
}

.button:hover {
    background-color: var(--color-primary-dark);
}

.modal-fade {
    transition: opacity var(--transition-slow);
}
```

---

## 📱 Responsive Breakpoints

### Breakpoint Variables
```css
--breakpoint-xs: 320px;     /* Extra small devices */
--breakpoint-sm: 576px;     /* Small devices (phones) */
--breakpoint-md: 768px;     /* Medium devices (tablets) */
--breakpoint-lg: 992px;     /* Large devices (desktops) */
--breakpoint-xl: 1200px;    /* Extra large devices */
--breakpoint-2xl: 1400px;   /* Ultra-wide devices */
```

### Media Query Usage
```css
/* Mobile first approach */
.container {
    width: 100%;
    padding: var(--spacing-4);
}

/* Tablet */
@media (min-width: 768px) {
    .container {
        width: 750px;
    }
}

/* Desktop */
@media (min-width: 992px) {
    .container {
        width: 960px;
    }
}

/* Using variables */
@media (min-width: var(--breakpoint-lg)) {
    .sidebar {
        display: flex;
    }
}
```

---

## 🌓 Dark Mode Support

### Light Mode (Default)
```css
:root {
    --color-bg-light: #ffffff;
    --color-text-primary: #111827;
}
```

### Dark Mode (Auto-detection)
```css
@media (prefers-color-scheme: dark) {
    :root {
        --color-bg-light: #1f2937;
        --color-text-primary: #ffffff;
    }
}
```

### Enabling Dark Mode
```html
<!-- Add to your main layout or stylesheet -->
<link rel="stylesheet" href="~/css/colors.css">
```

The system automatically detects user's OS preference and applies dark mode colors if available.

---

## 🛠️ Pre-defined Utility Classes

### Button Classes
```html
<button class="btn-primary">Primary Button</button>
<button class="btn-secondary">Secondary Button</button>
```

### Text Classes
```html
<p class="text-primary">Primary text</p>
<p class="text-secondary">Secondary text</p>
<p class="text-muted">Muted text</p>
```

### Background Classes
```html
<div class="bg-primary">Primary background</div>
<div class="bg-secondary">Secondary background</div>
<div class="bg-light">Light background</div>
<div class="bg-dark">Dark background</div>
```

### Alert Classes
```html
<div class="alert-success">Success message</div>
<div class="alert-warning">Warning message</div>
<div class="alert-error">Error message</div>
<div class="alert-info">Info message</div>
```

---

## 📝 Creating New Components

### Example: Custom Card Component
```css
.custom-card {
    background-color: var(--color-bg-light);
    border: 1px solid var(--color-border-light);
    border-radius: var(--radius-lg);
    padding: var(--spacing-6);
    box-shadow: var(--shadow-md);
    transition: all var(--transition-base);
}

.custom-card:hover {
    box-shadow: var(--shadow-lg);
    border-color: var(--color-primary);
}

.custom-card-title {
    font-size: var(--font-size-2xl);
    font-weight: var(--font-weight-bold);
    color: var(--color-text-primary);
    margin-bottom: var(--spacing-4);
}

.custom-card-content {
    color: var(--color-text-secondary);
    font-size: var(--font-size-base);
    line-height: 1.6;
}
```

---

## ✅ Best Practices

1. **Always use variables** - Never hardcode colors
   ```css
   /* ✅ Good */
   color: var(--color-primary);
   
   /* ❌ Bad */
   color: #2ecc71;
   ```

2. **Use semantic colors** - Choose based on meaning
   ```css
   /* ✅ Good */
   border-color: var(--color-error);
   
   /* ❌ Bad */
   border-color: var(--color-primary);
   ```

3. **Maintain consistency** - Use the same spacing throughout
   ```css
   /* ✅ Good */
   margin: var(--spacing-4);
   padding: var(--spacing-4);
   
   /* ❌ Bad */
   margin: 16px;
   padding: 1rem;
   ```

4. **Follow mobile-first approach** - Start with mobile, enhance for desktop
   ```css
   /* Mobile first */
   .container {
       width: 100%;
   }
   
   /* Then enhance for desktop */
   @media (min-width: var(--breakpoint-lg)) {
       .container {
           width: 960px;
       }
   }
   ```

5. **Test in dark mode** - Ensure all components work in both modes

---

## 📚 Related Resources

- `colors.css` - Main color scheme file
- `LOGIN_SETUP_GUIDE.md` - Login feature documentation
- `AI_CODING_STANDARD.md` - Code style standards
- `README.md` - Project architecture overview

---

## 🚀 Extending the Color System

### Adding New Colors
Edit `colors.css`:
```css
:root {
    /* ... existing colors ... */
    --color-custom: #your-color;
    --color-custom-dark: #your-darker-color;
}
```

### Creating New Component Styles
```css
.your-component {
    background-color: var(--color-primary);
    color: var(--color-text-light);
    padding: var(--spacing-4);
    border-radius: var(--radius-md);
    transition: all var(--transition-base);
}
```

---

## 💡 Tips & Tricks

- Use browser DevTools to inspect CSS variables
- Adjust opacity with `rgba()` for transparency: `rgba(46, 204, 113, 0.1)`
- Create utility classes for common combinations
- Test color contrast for accessibility (WCAG compliance)
- Use CSS custom properties for theme switching without page reload

---

**Last Updated:** May 29, 2026  
**Version:** 1.0
