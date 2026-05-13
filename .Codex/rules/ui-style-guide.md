# UI Style Guide - Hans-OS

Use this for UI and styling changes in `frontend/apps/web-antd`.

## Design System

Hans-OS uses Vue Vben Admin with Ant Design Vue, Tailwind CSS, CSS custom properties, and runtime theme preferences.

Prefer existing Vben patterns over custom foundational UI.

## Colors

- Use CSS variables for colors and surfaces.
- New color values should use HSL when adding tokens.
- Do not hard-code one-off hex colors in components unless the surrounding code already requires it and the reason is explicit.
- Support light and dark mode.

Core semantic tokens:

| Purpose | Token |
|---------|-------|
| Primary | `--primary` |
| Danger / error | `--destructive` |
| Success | `--success` |
| Warning | `--warning` |
| Background | `--background`, `--background-deep` |
| Text | `--foreground` |
| Card / elevated | `--card`, `--popover` |
| Border | `--border` |

Ant Design token mapping is handled by Vben design token utilities. Do not bypass it for common component theming.

## Layout

Default Vben dimensions:

| Element | Value |
|---------|-------|
| Sidebar width | 224px |
| Sidebar collapsed width | 60px |
| Mixed nav width | 80px |
| Header height | 50px |
| Tabbar height | 38px |
| Footer height | 32px |
| Content compact width | 1200px |

Design for:

- collapsed sidebar
- compact mode
- dark mode
- mobile/narrow widths where the page is expected to work

## Components

- Use Ant Design Vue for common controls: form, table, modal, drawer, select, date picker, upload, tabs.
- Use Vben shared components when the project already provides them.
- Do not create a new base component for one screen unless it removes real duplication or complexity.
- Reusable components should have typed props and emits.

## Tailwind

- Prefer Tailwind utilities for spacing, flex/grid, typography, and responsive layout.
- Keep spacing scale consistent.
- Avoid arbitrary values unless a fixed product constraint requires them.
- Use scoped CSS for complex component-specific styling.

## Icons

- Prefer Iconify strings such as `mdi:home` or `lucide:settings`.
- Use existing `Icon` patterns from Vben.
- Do not add custom SVGs when an existing Iconify icon is sufficient.

## Typography

- Match display size to context. Dense admin panels should use compact headings, not landing-page scale.
- Avoid negative letter spacing.
- Do not scale font size with viewport width.
- Ensure button/card text fits in mobile and desktop layouts.

## Interaction

- Use existing loading, empty, error, and disabled states.
- Preserve keyboard and focus behavior provided by Ant Design Vue.
- Avoid excessive custom animation. Prefer existing transition utilities.

## Visual QA

For non-trivial UI changes:

1. Run `cd frontend && pnpm check:type`.
2. Start the frontend if visual verification is needed: `cd frontend && pnpm dev:antd`.
3. Inspect desktop and narrow viewport behavior.
4. Verify dark mode if colors or surfaces changed.
