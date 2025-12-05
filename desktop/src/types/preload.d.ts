export interface ElectronApi {
  apiBaseUrl: string;
  appVersion: string;
  openExternal: (url: string) => Promise<void>;
}

declare global {
  interface Window {
    electron?: ElectronApi;
  }
}
