import path from 'path';
import { app } from 'electron';

export const FRONTEND_DEV_URL = process.env.ELECTRON_RENDERER_URL ?? 'http://localhost:3000';
export const BACKEND_PORT = process.env.MAILGO_BACKEND_PORT ?? '8080';
export const API_BASE_URL = `http://localhost:${BACKEND_PORT}/api`;

export const getFrontendEntry = () => {
  if (!app.isPackaged) {
    return { isDev: true, url: FRONTEND_DEV_URL };
  }

  const frontendDir = path.join(process.resourcesPath, 'frontend');
  const indexPath = path.join(frontendDir, 'index.html');
  return { isDev: false, url: `file://${indexPath}` };
};

export const getBackendDirectory = () => {
  if (!app.isPackaged) {
    return path.resolve(__dirname, '../../resources/backend');
  }

  return path.join(process.resourcesPath, 'backend');
};

export const getDataDirectory = () => {
  const dataDir = path.join(app.getPath('userData'), 'data');
  return dataDir;
};

export const getAppIcon = () => {
  if (!app.isPackaged) {
    return path.resolve(__dirname, '../assets/icons/icon.png');
  }

  const resourcesPath = (process as NodeJS.Process & { resourcesPath: string }).resourcesPath;
  return path.join(resourcesPath, 'icons', 'icon.png');
};
