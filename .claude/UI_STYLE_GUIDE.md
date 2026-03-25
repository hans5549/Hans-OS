# Hans-OS UI Style Guide

> Vue 3 + Ant Design Vue + Vben Admin 設計系統參考文件
> 適用於 `frontend/apps/web-antd`

---

## 一、色彩系統

### 色彩模型

所有顏色使用 **HSL 格式**，以 CSS Custom Properties 定義，支援執行時期動態切換。

### 語意色彩

| 用途 | CSS Variable | HSL 值（Light） | 說明 |
|------|-------------|-----------------|------|
| 主色 | `--primary` | `212 100% 45%` | 藍色，品牌識別色 |
| 危險/錯誤 | `--destructive` | `359.33 100% 65.1%` | 紅色，刪除/錯誤 |
| 成功 | `--success` | `144 57% 58%` | 綠色，成功狀態 |
| 警告 | `--warning` | `42 84% 61%` | 黃橘色，注意事項 |

### 基底色彩

| 用途 | CSS Variable | Light | Dark |
|------|-------------|-------|------|
| 背景 | `--background` | `0 0% 100%` | `222.34deg 10.43% 12.27%` |
| 深層背景 | `--background-deep` | `216 20.11% 95.47%` | `220deg 13.06% 9%` |
| 前景文字 | `--foreground` | `210 6% 21%` | `0 0% 95%` |
| 卡片 | `--card` | `0 0% 100%` | `222.34deg 10.43% 12.27%` |
| 彈出層 | `--popover` | `0 0% 100%` | 同 card |
| 邊框 | `--border` | `240 5.9% 90%` | 深灰 |
| 遮罩 | `--overlay` | `0 0% 0% / 45%` | 帶透明度黑色 |

### 色階生成

系統自動為語意色彩生成 8 個色階：`-50`、`-100`、`-200`、`-300`、`-400`、`-500`、`-600`、`-700`。

### 14 個內建主題

```
default (藍)   | violet (紫)    | pink (粉)     | yellow (黃)
sky-blue (天藍) | green (綠)    | zinc (鋅灰)   | deep-green (深綠)
deep-blue (深藍)| orange (橙)   | rose (玫瑰)   | neutral (中性)
slate (石板灰)  | gray (灰)     | custom (自訂)
```

使用者可在偏好設定中即時切換主題，或自訂色彩。

---

## 二、字型系統

### 字型堆疊

```css
--font-family: -apple-system, blinkmacsystemfont, 'Segoe UI', roboto,
               'Helvetica Neue', arial, 'Noto Sans', sans-serif,
               'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol',
               'Noto Color Emoji';
```

### 字型大小

- **基底字級**: `--font-size-base: 16px`（可設定 12px–20px）
- **選單字級**: `calc(var(--font-size-base) * 0.875)` = 14px

---

## 三、間距與圓角

### 圓角系統

| Token | 計算 | 預設值 |
|-------|------|--------|
| `--radius` | 基底值 | `0.5rem`（8px） |
| `--radius-sm` | `--radius - 4px` | 4px |
| `--radius-md` | `--radius - 2px` | 6px |
| `--radius-lg` | `--radius` | 8px |
| `--radius-xl` | `--radius + 4px` | 12px |

### 版面尺寸

| 元素 | 預設值 |
|------|--------|
| 側邊欄寬度 | 224px |
| 側邊欄收合寬度 | 60px |
| 混合導覽寬度 | 80px |
| 頂部導覽列高度 | 50px |
| 分頁列高度 | 38px |
| 頁尾高度 | 32px |
| 內容緊湊寬度 | 1200px |

---

## 四、陰影

```css
--shadow-float: 0 6px 16px 0 rgb(0 0 0 / 8%),
                0 3px 6px -4px rgb(0 0 0 / 12%),
                0 9px 28px 8px rgb(0 0 0 / 5%);
```

---

## 五、動畫與轉場

### 頁面轉場

預設使用 `fade-slide`，可設定的動畫名稱：

| 名稱 | 說明 | 時長 |
|------|------|------|
| `fade-slide` | 淡入滑動（預設） | — |
| `slide-up/down/left/right` | 方向滑動 | 0.25s |
| `fade-transition` | 純淡入淡出 | 0.2s |
| `fade-scale` | 淡入縮放 | 0.28s |
| `fade-up/down` | 淡入上/下滑 | 0.25s |

