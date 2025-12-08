const pemToArrayBuffer = (pem: string): ArrayBuffer => {
  const cleaned = pem.replace(/-----BEGIN PUBLIC KEY-----/, '')
    .replace(/-----END PUBLIC KEY-----/, '')
    .replace(/\s+/g, '');
  const binary = window.atob(cleaned);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
};

const arrayBufferToBase64 = (buffer: ArrayBuffer): string => {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (let i = 0; i < bytes.byteLength; i += 1) {
    binary += String.fromCharCode(bytes[i]);
  }
  return window.btoa(binary);
};

export const encryptWithPublicKey = async (publicKeyPem: string, plaintext: string): Promise<string> => {
  if (!window.crypto?.subtle) {
    throw new Error('WebCrypto is unavailable in this environment.');
  }

  const keyData = pemToArrayBuffer(publicKeyPem);
  const publicKey = await window.crypto.subtle.importKey(
    'spki',
    keyData,
    {
      name: 'RSA-OAEP',
      hash: 'SHA-256',
    },
    false,
    ['encrypt'],
  );

  const encoder = new TextEncoder();
  const encrypted = await window.crypto.subtle.encrypt(
    { name: 'RSA-OAEP' },
    publicKey,
    encoder.encode(plaintext),
  );

  return arrayBufferToBase64(encrypted);
};
