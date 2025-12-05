import { contextBridge, shell } from 'electron';
import { API_BASE_URL } from './config';

contextBridge.exposeInMainWorld('electron', {
  apiBaseUrl: API_BASE_URL,
  appVersion: process.env.MAILGO_APP_VERSION ?? 'dev',
  openExternal: (url: string) => shell.openExternal(url)
});
