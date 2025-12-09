const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Reuse the PowerShell publish script so we have a single source of truth.
// Note: this requires Windows/PowerShell. For non-Windows, keep the older
// dotnet publish flow or add a cross-platform script as needed.
const psScript = path.resolve(__dirname, 'build-backend.ps1');

if (process.platform !== 'win32') {
  console.error('[build-backend] PowerShell script is Windows-only. Run dotnet publish manually on this OS.');
  process.exit(1);
}

// Ensure output directory exists (script will write here).
const outputPath = path.resolve(__dirname, '../resources/backend');
fs.mkdirSync(outputPath, { recursive: true });

console.log(`[build-backend] Publishing via PowerShell: ${psScript}`);
const result = spawnSync('powershell.exe', ['-ExecutionPolicy', 'Bypass', '-File', psScript], {
  stdio: 'inherit'
});

if (result.status !== 0) {
  console.error('[build-backend] dotnet publish failed via PowerShell');
  process.exit(result.status ?? 1);
}

console.log('[build-backend] Backend publish complete.');
