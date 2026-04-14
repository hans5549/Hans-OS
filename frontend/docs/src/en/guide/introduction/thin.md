# Slimmed-Down Version

Starting from version `5.0`, we no longer provide slimmed-down repositories or branches. Our goal is to offer a more consistent development experience while reducing maintenance costs. Here’s how we introduce our project, slim down, and remove unnecessary features.

## Application Slimming

The current repository is already slimmed down around `web-antd`. If you want to simplify it further, start by removing the demo and documentation directories you do not need:

```bash
playground
docs

```

::: tip

If your project doesn’t include the `UI` component library you need, you can delete all other applications and create your own new application as needed.

:::

## Demo Code Slimming

If you don’t need demo code, you can simply delete the `playground` folder

## Documentation Slimming

If you don’t need documentation, you can delete the `docs` folder.

## Local API setup

The repository no longer ships with a built-in Nitro mock app. For local development, point `VITE_GLOB_API_URL` in `.env.development` to your existing backend or an external mock service.

## Installing Dependencies

Now that you’ve completed the slimming operations, you can install the dependencies and start your project:

```bash
# Run in the root directory
pnpm install

```

## Adjusting Commands

After slimming down, you may need to adjust commands according to your project. In the `package.json` file in the root directory, you can adjust the `scripts` field and remove any commands you don’t need.

```json
{
  "scripts": {
    "build:antd": "pnpm run build --filter=@vben/web-antd",
    "build:docs": "pnpm run build --filter=@vben/docs",
    "build:play": "pnpm run build --filter=@vben/playground",
    "dev:antd": "pnpm -F @vben/web-antd run dev",
    "dev:docs": "pnpm -F @vben/docs run dev",
    "dev:play": "pnpm -F @vben/playground run dev"
  }
}
```
