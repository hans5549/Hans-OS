# UI Framework Switching

`Vue Admin` currently keeps `Ant Design Vue` as the default UI framework for the demo app, consistent with the older version. If you want another UI framework, follow the steps below to add your own app variant.

## Adding a New UI Framework

If you want to use a different UI framework, you only need to follow these steps:

1. Create a new folder inside `apps`, for example, `apps/web-xxx`.
2. Change the `name` field in `apps/web-xxx/package.json` to `web-xxx`.
3. Remove dependencies and code from other UI frameworks and replace them with your chosen UI framework's logic, which requires minimal changes.
4. Adjust the language files within `locales`.
5. Adjust the components in `app.vue`.
6. Adapt the theme of the UI framework to match `Vben Admin`.
7. Adjust the application name in `.env`.
8. Add a `dev:xxx` script in the root directory of the repository.
9. Run `pnpm install` to install dependencies.
