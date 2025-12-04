import axios from 'axios';
import {
  Campaign,
  CampaignSendLog,
  CampaignSummary,
  CreateCampaignRequest,
  PagedResult,
  Recipient,
  RecipientUploadResult,
  SendNowRequest,
  SendTestRequest,
} from '../types';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const recipientService = {
  getAll: async (page = 1, pageSize = 50): Promise<PagedResult<Recipient>> => {
    const response = await api.get<PagedResult<Recipient>>('/recipients', {
      params: { page, pageSize },
    });
    return response.data;
  },
  uploadCsv: async (file: File): Promise<RecipientUploadResult> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<RecipientUploadResult>('/recipients/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
};

export const campaignService = {
  getAll: async (): Promise<CampaignSummary[]> => {
    const response = await api.get<CampaignSummary[]>('/campaigns');
    return response.data;
  },
  getById: async (id: string): Promise<Campaign> => {
    const response = await api.get<Campaign>(`/campaigns/${id}`);
    return response.data;
  },
  create: async (data: CreateCampaignRequest): Promise<Campaign> => {
    const response = await api.post<Campaign>('/campaigns', data);
    return response.data;
  },
  update: async (id: string, data: CreateCampaignRequest): Promise<Campaign> => {
    const response = await api.put<Campaign>(`/campaigns/${id}`, data);
    return response.data;
  },
  sendTest: async (id: string, data: SendTestRequest): Promise<void> => {
    await api.post(`/campaigns/${id}/send-test`, data);
  },
  sendNow: async (id: string, data: SendNowRequest): Promise<void> => {
    await api.post(`/campaigns/${id}/send-now`, data);
  },
  getLogs: async (id: string): Promise<CampaignSendLog[]> => {
    const response = await api.get<CampaignSendLog[]>(`/campaigns/${id}/logs`);
    return response.data;
  },
};
