// ESLint config enforcing this tool's own style: 4-space indent
// (matching germio's own C# convention), and snake_case for anything
// this codebase names itself (matching the JS convention already used
// in the master's own work, e.g. fsc-pipeline's dashboard).
export default [
  {
    files: ['**/*.js'],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: 'module',
    },
    rules: {
      indent: ['error', 4, { SwitchCase: 1 }],
      quotes: ['error', 'single', { avoidEscape: true }],
      semi: ['error', 'always'],
      'no-unused-vars': ['warn'],
    },
  },
];