### 元件動畫

- `accordion-down/up`: 手風琴展開/收合（0.2s）
- `collapsible-down/up`: 可折疊區塊（0.2s）
- `float`: 浮動動畫（5s 循環）

---

## 六、Z-Index 管理

```css
--popup-z-index: 2000;
```

Ant Design 的 `zIndexPopupBase` 設為 `2000`，避免下拉選單被遮蓋。

---

## 七、Tailwind CSS 整合

### 設定方式

- CSS Custom Properties 映射為 Tailwind 顏色工具類
- 暗色模式使用 `.dark` class（非 `prefers-color-scheme`）
- 啟用 `@tailwindcss/typography` 與 `@iconify/tailwind4` 外掛

### 自訂工具類

```css
@utility flex-center     /* display:flex; align-items:center; justify-content:center */
@utility flex-col-center /* 同上但 flex-direction:column */
```

### 元件類別

| 類別 | 用途 |
|------|------|
| `.outline-box` | 帶框線的互動區塊 |
| `.vben-link` | 帶 hover/active 狀態的連結 |
| `.card-box` | 帶邊框與背景的卡片 |
| `.enter-x` / `.enter-y` | 交錯入場動畫 |

---

## 八、Ant Design Token 映射

CSS Variables 與 Ant Design Token 的對應（由 `use-design-tokens.ts` 處理）：

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
| `borderRadius` | `--radius` × 16 |

---

## 九、圖標系統

### 圖標來源

- **Iconify**: 使用字串格式 `"mdi:home"`、`"lucide:settings"`
- **Vue 元件**: 直接傳入元件 `<Icon :icon="SomeIcon" />`
- **函式**: 渲染函式 `() => h(...)`
- **遠端 URL**: HTTP 圖片連結

### 使用方式

```vue
<Icon icon="mdi:home" />
<Icon :icon="SomeVueComponent" />
<Icon icon="https://example.com/icon.svg" />
```

### 註冊自訂圖標

```typescript
import { createIconifyIcon } from '@vben/icons';
export const MdiKeyboardEsc = createIconifyIcon('mdi:keyboard-esc');
```

---

## 十、版面配置

### 支援的版面模式

| 模式 | 說明 |
|------|------|
| `sidebar-nav` | 側邊欄導覽（預設） |
| `mixed-nav` | 混合式導覽 |
| `top-nav` | 頂部導覽 |

### 偏好設定

```typescript
sidebar: { collapsed: false, expandOnHover: true, draggable: true }
header: { enable: true, mode: 'fixed', menuAlign: 'start' }
tabbar: { enable: true, styleType: 'chrome' }
transition: { enable: true, name: 'fade-slide', progress: true }
```

### 無障礙功能

- `colorGrayMode`: 灰階濾鏡模式
- `colorWeakMode`: 色弱友善模式
- `compact`: 緊湊版面模式

---

## 十一、設計檔案結構

```
frontend/packages/@core/base/design/src/
├── design-tokens/
│   ├── default.css      # Light 模式色彩與 token
│   ├── dark.css         # Dark 模式色彩與主題變體
│   └── index.ts
├── css/
│   ├── global.css       # Tailwind 設定、動畫、工具類
│   ├── ui.css           # 元件樣式
│   ├── transition.css   # 頁面轉場動畫
│   └── nprogress.css    # 載入進度條
└── index.ts
```

---

## 十二、開發規範摘要

1. **使用 CSS Variables** — 所有顏色、間距透過 `var(--xxx)` 引用，勿硬編碼
2. **HSL 格式** — 新增色彩一律使用 HSL
3. **Ant Design Vue 元件優先** — 使用框架元件，避免自行實作基礎 UI
4. **Tailwind 輔助** — 間距、排版用 Tailwind 工具類
5. **暗色模式** — 所有新元件必須支援 light/dark 兩種模式
6. **Iconify 圖標** — 優先使用 Iconify 格式 `"prefix:name"`
7. **回應式設計** — 考慮側邊欄收合、緊湊模式等情境
8. **動畫適度** — 使用既有轉場類別，避免自訂過多動畫
