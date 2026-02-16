/**
 * Listing-related type definitions
 */

// Ad Format Type
export const AdFormatType = {
    Post: "Post",
    Story: "Story",
    Repost: "Repost",
} as const;

export type AdFormatType = (typeof AdFormatType)[keyof typeof AdFormatType];

// Ad Format Names
export const AdFormatNames: Record<string, string> = {
    [AdFormatType.Post]: "Post",
    [AdFormatType.Story]: "Story",
    [AdFormatType.Repost]: "Repost",
};

// Ad Format Interface
export interface AdFormat {
    id?: string;
    formatType: string;
    priceInTon: number;
    durationHours: number;
    terms: string;
}

// Listing Status Type
export const ListingStatus = {
    Draft: "Draft",
    Published: "Published",
    Paused: "Paused",
    Archived: "Archived",
} as const;

export type ListingStatus = (typeof ListingStatus)[keyof typeof ListingStatus];

// Main Listing Interface
export interface Listing {
    id: string;
    channelId: string;
    channelUsername: string;
    channelTitle: string;
    title: string;
    description: string;
    status: string;
    adFormats: AdFormat[];
    subscriberCount: number;
    avgViews: number;
    premiumSubscriberCount?: number;
    language: string;
    isMine: boolean;
    minPrice?: number;
    isOwner?: boolean;
    createdAt?: string;
    updatedAt?: string;
}

// Listing Filter Parameters
export interface ListingFilterParams {
    searchTerm?: string;
    minPrice?: number;
    maxPrice?: number;
    minSubscribers?: number;
    minAvgViews?: number;
    language?: string;
    page?: number;
    pageSize?: number;
}

// Listings API Response
export interface ListingsResponse {
    items: Listing[];
    totalCount: number;
    page: number;
    pageSize: number;
}

// Helper function to get status color
export const getListingStatusColor = (status?: string): string => {
    if (!status) {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--muted-color")
            .trim();
    }

    const statusLower = status.toLowerCase();

    if (statusLower === "published") {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--success-color")
            .trim();
    }
    if (statusLower === "draft") {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--warning-color")
            .trim();
    }
    if (statusLower === "paused") {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--primary-color")
            .trim();
    }
    if (statusLower === "archived") {
        return getComputedStyle(document.documentElement)
            .getPropertyValue("--muted-color")
            .trim();
    }
    return getComputedStyle(document.documentElement).getPropertyValue("--muted-color").trim();
};

// Helper function to validate listing status
export const isValidListingStatus = (status: string): status is ListingStatus => {
    return Object.values(ListingStatus).includes(status as ListingStatus);
};

// Create Listing Request
export interface CreateListingRequest {
    channelId: string;
    title: string;
    description: string;
    adFormats: AdFormat[];
}

// Create Listing Response
export interface CreateListingResponse {
    id: string;
    channelId: string;
    title: string;
    status: string;
}

// Update Listing Request (PUT)
export interface UpdateListingRequest {
    title: string;
    description: string;
    status: string | null;
    adFormats: AdFormat[];
}

// Update Listing Response (PUT)
export interface UpdateListingResponse {
    id: string;
    channelId: string;
    title: string;
    description: string;
    status: string;
    adFormats: AdFormat[];
    updatedAt: string;
}
