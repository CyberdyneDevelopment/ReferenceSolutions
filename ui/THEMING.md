# Theming Guide - ManagementUI-Tailwind

This guide explains how to customize the appearance of the Blazor Tailwind Management UI.

## Theme Architecture

The UI uses **CSS custom properties** combined with Tailwind CSS v4:

```
Styles/input.css          → Defines CSS variables + component classes
    ↓ (tailwindcss.exe)
wwwroot/css/app.css       → Compiled CSS used by the app
    ↓
Blazor Components         → Use Tailwind utility classes
```

This architecture allows:
- Complete color customization
- No npm/node required
- Fast CSS compilation with standalone CLI
- Easy brand customization

---

## Default Theme: Cyberdyne

The default theme is a dark, industrial design inspired by sci-fi aesthetics:

```css
:root {
  --background: 224 71% 4%;      /* Deep navy/black */
  --foreground: 213 31% 91%;     /* Cold white */
  --primary: 0 84% 60%;          /* Terminator red */
  --accent: 199 89% 48%;         /* Electric cyan */
  --secondary: 217 19% 27%;      /* Gunmetal grey */
  --border: 217 19% 20%;         /* Subtle borders */
}
```

### Visual Characteristics

- **No rounded corners** - Sharp, brutalist edges
- **Monospace font** - Terminal/code aesthetic
- **Grid background** - Subtle pattern
- **Red glow effects** - Hover states on buttons
- **Animated indicators** - Pulsing status dots

---

## Quick Customization

### Change Primary Color

Edit `Styles/input.css` and rebuild:

```css
:root {
  /* Change from red to blue */
  --primary: 221 83% 53%;        /* Blue */
}
```

Then rebuild the CSS:
```bash
./tailwindcss.exe -i Styles/input.css -o wwwroot/css/app.css --minify
```

Or just run `dotnet build` (CSS is built automatically).

### Change to Your Brand Colors

```css
:root {
  /* Your brand purple */
  --primary: 270 50% 50%;

  /* Your brand teal for data elements */
  --accent: 180 60% 45%;

  /* Lighter background */
  --background: 220 30% 10%;
}
```

---

## CSS Variable Reference

### Core Colors

| Variable | Purpose | Default |
|----------|---------|---------|
| `--background` | Page background | Deep navy `224 71% 4%` |
| `--foreground` | Primary text | Cold white `213 31% 91%` |
| `--card` | Card backgrounds | Same as background |
| `--primary` | Buttons, accents | Red `0 84% 60%` |
| `--secondary` | Secondary elements | Gunmetal `217 19% 27%` |
| `--accent` | Data highlights | Cyan `199 89% 48%` |

### UI Chrome

| Variable | Purpose | Default |
|----------|---------|---------|
| `--border` | Borders | Dark gray `217 19% 20%` |
| `--input` | Input borders | Same as border |
| `--muted` | Disabled states | `217 19% 15%` |
| `--muted-foreground` | Placeholder text | `215 20% 65%` |

---

## Component Classes

The theme defines these CSS component classes in `Styles/input.css`:

### Buttons

```css
.btn-primary    /* Red filled button with glow */
.btn-cyber      /* Outlined with red hover glow */
.btn-outline    /* Gray outlined, cyan hover */
.btn-ghost      /* Transparent background */
.btn-sm         /* Small size modifier */
.btn-icon       /* Square icon button */
```

### Cards

```css
.card           /* Dark bordered container */
.card-header    /* Header with bottom border */
.card-title     /* Red uppercase title */
.card-content   /* Padded content area */
```

### Tables

```css
.table-container  /* Bordered wrapper */
.table            /* Full-width table */
/* th and td styles are automatic */
```

### Badges

```css
.badge-running  /* Blue with pulse */
.badge-success  /* Green */
.badge-failed   /* Red */
.badge-idle     /* Gray */
```

### Navigation

```css
.nav-item        /* Sidebar link */
.nav-item-active /* Active state (red) */
```

---

## Creating a Custom Theme

### Step 1: Modify Variables

Edit `Styles/input.css`:

```css
@layer base {
  :root {
    /* Corporate Blue Theme */
    --background: 222 47% 8%;
    --foreground: 210 40% 98%;
    --primary: 221 83% 53%;        /* Blue */
    --accent: 262 83% 58%;         /* Purple accent */
    --secondary: 217 33% 17%;
    --border: 217 33% 20%;
  }
}
```

### Step 2: Rebuild CSS

```bash
./tailwindcss.exe -i Styles/input.css -o wwwroot/css/app.css --minify
```

Or just rebuild the project:
```bash
dotnet build
```

### Step 3: Update Hardcoded Colors

Some colors are hardcoded in Razor components for SVG elements. Search for:
- `text-red-500` → Change to your primary
- `text-cyan-500` → Change to your accent
- `bg-red-500` → Change to your primary background

---

## Pre-built Theme Presets

### Cyberdyne (Default)

Dark theme with red accents, terminal-inspired.

```css
--primary: 0 84% 60%;              /* Red */
--background: 224 71% 4%;          /* Near black */
--accent: 199 89% 48%;             /* Cyan */
```

### Corporate Blue

Professional blue theme.

```css
--primary: 221 83% 53%;            /* Blue */
--background: 222 47% 8%;          /* Dark blue-gray */
--accent: 262 83% 58%;             /* Purple */
```

### Matrix Green

Hacker-inspired green theme.

```css
--primary: 142 71% 45%;            /* Green */
--background: 0 0% 4%;             /* Pure black */
--accent: 142 71% 45%;             /* Same green */
--foreground: 142 71% 45%;         /* Green text */
```

### Sunset Orange

Warm orange theme.

```css
--primary: 25 95% 53%;             /* Orange */
--background: 20 14% 8%;           /* Warm dark */
--accent: 38 92% 50%;              /* Gold */
```

---

## Adding Custom Fonts

### Step 1: Add Font Import

Edit `Components/App.razor`:

```html
<head>
    <link href="https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;700&display=swap" rel="stylesheet">
    ...
</head>
```

### Step 2: Update CSS

Edit `Styles/input.css`:

```css
body {
    font-family: 'JetBrains Mono', monospace;
}
```

---

## Logo Customization

Replace the logo in `Components/Layout/MainLayout.razor`:

```razor
<!-- Replace: -->
<svg class="w-6 h-6 text-red-500 mr-2">...</svg>
<span class="font-bold text-lg tracking-widest text-red-500">CYBERDYNE</span>

<!-- With: -->
<img src="logo.svg" alt="Company" class="h-8" />
```

Place your logo at `wwwroot/logo.svg`.

---

## Tailwind CSS Build

The project uses Tailwind CSS v4 standalone CLI:

```xml
<!-- ManagementUI-Tailwind.csproj -->
<Target Name="BuildTailwind" BeforeTargets="Build">
    <Exec Command="tailwindcss.exe -i Styles/input.css -o wwwroot/css/app.css --minify" />
</Target>
```

For development with hot reload:

```bash
./tailwindcss.exe -i Styles/input.css -o wwwroot/css/app.css --watch
```

---

## Best Practices

1. **Use HSL format** for color variables - easier to adjust
2. **Keep component classes** instead of inline Tailwind for consistency
3. **Maintain contrast ratios** for accessibility (WCAG 2.1)
4. **Test all pages** when changing themes
5. **Rebuild CSS** after any changes to input.css
