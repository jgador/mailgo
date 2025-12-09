export enum CampaignStatus {
  Draft = 'Draft',
  Sending = 'Sending',
  Completed = 'Completed',
  Failed = 'Failed',
}

export interface CampaignSummary {
  id: string;
  name: string;
  subject: string;
  fromName: string;
  status: CampaignStatus;
  totalRecipients: number;
  sentCount: number;
  failedCount: number;
  createdAt: string;
  lastUpdatedAt: string;
}

export interface Campaign extends CampaignSummary {
  htmlBody: string;
  textBody?: string;
}

export interface Recipient {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  createdAt: string;
}

export interface RecipientUploadResult {
  totalRows: number;
  inserted: number;
  skippedInvalid: number;
}

export interface CampaignSendLog {
  id: string;
  campaignId: string;
  recipientEmail: string; // Flattened for display usually, or join with recipient
  status: 'Sent' | 'Failed';
  errorMessage?: string;
  sentAt?: string;
}

export enum EncryptionType {
  None = 'None',
  SSL = 'SSL',
  StartTls = 'StartTls',
}

export interface SmtpSettings {
  smtpHost: string;
  smtpPort: number;
  smtpUsername?: string;
  smtpPassword?: string;
  smtpPasswordEncrypted?: string;
  smtpPasswordKeyId?: string;
  encryption: EncryptionType;
  allowSelfSigned?: boolean;
  overrideFromName?: string;
}

export interface SendTestRequest extends SmtpSettings {
  testEmail: string;
}

export interface SendNowRequest extends SmtpSettings {}

export interface CreateCampaignRequest {
  name: string;
  subject: string;
  fromName: string;
  htmlBody: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface SmtpPublicKey {
  keyId: string;
  publicKeyPem: string;
}
