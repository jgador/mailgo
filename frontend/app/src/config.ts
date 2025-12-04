type ConfigValues = {
  apiBaseUrl: string;
};

class AppConfig {
  readonly apiBaseUrl: string;

  private constructor(values: ConfigValues) {
    this.apiBaseUrl = values.apiBaseUrl;
  }

  static fromEnv(): AppConfig {
    const apiBaseUrl = (process.env.REACT_APP_API_BASE_URL ?? '').trim();

    if (!apiBaseUrl) {
      throw new Error('REACT_APP_API_BASE_URL must be set (e.g. in .env.local).');
    }

    return new AppConfig({ apiBaseUrl });
  }
}

export const appConfig = AppConfig.fromEnv();
