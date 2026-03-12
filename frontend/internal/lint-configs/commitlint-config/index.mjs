import { execSync } from 'node:child_process';

import { getPackagesSync } from '@vben/node-utils';

const { packages } = getPackagesSync();

const allowedScopes = [
  ...packages.map((pkg) => pkg.packageJson.name),
  'project',
  'style',
  'lint',
  'ci',
  'dev',
  'deploy',
  'other',
];

// precomputed scope
const scopeComplete = execSync('git status --porcelain || true')
  .toString()
  .trim()
  .split('\n')
  .find((r) => ~r.indexOf('M  src'))
  ?.replaceAll(/(\/)/g, '%%')
  ?.match(/src%%((\w|-)*)/)?.[1]
  ?.replace(/s$/, '');

/**
 * @type {import('cz-git').UserConfig}
 */
const userConfig = {
  extends: ['@commitlint/config-conventional'],
  plugins: ['commitlint-plugin-function-rules'],
  prompt: {
    /** @use `pnpm commit :f` */
    alias: {
      b: 'build: bump dependencies',
      c: 'chore: update config',
      f: 'docs: fix typos',
      r: 'docs: update README',
      s: 'style: update code format',
    },
    allowCustomIssuePrefixs: false,
    // scopes: [...scopes, 'mock'],
    allowEmptyIssuePrefixs: false,
    customScopesAlign: scopeComplete ? 'bottom' : 'top',
    defaultScope: scopeComplete,
    // English
    typesAppend: [
      { name: 'workflow: workflow improvements', value: 'workflow' },
      { name: 'types:    type definition file changes', value: 'types' },
    ],

    // 中英文對照版
    // messages: {
    //   type: '選擇你要提交的類型 :',
    //   scope: '選擇一個提交範圍 (可選):',
    //   customScope: '請輸入自訂的提交範圍 :',
    //   subject: '填寫簡短精煉的變更描述 :\n',
    //   body: '填寫更詳細的變更描述 (可選)。使用 "|" 換行 :\n',
    //   breaking: '列舉非相容性重大的變更 (可選)。使用 "|" 換行 :\n',
    //   footerPrefixsSelect: '選擇關聯 issue 前綴 (可選):',
    //   customFooterPrefixs: '輸入自訂 issue 前綴 :',
    //   footer: '列舉關聯 issue (可選) 例如: #31, #I3244 :\n',
    //   confirmCommit: '是否提交或修改 commit ?',
    // },
    // types: [
    //   { value: 'feat', name: 'feat:     新增功能' },
    //   { value: 'fix', name: 'fix:      修復缺陷' },
    //   { value: 'docs', name: 'docs:     文件變更' },
    //   { value: 'style', name: 'style:    程式碼格式' },
    //   { value: 'refactor', name: 'refactor: 程式碼重構' },
    //   { value: 'perf', name: 'perf:     效能最佳化' },
    //   { value: 'test', name: 'test:     補上遺漏測試或調整既有測試' },
    //   { value: 'build', name: 'build:    建置流程、外部依賴變更 (如升級 npm 套件、修改打包設定等)' },
    //   { value: 'ci', name: 'ci:       修改 CI 設定、腳本' },
    //   { value: 'revert', name: 'revert:   回滾 commit' },
    //   { value: 'chore', name: 'chore:    對建置流程或輔助工具與函式庫的變更 (不影響原始碼、測試案例)' },
    //   { value: 'wip', name: 'wip:      開發中' },
    //   { value: 'workflow', name: 'workflow: 工作流程改进' },
    //   { value: 'types', name: 'types:    类型定义文件修改' },
    // ],
    // emptyScopesAlias: 'empty:      不填写',
    // customScopesAlias: 'custom:     自定义',
  },
  rules: {
    /**
     * type[scope]: [function] description
     *
     * ^^^^^^^^^^^^^^ empty line.
     * - Something here
     */
    'body-leading-blank': [2, 'always'],
    /**
     * type[scope]: [function] description
     *
     * - something here
     *
     * ^^^^^^^^^^^^^^
     */
    'footer-leading-blank': [1, 'always'],
    /**
     * type[scope]: [function] description
     *      ^^^^^
     */
    'function-rules/scope-enum': [
      2, // level: error
      'always',
      (parsed) => {
        if (!parsed.scope || allowedScopes.includes(parsed.scope)) {
          return [true];
        }

        return [false, `scope must be one of ${allowedScopes.join(', ')}`];
      },
    ],
    /**
     * type[scope]: [function] description [No more than 108 characters]
     *      ^^^^^
     */
    'header-max-length': [2, 'always', 108],

    'scope-enum': [0],
    'subject-case': [0],
    'subject-empty': [2, 'never'],
    'type-empty': [2, 'never'],
    /**
     * type[scope]: [function] description
     * ^^^^
     */
    'type-enum': [
      2,
      'always',
      [
        'feat',
        'fix',
        'perf',
        'style',
        'docs',
        'test',
        'refactor',
        'build',
        'ci',
        'chore',
        'revert',
        'types',
        'release',
      ],
    ],
  },
};

export default userConfig;
