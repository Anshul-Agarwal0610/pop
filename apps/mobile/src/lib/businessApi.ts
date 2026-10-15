import { apiRequest } from './api';
import type { BusinessAccount, BusinessCampaign } from '../types/business';

export const businessApi = {
  accounts: () => apiRequest<BusinessAccount[]>('/api/business/accounts'),
  campaigns: () => apiRequest<BusinessCampaign[]>('/api/business/campaigns'),
  createCampaign: (businessId: number, name: string, objective: string) =>
    apiRequest<BusinessCampaign>(`/api/business/accounts/${businessId}/campaigns`, {
      method: 'POST', body: JSON.stringify({ name, objective, status: 'Draft' }),
    }),
};
