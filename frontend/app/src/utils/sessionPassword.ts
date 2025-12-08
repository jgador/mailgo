export interface SessionPasswordCache {
  cipherText: string;
  keyId: string;
}

const SESSION_PASSWORD_KEY = 'mailgo:smtp-password-session';

export const loadSessionPassword = (): SessionPasswordCache | null => {
  if (typeof window === 'undefined') return null;
  try {
    const raw = window.sessionStorage.getItem(SESSION_PASSWORD_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as SessionPasswordCache;
    if (!parsed?.cipherText || !parsed?.keyId) return null;
    return parsed;
  } catch {
    return null;
  }
};

export const saveSessionPassword = (value: SessionPasswordCache | null) => {
  if (typeof window === 'undefined') return;
  if (!value) {
    window.sessionStorage.removeItem(SESSION_PASSWORD_KEY);
    return;
  }
  window.sessionStorage.setItem(SESSION_PASSWORD_KEY, JSON.stringify(value));
};

export const isSessionPasswordCompatible = (
  cache: SessionPasswordCache | null,
  keyId?: string,
): cache is SessionPasswordCache => {
  if (!cache || !keyId) return false;
  return cache.keyId === keyId;
};
