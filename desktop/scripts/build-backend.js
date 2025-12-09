const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const projectPath = path.resolve(__dirname, '../../backend/src/Mailgo.AppHost/Mailgo.AppHost.csproj');
const outputPath = path.resolve(__dirname, '../resources/backend');

fs.mkdirSync(outputPath, { recursive: true });

const runtime =
  process.platform === 'win32'
    ? 'win-x64'
    : process.platform === 'darwin'
      ? 'osx-x64'
      : 'linux-x64';

console.log(`Publishing backend to ${outputPath}`);
const publishArgs = [
  'publish',
  projectPath,
  '-c',
  'Release',
  '-o',
  outputPath,
  '-r',
  runtime,
  '--self-contained',
  'true',
  '-p:PublishSingleFile=true',
  '-p:IncludeNativeLibrariesForSelfExtract=true',
  '-p:PublishTrimmed=false'
];

const result = spawnSync('dotnet', publishArgs, {
  stdio: 'inherit'
});

if (result.status !== 0) {
  console.error('dotnet publish failed');
  process.exit(result.status ?? 1);
}

console.log('Backend publish complete.');
