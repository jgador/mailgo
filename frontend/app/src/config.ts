declare global {
  interface Window {
    electron?: {
      apiBaseUrl?: string;
    };
  }
}

type ConfigValues = {
  apiBaseUrl: string;
};

class AppConfig {
  readonly apiBaseUrl: string;

  private constructor(values: ConfigValues) {
    this.apiBaseUrl = values.apiBaseUrl;
  }

  static fromEnv(): AppConfig {
    const envApiBaseUrl = (process.env.REACT_APP_API_BASE_URL ?? '').trim();
    const electronApiBaseUrl =
      typeof window !== 'undefined' && window.electron?.apiBaseUrl
        ? window.electron.apiBaseUrl.trim()
        : '';

    const apiBaseUrl = envApiBaseUrl || electronApiBaseUrl;

    if (!apiBaseUrl) {
      throw new Error('API base URL must be set via REACT_APP_API_BASE_URL or provided by the desktop shell.');
    }

    return new AppConfig({ apiBaseUrl });
  }
}

export const appConfig = AppConfig.fromEnv();
