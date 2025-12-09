import React, { useEffect, useState } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { EncryptionType, SmtpPublicKey } from '../types';
import { securityService } from '../services/api';
import { encryptWithPublicKey } from '../utils/crypto';
import {
  isSessionPasswordCompatible,
  loadSessionPassword,
  saveSessionPassword,
  SessionPasswordCache,
} from '../utils/sessionPassword';
import {
  loadSmtpDefaults,
  saveSmtpDefaults,
  clearSmtpDefaults,
  SmtpDefaults,
} from '../utils/smtpDefaults';

const SettingsPage: React.FC = () => {
  const [form, setForm] = useState<SmtpDefaults>(() => loadSmtpDefaults());
  const [portInput, setPortInput] = useState<string>(() => String(loadSmtpDefaults().port));
  const [status, setStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [password, setPassword] = useState('');
  const [encryptedPassword, setEncryptedPassword] = useState<SessionPasswordCache | null>(null);
  const [smtpKey, setSmtpKey] = useState<SmtpPublicKey | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    const defaults = loadSmtpDefaults();
    setForm(defaults);
    setPortInput(String(defaults.port));
    setPassword('');
    setPasswordError(null);
    const cached = loadSessionPassword();
    const abortController = new AbortController();
    securityService
      .getSmtpKey()
      .then((key) => {
        if (abortController.signal.aborted) return;
        setSmtpKey(key);
        if (isSessionPasswordCompatible(cached, key.keyId)) {
          setEncryptedPassword(cached);
        } else {
          saveSessionPassword(null);
        }
      })
      .catch((err) => {
        if (abortController.signal.aborted) return;
        console.error('Failed to load SMTP encryption key', err);
        setPasswordError('Unable to load encryption key. Password will not be cached.');
      });
    setShowPassword(false);
    return () => abortController.abort();
  }, []);

  const handleChange = <K extends keyof SmtpDefaults>(key: K, value: SmtpDefaults[K]) => {
    setForm((prev) => ({
      ...prev,
      [key]: value,
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setStatus('saving');
    try {
      saveSmtpDefaults(form);
      setStatus('saved');
      setTimeout(() => setStatus('idle'), 2500);
    } catch (err) {
      console.error('Failed to persist SMTP defaults', err);
      setStatus('error');
    }
  };

  const handlePasswordChange = async (value: string) => {
    setPassword(value);
    setPasswordError(null);
    if (!value) {
      setEncryptedPassword(null);
      saveSessionPassword(null);
      return;
    }
    if (!smtpKey) {
      setPasswordError('Encryption key unavailable.');
      return;
    }
    try {
      const cipherText = await encryptWithPublicKey(smtpKey.publicKeyPem, value);
      const payload = { cipherText, keyId: smtpKey.keyId };
      setEncryptedPassword(payload);
      saveSessionPassword(payload);
    } catch (err) {
      console.error('Failed to encrypt password', err);
      setPasswordError('Unable to encrypt password. Please retry.');
    }
  };

  const handlePortChange = (value: string) => {
    const numericValue = value.replace(/\D/g, '');
    setPortInput(numericValue);
    if (numericValue) {
      handleChange('port', Number(numericValue));
    }
  };

  const handleReset = () => {
    clearSmtpDefaults();
    const defaults = loadSmtpDefaults();
    setForm({ ...defaults, port: 0, allowSelfSigned: false });
    setPortInput('');
    setPassword('');
    setEncryptedPassword(null);
    saveSessionPassword(null);
    setShowPassword(false);
    setStatus('idle');
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Settings</h1>
        <p className="text-sm text-gray-600 mt-1">
          Configure default SMTP details used to pre-fill the send dialogs. Passwords are encrypted for this browser session only.
        </p>
      </div>

      <form
        onSubmit={handleSubmit}
        className="bg-white shadow-sm border border-gray-200 rounded-xl p-6 space-y-5"
      >
        {status === 'saved' && (
          <div className="rounded-md bg-green-50 border border-green-200 px-4 py-2 text-sm text-green-700">
            Defaults saved. They will be applied the next time you send a test or campaign.
          </div>
        )}
        {status === 'error' && (
          <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
            Unable to save settings. Please check your browser storage permissions.
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-1">SMTP Host</label>
            <input
              type="text"
              required
              value={form.host}
              onChange={(e) => handleChange('host', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
              placeholder="smtp.yourprovider.com"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Port</label>
            <input
              type="text"
              inputMode="numeric"
              pattern="[0-9]*"
              required
              value={portInput}
              onChange={(e) => handlePortChange(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
              placeholder="587"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Username / Email</label>
            <input
              type="text"
              value={form.username || ''}
              onChange={(e) => handleChange('username', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
              placeholder="apikey or email"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Encryption</label>
            <select
              value={form.encryption}
              onChange={(e) => handleChange('encryption', e.target.value as EncryptionType)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
            >
              <option value={EncryptionType.None}>None</option>
              <option value={EncryptionType.StartTls}>STARTTLS</option>
              <option value={EncryptionType.SSL}>SSL/TLS</option>
            </select>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Password (session only)
          </label>
          <div className="relative">
            <input
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={(e) => handlePasswordChange(e.target.value)}
              className="w-full pr-11 pl-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
              placeholder="Only used when sending"
              autoComplete="new-password"
            />
            <button
              type="button"
              onClick={() => setShowPassword((prev) => !prev)}
              className="absolute inset-y-0 right-2 flex items-center text-gray-400 hover:text-gray-600"
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>
          <p className="text-xs text-gray-500 mt-1">
            Encrypted with key {smtpKey?.keyId ?? 'loading...'} and stored in session storage; clears when the tab or app closes.
          </p>
          {encryptedPassword && (
            <p className="text-xs text-green-600 mt-1">Password cached for this session.</p>
          )}
          {passwordError && <p className="text-xs text-red-600 mt-1">{passwordError}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Default From Name (optional)
          </label>
          <input
            type="text"
            value={form.overrideFromName || ''}
            onChange={(e) => handleChange('overrideFromName', e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
            placeholder="Mailgo"
          />
          <p className="text-xs text-gray-500 mt-1">
            Sender email will use the SMTP username.
          </p>
        </div>

        <div className="flex flex-wrap gap-3 pt-4">
          <button
            type="submit"
            disabled={status === 'saving'}
            className="inline-flex items-center justify-center rounded-lg bg-brand-blue px-4 py-2 text-sm font-semibold text-white hover:bg-brand-blue-dark transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {status === 'saving' ? 'Saving...' : 'Save'}
          </button>
          <button
            type="button"
            onClick={handleReset}
            className="inline-flex items-center justify-center rounded-lg border border-gray-300 px-4 py-2 text-sm font-semibold text-gray-700 hover:bg-gray-50 transition-colors"
          >
            Reset to Blank
          </button>
        </div>

        <p className="text-xs text-gray-500">
          Passwords are encrypted in session storage and clear when the tab or app closes; they are not saved with your defaults.
        </p>
      </form>
    </div>
  );
};

export default SettingsPage;
