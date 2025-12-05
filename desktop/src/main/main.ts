import { app, BrowserWindow, shell, Menu } from 'electron';
import path from 'path';
import { getAppIcon, getFrontendEntry } from './config';
import { startBackend, stopBackend } from './backend';

const APP_VERSION = app.getVersion();
process.env.MAILGO_APP_VERSION = APP_VERSION;

const createWindow = () => {
  const frontend = getFrontendEntry();

  // Remove all default menus in the desktop shell.
  Menu.setApplicationMenu(null);

  const win = new BrowserWindow({
    width: 1280,
    height: 800,
    icon: getAppIcon(),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    },
    autoHideMenuBar: true
  });

  if (frontend.isDev) {
    win.webContents.openDevTools({ mode: 'detach' });
  }

  const loadUrl = frontend.isDev ? frontend.url : `${frontend.url}#/`;
  win.loadURL(loadUrl);

  win.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url);
    return { action: 'deny' };
  });
};

app.whenReady().then(() => {
  startBackend();
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  stopBackend();
});
