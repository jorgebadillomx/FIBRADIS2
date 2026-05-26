---
name: ui-ux-pro-max
description: UI/UX design intelligence with searchable database.
---

Use this skill when the task involves UI structure, visual design decisions, interaction patterns, accessibility, responsive behavior, or design-system work.

## When to Apply

Apply this skill for:
- New pages, dashboards, landing pages, or complex components
- Style, color, typography, spacing, or layout decisions
- UX, accessibility, or visual-consistency reviews
- Navigation, animation, chart, or responsive-design work

Skip this skill for:
- Pure backend or database work
- Infra/DevOps tasks with no UI impact

## FIBRADIS Usage

Before changing UI in this repo:
1. Read `design-system/MASTER.md`
2. If a page-specific file exists in `design-system/pages/`, prioritize it over `MASTER.md`
3. Use this search tool when you need style, UX, typography, color, chart, or stack guidance

## Commands

Check Python:

```powershell
python --version
```

Generate a design system:

```powershell
python .codex/skills/ui-ux-pro-max/scripts/search.py "<query>" --design-system --stack react
```

Examples:

```powershell
python .codex/skills/ui-ux-pro-max/scripts/search.py "fibras fintech editorial premium" --design-system
python .codex/skills/ui-ux-pro-max/scripts/search.py "dashboard accessibility loading" --domain ux
python .codex/skills/ui-ux-pro-max/scripts/search.py "comparison trend" --domain chart
python .codex/skills/ui-ux-pro-max/scripts/search.py "react query dashboard performance" --stack react
```

Persist the generated design system:

```powershell
python .codex/skills/ui-ux-pro-max/scripts/search.py "<query>" --design-system --persist -p "Project Name"
```

## Search Reference

Domains:
- `product`
- `style`
- `typography`
- `color`
- `landing`
- `chart`
- `ux`
- `react`
- `web`
- `prompt`

Stacks:
- `react`
- `nextjs`
- `vue`
- `svelte`
- `astro`
- `swiftui`
- `react-native`
- `flutter`
- `nuxtjs`
- `nuxt-ui`
- `html-tailwind`
- `shadcn`
- `jetpack-compose`
- `threejs`

Use `--design-system` first, then supplement with `--domain` or `--stack` searches as needed.
