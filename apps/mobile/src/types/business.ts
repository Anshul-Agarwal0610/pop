export interface BusinessAccount {
  id: number; ownerUserId: number; name: string; websiteUrl?: string | null; status: string;
  subscriptionPlan: string; subscriptionStatus: string; trialEndsAt?: string | null; createdAt: string;
}
export interface BusinessCampaign {
  id: number; businessId: number; businessName: string; name: string; objective: string; status: string;
  startsAt?: string | null; endsAt?: string | null; createdAt: string; impressions: number; votes: number;
  completions: number; completionRate: number;
}
