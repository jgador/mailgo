// Registers ts-node for the Electron main process in dev and loads the TS entrypoint.
require('ts-node').register({
  transpileOnly: true,
  project: __dirname + '/tsconfig.json'
});

require('./src/main/main.ts');
