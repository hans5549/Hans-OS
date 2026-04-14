# Directory Explanation

The directory uses Monorepo management, and the project structure is as follows:

```bash
.
├── Dockerfile # Docker image build file
├── README.md # Project documentation
├── apps # Project applications directory
│   └── web-antd # Frontend application based on Ant Design Vue
├── build-local-docker-image.sh # Script for building Docker images locally
├── cspell.json # CSpell configuration file
├── docs # Project documentation directory
├── eslint.config.mjs # ESLint configuration file
├── internal # Internal tools directory
│   ├── lint-configs # Code linting configurations
│   │   ├── commitlint-config # Commitlint configuration
│   │   ├── eslint-config # ESLint configuration
│   │   ├── prettier-config # Prettier configuration
│   │   └── stylelint-config # Stylelint configuration
│   ├── node-utils # Node.js tools
│   ├── tailwind-config # Tailwind configuration
│   ├── tsconfig # Common tsconfig settings
│   └── vite-config # Common Vite configuration
├── package.json # Project dependency configuration
├── packages # Project packages directory
│   ├── @core # Core package
│   │   ├── base # Base package
│   │   │   ├── design # Design related
│   │   │   ├── icons # Icons
│   │   │   ├── shared # Shared
│   │   │   └── typings # Type definitions
│   │   ├── composables # Composable APIs
│   │   ├── preferences # Preferences
│   │   └── ui-kit # UI component collection
│   │       ├── layout-ui # Layout UI
│   │       ├── menu-ui  # Menu UI
│   │       ├── shadcn-ui # shadcn UI
│   │       └── tabs-ui # Tabs UI
│   ├── constants # Constants
│   ├── effects # Effects related packages
│   │   ├── access # Access control
│   │   ├── plugins # Plugins
│   │   ├── common-ui # Common UI
│   │   ├── hooks # Composable APIs
│   │   ├── layouts # Layouts
│   │   └── request # Request
│   ├── icons # Icons
│   ├── locales # Internationalization
│   ├── preferences  # Preferences
│   ├── stores # State management
│   ├── styles # Styles
│   ├── types # Type definitions
│   └── utils # Utilities
├── playground # Demo directory
├── pnpm-lock.yaml # pnpm lock file
├── pnpm-workspace.yaml # pnpm workspace configuration file
├── scripts # Scripts directory
│   ├── turbo-run # Turbo run script
│   └── vsh # VSH script
├── stylelint.config.mjs # Stylelint configuration file
├── turbo.json # Turbo configuration file
├── vben-admin.code-workspace # VS Code workspace configuration file
└── vitest.config.ts # Vite configuration file
```
