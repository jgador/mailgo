import { ChildProcessWithoutNullStreams, spawn } from 'child_process';
import fs from 'fs';
import path from 'path';
import { app } from 'electron';
import { BACKEND_PORT, getBackendDirectory, getDataDirectory } from './config';

let backendProcess: ChildProcessWithoutNullStreams | null = null;

const findExecutable = (backendDir: string) => {
  const exePath = path.join(backendDir, process.platform === 'win32' ? 'Mailgo.AppHost.exe' : 'Mailgo.AppHost');
  const dllPath = path.join(backendDir, 'Mailgo.AppHost.dll');

  if (fs.existsSync(exePath)) {
    return { command: exePath, args: [] };
  }

  if (!fs.existsSync(dllPath)) {
    throw new Error('Backend binaries not found. Run npm run build:backend from desktop/ first.');
  }

  return { command: 'dotnet', args: [dllPath] };
};

export const startBackend = () => {
  const shouldStartEmbedded = app.isPackaged || process.env.START_EMBEDDED_BACKEND === 'true';
  const shouldSkipEmbedded = process.env.SKIP_EMBEDDED_BACKEND === 'true';

  if (!shouldStartEmbedded || shouldSkipEmbedded) {
    console.log('[backend] skipping embedded backend (expecting an external API on the configured port)');
    return;
  }

  if (backendProcess) {
    return;
  }

  const backendDir = getBackendDirectory();
  const dataDir = getDataDirectory();
  fs.mkdirSync(dataDir, { recursive: true });

  const executable = findExecutable(backendDir);
  const env = {
    ...process.env,
    ASPNETCORE_URLS: `http://localhost:${BACKEND_PORT}`,
    ConnectionStrings__Default: `Data Source=${path.join(dataDir, 'app.db')}`
  };

  backendProcess = spawn(executable.command, executable.args, {
    cwd: backendDir,
    env
  });

  backendProcess.stdout.on('data', (data) => {
    console.log(`[backend] ${data}`.trimEnd());
  });

  backendProcess.stderr.on('data', (data) => {
    console.error(`[backend] ${data}`.trimEnd());
  });

  backendProcess.on('close', (code) => {
    console.log(`[backend] exited with code ${code}`);
    backendProcess = null;
  });

  app.on('will-quit', stopBackend);
};

export const stopBackend = () => {
  if (!backendProcess) {
    return;
  }

  backendProcess.kill();
  backendProcess = null;
};
