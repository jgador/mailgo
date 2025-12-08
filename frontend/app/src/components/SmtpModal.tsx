import React, { useEffect, useState } from 'react';
import { EncryptionType, SendNowRequest, SendTestRequest, SmtpPublicKey } from '../types';
import { X, Lock, Send, Mail, Eye, EyeOff } from 'lucide-react';
import { securityService } from '../services/api';
import { encryptWithPublicKey } from '../utils/crypto';
import {
  isSessionPasswordCompatible,
  loadSessionPassword,
  saveSessionPassword,
  SessionPasswordCache,
} from '../utils/sessionPassword';
import { loadSmtpDefaults } from '../utils/smtpDefaults';

interface SmtpModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (settings: SendTestRequest | SendNowRequest) => Promise<void>;
  isTest?: boolean;
  title?: string;
  summaryCampaignName?: string;
  summarySubject?: string;
  summaryFromName?: string;
  summaryFromEmail?: string;
}

const SmtpModal: React.FC<SmtpModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isTest = false,
  title = 'SMTP Configuration',
  summaryCampaignName,
  summarySubject,
  summaryFromName,
  summaryFromEmail,
}) => {
  const DEFAULT_PORT = 587;
  const [host, setHost] = useState('');
  const [port, setPort] = useState<number>(DEFAULT_PORT);
  const [portInput, setPortInput] = useState<string>(String(DEFAULT_PORT));
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [encryptedPassword, setEncryptedPassword] = useState<SessionPasswordCache | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [encryption, setEncryption] = useState<EncryptionType>(EncryptionType.StartTls);
  const [testEmail, setTestEmail] = useState('');
  const [smtpKey, setSmtpKey] = useState<SmtpPublicKey | null>(null);
  const [showSettings, setShowSettings] = useState(false);
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    const defaults = loadSmtpDefaults();
    setHost(defaults.host || '');
    setPort(defaults.port || DEFAULT_PORT);
    setPortInput(String(defaults.port || DEFAULT_PORT));
    setUsername(defaults.username || '');
    setEncryption(defaults.encryption || EncryptionType.StartTls);
    setError(null);
    setLoading(false);
    setPassword('');
    setEncryptedPassword(null);
    setShowPassword(false);
    setPasswordError(null);
    setShowSettings(false);
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
        console.error('Failed to fetch SMTP public key', err);
        setPasswordError('Unable to load encryption key. Password will not be sent.');
      });
    if (isTest) {
      setTestEmail('');
    }
    return () => abortController.abort();
  }, [isOpen, isTest]);

  if (!isOpen) return null;

  const handlePortChange = (value: string) => {
    const numericValue = value.replace(/\D/g, '');
    setPortInput(numericValue);
    if (numericValue) {
      setPort(Number(numericValue));
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
      console.error('Failed to encrypt SMTP password', err);
      setPasswordError('Unable to encrypt password. Please retry.');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    setPasswordError(null);

    try {
      let passwordPayload = encryptedPassword;
      if (password && smtpKey) {
        const cipherText = await encryptWithPublicKey(smtpKey.publicKeyPem, password);
        passwordPayload = { cipherText, keyId: smtpKey.keyId };
        saveSessionPassword(passwordPayload);
      }

      const basePayload: SendNowRequest = {
        smtpHost: host,
        smtpPort: port,
        smtpUsername: username || undefined,
        smtpPasswordEncrypted: passwordPayload?.cipherText,
        smtpPasswordKeyId: passwordPayload?.keyId,
        encryption,
        allowSelfSigned: false,
      };

      if (password && !smtpKey) {
        throw new Error('Cannot send without encryption key. Please reload and try again.');
      }

      if (isTest) {
        if (!testEmail) throw new Error("Test recipient email is required.");
        const payload = { ...basePayload, testEmail };
        await onSubmit(payload);
      } else {
        await onSubmit(basePayload);
      }
      onClose();
    } catch (err: any) {
      setError(err.message || 'Failed to send. Check settings.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md overflow-hidden animate-in fade-in zoom-in duration-200">
        <div className="bg-gray-50 px-6 py-4 border-b border-gray-100 flex justify-between items-center">
          <h3 className="font-semibold text-gray-800 flex items-center gap-2">
            <Lock className="w-4 h-4 text-brand-blue" />
            {title}
          </h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 transition-colors">
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="bg-red-50 text-red-600 text-sm p-3 rounded-lg border border-red-100">
              {error}
            </div>
          )}

          {isTest && (
            <div className="space-y-2">
              <label className="block text-sm font-medium text-gray-700">Test Recipient</label>
              <div className="relative">
                <Mail className="absolute left-3 top-2.5 w-4 h-4 text-gray-400" />
                <input
                  type="email"
                  required
                  value={testEmail}
                  onChange={(e) => setTestEmail(e.target.value)}
                  className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue focus:border-brand-blue outline-none transition-all"
                  placeholder="you@example.com"
                />
              </div>
            </div>
          )}

          {!isTest && (
            <div className="space-y-3">
              <h4 className="text-sm font-semibold text-gray-800">
                Ready to send {summaryCampaignName ? `"${summaryCampaignName}"` : 'this campaign'}?
              </h4>
              <div className="bg-gray-50 border border-gray-200 rounded-lg p-4 space-y-2">
                <div className="text-sm text-gray-700">
                  <span className="font-semibold text-gray-800">From:</span>{' '}
                  {summaryFromName || '—'} &lt;{summaryFromEmail || '—'}&gt;
                </div>
                <div className="text-sm text-gray-700">
                  <span className="font-semibold text-gray-800">Subject:</span> {summarySubject || '—'}
                </div>
                <div className="text-sm text-gray-700">
                  <span className="font-semibold text-gray-800">Delivery Method:</span> Sending via {host || '—'}
                </div>
              </div>
              <button
                type="button"
                onClick={() => setShowSettings((prev) => !prev)}
                className="text-sm text-brand-blue hover:text-brand-blue-dark font-semibold inline-flex items-center gap-2"
                aria-expanded={showSettings}
              >
                <span role="img" aria-hidden="true">⚙️</span> Change Connection Settings
              </button>
            </div>
          )}

          {(isTest || showSettings) && (
            <div className="space-y-4">
              {!isTest && <div className="h-px bg-gray-200" aria-hidden />}

              <div className="grid grid-cols-3 gap-4">
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">SMTP Host</label>
                  <input
                    type="text"
                    required
                    value={host}
                    onChange={(e) => setHost(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
                    placeholder="smtp.example.com"
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

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Encryption</label>
                <select
                  value={encryption}
                  onChange={(e) => setEncryption(e.target.value as EncryptionType)}
                  className="w-full h-10 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
                >
                  <option value={EncryptionType.None}>None</option>
                  <option value={EncryptionType.StartTls}>STARTTLS</option>
                  <option value={EncryptionType.SSL}>SSL/TLS</option>
                </select>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Username / Email</label>
                  <input
                    type="text"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
                    placeholder="apikey or email"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
                  <div className="relative">
                    <input
                      type={showPassword ? 'text' : 'password'}
                      value={password}
                      onChange={(e) => handlePasswordChange(e.target.value)}
                      className="w-full pr-11 pl-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none"
                      placeholder="********"
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
                    Encrypted in session storage with key {smtpKey?.keyId ?? 'loading...'}; clears when the tab closes.
                  </p>
                  {encryptedPassword && (
                    <p className="text-xs text-green-600 mt-1">Password cached for this session.</p>
                  )}
                  {passwordError && <p className="text-xs text-red-600 mt-1">{passwordError}</p>}
                </div>
              </div>
            </div>
          )}

          <div className="pt-2">
            <button
              type="submit"
              disabled={loading}
              className="w-full flex items-center justify-center gap-2 bg-brand-blue hover:bg-brand-blue-dark text-white font-semibold py-2.5 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? (
                <span className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <>
                  <Send size={18} />
                  {isTest ? 'Send Test Email' : 'Start Campaign'}
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default SmtpModal;
