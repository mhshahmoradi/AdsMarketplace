/**
 * Campaign-related type definitions
 */

// Campaign Status Type
export const CampaignStatus = {
    Draft: "Draft",
    Open: "Open",
    InProgress: "In Progress",
    Completed: "Completed",
    Cancelled: "Cancelled",
} as const;

export type CampaignStatus = (typeof CampaignStatus)[keyof typeof CampaignStatus];

// Application Interface
export interface CampaignApplication {
    id: string;
    channelId: string;
    channelTitle: string;
    channelUsername: string;
    proposedPrice: number;
    message: string;
    status: string;
    createdAt: string;
}

// Main Campaign Interface
export interface Campaign {
    id: string;
    title: string;
    brief: string;
    budgetInTon: number;
    minSubscribers: number;
    minAvgViews: number;
    targetLanguages: string;
    scheduleStart: string;
    scheduleEnd: string;
    status: string;
    advertiserUserId: string;
    advertiserUsername: string;
    isMine: boolean;
    applications: CampaignApplication[];
    createdAt: string;
    updatedAt: string;
}

// Create Campaign API Response
export interface CreateCampaignResponse {
    id: string;
    title: string;
    budgetInTon: number;
    status: CampaignStatus;
}

// Update Campaign API Response (PUT)
export interface UpdateCampaignResponse {
    id: string;
    title: string;
    brief: string;
    budgetInTon: number;
    minSubscribers: number;
    minAvgViews: number;
    targetLanguages: string;
    scheduleStart: string;
    scheduleEnd: string;
    status: string;
    updatedAt: string;
}

// Campaign Filter Parameters
export interface CampaignFilterParams {
    searchTerm?: string;
    minBudget?: number;
    maxBudget?: number;
    language?: string;
    status?: CampaignStatus | string;
    minSubscribers?: number;
    minAvgViews?: number;
    page?: number;
    pageSize?: number;
}

// Helper function to get status color
export const getCampaignStatusColor = (status: CampaignStatus | string): string => {
    const statusStr = typeof status === "string" ? status : String(status);
    const statusLower = statusStr.toLowerCase();

    if (statusLower.includes("open") || statusLower.includes("inprogress")) {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--success-color")
            .trim();
    }
    if (statusLower.includes("draft")) {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--warning-color")
            .trim();
    }
    if (statusLower.includes("completed") || statusLower.includes("cancelled")) {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--muted-color")
            .trim();
    }
    return getComputedStyle(document.documentElement).getPropertyValue("--muted-color").trim();
};

// Helper function to validate campaign status
export const isValidCampaignStatus = (status: string): status is CampaignStatus => {
    return Object.values(CampaignStatus).includes(status as CampaignStatus);
};
