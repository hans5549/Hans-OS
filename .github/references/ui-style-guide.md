---
description: 'UI Style Guide — Vue 3 + Ant Design Vue + Vben Admin Design System'
---

# Hans-OS UI Style Guide

> Vue 3 + Ant Design Vue + Vben Admin Design System

## Color System

### Color Model

All colors use **HSL format**, defined via CSS Custom Properties, supporting runtime dynamic switching.

### Semantic Colors

| Purpose | CSS Variable | Description |
|---------|-------------|-------------|
| Primary | `--primary` | Blue, brand identity color |
| Danger/Error | `--destructive` | Red, delete/error |
| Success | `--success` | Green, success state |
| Warning | `--warning` | Yellow-orange, caution |

### Ant Design Token Mapping

| Ant Design Token | CSS Variable |
|------------------|-------------|
| `colorPrimary` | `--primary` |
| `colorError` | `--destructive` |
| `colorWarning` | `--warning` |
| `colorSuccess` | `--success` |
| `colorTextBase` | `--foreground` |
| `colorBorder` | `--border` |
| `colorBgElevated` | `--popover` |
| `colorBgContainer` | `--card` |
| `colorBgBase` | `--background` |
| `colorBgLayout` | `--background-deep` |
| `colorBgMask` | `--overlay` |

## Layout Dimensions

| Element | Default Value |
|---------|---------------|
| Sidebar width | 224px |
| Sidebar collapsed width | 60px |
| Top navbar height | 50px |
| Tab bar height | 38px |
| Footer height | 32px |

## Tailwind CSS Integration

- CSS Custom Properties are mapped to Tailwind color utility classes
- Dark mode uses `.dark` class (not `prefers-color-scheme`)

### Custom Utility Classes

```css
@utility flex-center     /* display:flex; align-items:center; justify-content:center */
@utility flex-col-center /* same as above but flex-direction:column */
```

## Icon System

- **Iconify** preferred: `"mdi:home"`, `"lucide:settings"`
- Vue components: pass directly via `<Icon :icon="SomeIcon" />`
- Register custom icons: `createIconifyIcon('mdi:keyboard-esc')`

## Development Guidelines

1. **Use CSS Variables** — reference all colors and spacing via `var(--xxx)`, never hard-code
2. **HSL Format** — always use HSL when adding new colors
3. **Ant Design Vue Components First** — use framework components, avoid implementing basic UI from scratch
4. **Tailwind Utilities** — use Tailwind utility classes for spacing and layout
5. **Dark Mode** — all new components must support both light/dark modes
6. **Iconify Icons** — prefer Iconify format
7. **Responsive Design** — consider sidebar collapse, compact mode, and similar scenarios
8. **Moderate Animations** — use existing transition classes, avoid excessive custom animations
