# @vben/turbo-run

`turbo-run` 是一个命令行工具，允许你在多个包中并行執行命令。它提供了一个交互式的界面，让你可以选择要執行命令的包。

## 特性

- 🚀 交互式选择要執行的包
- 📦 支持 monorepo 專案结构
- 🔍 自动检测可用的命令
- 🎯 精确过滤目标包

## 安装

```bash
pnpm add -D @vben/turbo-run
```

## 使用方法

基本语法：

```bash
turbo-run [script]
```

例如，如果你想執行 `dev` 命令：

```bash
turbo-run dev
```

工具会自动检测哪些包有 `dev` 命令，并提供一个交互式界面让你选择要執行的包。

## 示例

假设你的專案中有以下包：

- `@vben/app`
- `@vben/admin`
- `@vben/website`

当你執行：

```bash
turbo-run dev
```

工具会：

1. 检测哪些包有 `dev` 命令
2. 顯示一个交互式选择界面
3. 让你选择要執行命令的包
4. 使用 `pnpm --filter` 在选定的包中執行命令

## 注意事项

- 確保你的專案使用 pnpm 作为包管理器
- 確保目标包在 `package.json` 中定义了相应的脚本命令
- 该工具需要在 monorepo 專案的根目录下執行
