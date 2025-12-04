import React, { useEffect, useState } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { EncryptionType } from '../types';
import {
  loadSmtpDefaults,
  saveSmtpDefaults,
  clearSmtpDefaults,
  SmtpDefaults,
} from '../utils/smtpDefaults';

const SettingsPage: React.FC = () => {
  const [form, setForm] = useState<SmtpDefaults>(loadSmtpDefaults());
  const [status, setStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    setForm(loadSmtpDefaults());
    setPassword('');
    setShowPassword(false);
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

  const handleReset = () => {
    clearSmtpDefaults();
    const defaults = loadSmtpDefaults();
    setForm(defaults);
    setPassword('');
    setShowPassword(false);
    setStatus('idle');
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Settings</h1>
        <p className="text-sm text-gray-600 mt-1">
          Configure default SMTP details used to pre-fill the send dialogs. Passwords are never persisted.
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
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="smtp.yourprovider.com"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Port</label>
            <input
              type="number"
              min={1}
              required
              value={form.port}
              onChange={(e) => handleChange('port', Number(e.target.value))}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
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
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="apikey or email"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Encryption</label>
            <select
              value={form.encryption}
              onChange={(e) => handleChange('encryption', e.target.value as EncryptionType)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
            >
              <option value={EncryptionType.None}>None</option>
              <option value={EncryptionType.StartTls}>STARTTLS</option>
              <option value={EncryptionType.SSL}>SSL/TLS</option>
            </select>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Password (not saved)
          </label>
          <div className="relative">
            <input
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full pr-11 pl-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
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
            This field is cleared every visit and never stored with your defaults.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Encryption Hostname (optional)
            </label>
            <input
              type="text"
              value={form.encryptionHostname || ''}
              onChange={(e) => handleChange('encryptionHostname', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="mail.example.com"
            />
          </div>
          <div className="flex items-center gap-3 pt-6">
            <input
              id="allowSelfSigned"
              type="checkbox"
              checked={form.allowSelfSigned}
              onChange={(e) => handleChange('allowSelfSigned', e.target.checked)}
              className="w-4 h-4 text-blue-600 border-gray-300 rounded"
            />
            <label htmlFor="allowSelfSigned" className="text-sm text-gray-700">
              Allow self-signed certificates
            </label>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Default From Name (optional)
            </label>
            <input
              type="text"
              value={form.overrideFromName || ''}
              onChange={(e) => handleChange('overrideFromName', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="MailGo"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Default From Email (optional)
            </label>
            <input
              type="email"
              value={form.overrideFromAddress || ''}
              onChange={(e) => handleChange('overrideFromAddress', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="no-reply@example.com"
            />
          </div>
        </div>

        <div className="flex flex-wrap gap-3 pt-4">
          <button
            type="submit"
            disabled={status === 'saving'}
            className="inline-flex items-center justify-center rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {status === 'saving' ? 'Saving...' : 'Save Defaults'}
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
          Passwords remain ephemeral and must be provided when launching a test or production send.
        </p>
      </form>
    </div>
  );
};

export default SettingsPage;
