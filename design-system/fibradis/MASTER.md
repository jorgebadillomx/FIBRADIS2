# Design System Master File

> **LOGIC:** When building a specific page, first check `design-system/pages/[page-name].md`.
> If that file exists, its rules **override** this Master file.
> If not, strictly follow the rules below.

---

**Project:** FIBRADIS
**Generated:** 2026-05-25 19:06:33 (palette adjusted manually for inmobiliario premium neutro)
**Category:** Real Estate Financial Platform

---

## Global Rules

### Color Palette

| Role | Hex | CSS Variable |
|------|-----|--------------|
| Primary | `#1A4A3A` | `--color-primary` |
| On Primary | `#FFFFFF` | `--color-on-primary` |
| Gold/CTA | `#9A7B2E` | `--color-gold` |
| Background | `#FFFFFF` | `--color-background` |
| Card | `#F8FAFC` | `--color-card` |
| Foreground | `#1E293B` | `--color-foreground` |
| Muted | `#F1F5F9` | `--color-muted` |
| Muted FG | `#64748B` | `--color-muted-foreground` |
| Border | `#E2E8F0` | `--color-border` |
| Destructive | `#DC2626` | `--color-destructive` |
| Ring | `#1A4A3A` | `--color-ring` |

**Color Notes:** Verde bosque sobre fondo blanco limpio + gris frío para cards — premium, legible, moderno

### Typography

- **Heading Font:** Playfair Display (display serif — editorial, premium, real estate)
- **Body Font:** IBM Plex Sans (humanist sans — claro, tabular, financiero)
- **Mood:** premium, editorial, trustworthy, professional, real estate
- **Google Fonts:** [Playfair Display + IBM Plex Sans](https://fonts.google.com/share?selection.family=Playfair+Display:wght@400;600;700|IBM+Plex+Sans:wght@300;400;500;600)

**CSS Import:**
```css
@import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;600;700&family=IBM+Plex+Sans:wght@300;400;500;600&display=swap');
```

### Spacing Variables

| Token | Value | Usage |
|-------|-------|-------|
| `--space-xs` | `4px` / `0.25rem` | Tight gaps |
| `--space-sm` | `8px` / `0.5rem` | Icon gaps, inline spacing |
| `--space-md` | `16px` / `1rem` | Standard padding |
| `--space-lg` | `24px` / `1.5rem` | Section padding |
| `--space-xl` | `32px` / `2rem` | Large gaps |
| `--space-2xl` | `48px` / `3rem` | Section margins |
| `--space-3xl` | `64px` / `4rem` | Hero padding |

### Shadow Depths

| Level | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.05)` | Subtle lift |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.1)` | Cards, buttons |
| `--shadow-lg` | `0 10px 15px rgba(0,0,0,0.1)` | Modals, dropdowns |
| `--shadow-xl` | `0 20px 25px rgba(0,0,0,0.15)` | Hero images, featured cards |

---

## Component Specs

### Buttons

```css
/* Primary Button */
.btn-primary {
  background: #1A4A3A;
  color: white;
  padding: 12px 24px;
  border-radius: 6px;
  font-weight: 600;
  font-family: 'IBM Plex Sans', sans-serif;
  letter-spacing: 0.01em;
  transition: all 200ms ease;
  cursor: pointer;
}

.btn-primary:hover {
  background: #2D6A4F;
  transform: translateY(-1px);
}

/* Secondary Button */
.btn-secondary {
  background: transparent;
  color: #1A4A3A;
  border: 1.5px solid #1A4A3A;
  padding: 12px 24px;
  border-radius: 6px;
  font-weight: 500;
  transition: all 200ms ease;
  cursor: pointer;
}

/* Accent/CTA Button */
.btn-accent {
  background: #9A7B2E;
  color: white;
  padding: 12px 24px;
  border-radius: 6px;
  font-weight: 600;
  transition: all 200ms ease;
  cursor: pointer;
}
```

### Cards

```css
.card {
  background: #FFFFFF;
  border: 1px solid #D8CFC4;
  border-radius: 8px;
  padding: 24px;
  box-shadow: var(--shadow-sm);
  transition: all 200ms ease;
}

.card:hover {
  box-shadow: var(--shadow-md);
  border-color: #B8A99A;
}
```

### Inputs

```css
.input {
  padding: 12px 16px;
  border: 1px solid #D8CFC4;
  border-radius: 6px;
  font-size: 16px;
  background: #FFFFFF;
  color: #2C2416;
  transition: border-color 200ms ease;
}

.input:focus {
  border-color: #1A4A3A;
  outline: none;
  box-shadow: 0 0 0 3px #1A4A3A20;
}
```

### Modals

```css
.modal-overlay {
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
}

.modal {
  background: white;
  border-radius: 16px;
  padding: 32px;
  box-shadow: var(--shadow-xl);
  max-width: 500px;
  width: 90%;
}
```

---

## Style Guidelines

**Style:** Data-Dense Dashboard

**Keywords:** Multiple charts/widgets, data tables, KPI cards, minimal padding, grid layout, space-efficient, maximum data visibility

**Best For:** Business intelligence dashboards, financial analytics, enterprise reporting, operational dashboards, data warehousing

**Key Effects:** Hover tooltips, chart zoom on click, row highlighting on hover, smooth filter animations, data loading spinners

### Page Pattern

**Pattern Name:** Real-Time / Operations Landing

- **Conversion Strategy:** For ops/security/iot products. Demo or sandbox link. Trust signals.
- **CTA Placement:** Primary CTA in nav + After metrics
- **Section Order:** 1. Hero (product + live preview or status), 2. Key metrics/indicators, 3. How it works, 4. CTA (Start trial / Contact)

---

## Anti-Patterns (Do NOT Use)

- ❌ Light mode default
- ❌ Slow rendering

### Additional Forbidden Patterns

- ❌ **Emojis as icons** — Use SVG icons (Heroicons, Lucide, Simple Icons)
- ❌ **Missing cursor:pointer** — All clickable elements must have cursor:pointer
- ❌ **Layout-shifting hovers** — Avoid scale transforms that shift layout
- ❌ **Low contrast text** — Maintain 4.5:1 minimum contrast ratio
- ❌ **Instant state changes** — Always use transitions (150-300ms)
- ❌ **Invisible focus states** — Focus states must be visible for a11y

---

## Pre-Delivery Checklist

Before delivering any UI code, verify:

- [ ] No emojis used as icons (use SVG instead)
- [ ] All icons from consistent icon set (Heroicons/Lucide)
- [ ] `cursor-pointer` on all clickable elements
- [ ] Hover states with smooth transitions (150-300ms)
- [ ] Light mode: text contrast 4.5:1 minimum
- [ ] Focus states visible for keyboard navigation
- [ ] `prefers-reduced-motion` respected
- [ ] Responsive: 375px, 768px, 1024px, 1440px
- [ ] No content hidden behind fixed navbars
- [ ] No horizontal scroll on mobile
