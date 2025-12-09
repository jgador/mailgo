const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const projectPath = path.resolve(__dirname, '../../backend/src/Mailgo.AppHost/Mailgo.AppHost.csproj');
const outputPath = path.resolve(__dirname, '../resources/backend');

fs.mkdirSync(outputPath, { recursive: true });

console.log(`Publishing backend to ${outputPath}`);
const result = spawnSync('dotnet', ['publish', projectPath, '-c', 'Release', '-o', outputPath], {
  stdio: 'inherit'
});

if (result.status !== 0) {
  console.error('dotnet publish failed');
  process.exit(result.status ?? 1);
}

console.log('Backend publish complete.');
