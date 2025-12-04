import React, { useEffect, useState } from 'react';
import { EncryptionType, SendNowRequest, SendTestRequest } from '../types';
import { X, Lock, Send, Mail } from 'lucide-react';
import { loadSmtpDefaults } from '../utils/smtpDefaults';

interface SmtpModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (settings: SendTestRequest | SendNowRequest) => Promise<void>;
  isTest?: boolean;
  title?: string;
}

const SmtpModal: React.FC<SmtpModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  isTest = false,
  title = 'SMTP Configuration',
}) => {
  const DEFAULT_PORT = 587;
  const [host, setHost] = useState('');
  const [port, setPort] = useState<number>(DEFAULT_PORT);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [encryption, setEncryption] = useState<EncryptionType>(EncryptionType.StartTls);
  const [testEmail, setTestEmail] = useState('');
  const [encryptionHostname, setEncryptionHostname] = useState('');
  const [allowSelfSigned, setAllowSelfSigned] = useState(false);
  const [overrideFromName, setOverrideFromName] = useState('');
  const [overrideFromAddress, setOverrideFromAddress] = useState('');
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    const defaults = loadSmtpDefaults();
    setHost(defaults.host || '');
    setPort(defaults.port || DEFAULT_PORT);
    setUsername(defaults.username || '');
    setEncryption(defaults.encryption || EncryptionType.StartTls);
    setEncryptionHostname(defaults.encryptionHostname || '');
    setAllowSelfSigned(Boolean(defaults.allowSelfSigned));
    setOverrideFromName(defaults.overrideFromName || '');
    setOverrideFromAddress(defaults.overrideFromAddress || '');
    setError(null);
    setLoading(false);
    setPassword('');
    if (isTest) {
      setTestEmail('');
    }
  }, [isOpen, isTest]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const basePayload: SendNowRequest = {
        smtpHost: host,
        smtpPort: port,
        smtpUsername: username || undefined,
        smtpPassword: password || undefined,
        encryption,
        encryptionHostname: encryptionHostname || undefined,
        allowSelfSigned,
        overrideFromName: overrideFromName || undefined,
        overrideFromAddress: overrideFromAddress || undefined,
      };

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
            <Lock className="w-4 h-4 text-blue-600" />
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
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Test Recipient</label>
              <div className="relative">
                <Mail className="absolute left-3 top-2.5 w-4 h-4 text-gray-400" />
                <input
                  type="email"
                  required
                  value={testEmail}
                  onChange={(e) => setTestEmail(e.target.value)}
                  className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-all"
                  placeholder="you@example.com"
                />
              </div>
            </div>
          )}

          <div className="grid grid-cols-3 gap-4">
            <div className="col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">SMTP Host</label>
              <input
                type="text"
                required
                value={host}
                onChange={(e) => setHost(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                placeholder="smtp.example.com"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Port</label>
              <input
                type="number"
                required
                value={port}
                onChange={(e) => setPort(Number(e.target.value))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                placeholder="587"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
             <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Encryption</label>
              <select
                value={encryption}
                onChange={(e) => setEncryption(e.target.value as EncryptionType)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              >
                <option value={EncryptionType.None}>None</option>
                <option value={EncryptionType.StartTls}>STARTTLS</option>
                <option value={EncryptionType.SSL}>SSL/TLS</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Encryption Hostname (optional)</label>
            <input
              type="text"
              value={encryptionHostname}
              onChange={(e) => setEncryptionHostname(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="mail.example.com"
            />
          </div>

          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              className="w-4 h-4 text-blue-600 border-gray-300 rounded"
              checked={allowSelfSigned}
              onChange={(e) => setAllowSelfSigned(e.target.checked)}
            />
            Allow self-signed certificates
          </label>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Username / Email</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="apikey or email"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Override From Name (optional)</label>
              <input
                type="text"
                value={overrideFromName}
                onChange={(e) => setOverrideFromName(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                placeholder="Mail Go"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Override From Email (optional)</label>
              <input
                type="email"
                value={overrideFromAddress}
                onChange={(e) => setOverrideFromAddress(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                placeholder="no-reply@example.com"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              placeholder="********"
              autoComplete="new-password"
            />
            <p className="text-xs text-gray-500 mt-1">Credentials are not stored permanently.</p>
          </div>

          <div className="pt-2">
            <button
              type="submit"
              disabled={loading}
              className="w-full flex items-center justify-center gap-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2.5 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
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
