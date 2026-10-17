// eslint-disable-next-line @typescript-eslint/no-var-requires
const { getDefaultConfig } = require('expo/metro-config');
// eslint-disable-next-line @typescript-eslint/no-var-requires
const path = require('path');

const projectRoot = __dirname;
// Only the shared tokens folder, not the whole repo (Frontend/Backend/demo
// would otherwise get crawled/watched too, which is slow and unnecessary).
const sharedTokensDir = path.resolve(projectRoot, '../../design-tokens');

const config = getDefaultConfig(projectRoot);

// Allow Metro to resolve and watch files outside this project's own folder
// (the shared ../../design-tokens/tokens.json), since this repo isn't an
// npm workspace and there's no other way to import across project roots.
config.watchFolders = [sharedTokensDir];
config.resolver.nodeModulesPaths = [path.resolve(projectRoot, 'node_modules')];

module.exports = config;
