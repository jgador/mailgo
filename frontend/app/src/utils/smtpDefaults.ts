import { EncryptionType } from '../types';

const STORAGE_KEY = 'mailgo:smtp-defaults';

export interface SmtpDefaults {
  host: string;
  port: number;
  username?: string;
  encryption: EncryptionType;
  encryptionHostname?: string;
  allowSelfSigned: boolean;
  overrideFromName?: string;
  overrideFromAddress?: string;
}

const defaultValues: SmtpDefaults = {
  host: '',
  port: 587,
  username: '',
  encryption: EncryptionType.StartTls,
  encryptionHostname: '',
  allowSelfSigned: false,
  overrideFromName: '',
  overrideFromAddress: '',
};

export function loadSmtpDefaults(): SmtpDefaults {
  if (typeof window === 'undefined') {
    return { ...defaultValues };
  }

  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return { ...defaultValues };
    }
    const parsed = JSON.parse(raw) as Partial<SmtpDefaults>;
    return {
      ...defaultValues,
      ...parsed,
      port: Number(parsed.port ?? defaultValues.port),
    };
  } catch {
    return { ...defaultValues };
  }
}

export function saveSmtpDefaults(values: SmtpDefaults) {
  if (typeof window === 'undefined') {
    return;
  }
  const payload: SmtpDefaults = {
    ...defaultValues,
    ...values,
    port: Number(values.port) || defaultValues.port,
  };
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
}

export function clearSmtpDefaults() {
  if (typeof window === 'undefined') {
    return;
  }
  window.localStorage.removeItem(STORAGE_KEY);
}
