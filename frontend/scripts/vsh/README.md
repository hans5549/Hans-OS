# @vben/vsh

一个 Shell 脚本工具集合，用于 Vue Vben Admin 專案的开发和管理。

## 功能特性

- 🚀 基于 Node.js 的现代化 Shell 工具
- 📦 支持模块化开发和按需載入
- 🔍 提供依賴檢查和分析功能
- 🔄 支持循环依賴扫描
- 📝 提供包发布檢查功能

## 安装

```bash
# 使用 pnpm 安装
pnpm add -D @vben/vsh

# 或者使用 npm
npm install -D @vben/vsh

# 或者使用 yarn
yarn add -D @vben/vsh
```

## 使用方法

### 全局安装

```bash
# 全局安装
pnpm add -g @vben/vsh

# 使用 vsh 命令
vsh [command]
```

### 本地使用

```bash
# 在 package.json 中添加脚本
{
  "scripts": {
    "vsh": "vsh"
  }
}

# 執行命令
pnpm vsh [command]
```

## 命令清單

- `vsh check-deps`: 檢查專案依賴
- `vsh scan-circular`: 扫描循环依賴
- `vsh publish-check`: 檢查包发布配置
