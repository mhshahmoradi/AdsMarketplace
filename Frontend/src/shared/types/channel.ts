/**
 * Channel-related type definitions
 */

// Channel Statistics Interface
export interface ChannelStats {
    subscriberCount: number;
    avgViewsPerPost: number;
    avgReachPerPost?: number;
    premiumSubscriberCount?: number;
    avgSharesPerPost?: number;
    avgReactionsPerPost?: number;
    avgViewsPerStory?: number;
    avgSharesPerStory?: number;
    avgReactionsPerStory?: number;
    enabledNotificationsPercent?: number;
    followersCount?: number;
    followersPrevCount?: number;
    primaryLanguage: string;
    languagesGraphJson?: string | Record<string, unknown> | unknown[];
    statsStartDate?: string;
    statsEndDate?: string;
    fetchedAt: string;
}

// Main Channel Interface
export interface Channel {
    id: string;
    tgChannelId: number;
    username: string;
    title: string;
    description: string;
    photoUrl?: string;
    status: string;
    stats?: ChannelStats;
    createdAt: string;
    verified?: boolean;
    tags?: string[];
    category?: string;
    language?: string;
    isOwner?: boolean;
    isAdmin?: boolean;
    // For backward compatibility with different API responses
    subscriberCount?: number;
    avgViewsPerPost?: number;
    image?: string;
}

// Add Channel API Response
export interface AddChannelResponse {
    channelId: string;
    tgChannelId: number;
    username: string;
    title: string;
    status: string;
}

// Verify Channel API Response
export interface VerifyChannelResponse {
    status: string;
    title?: string;
    detail?: string;
}

// Channel Filter Parameters
export interface ChannelFilterParams {
    searchTerm?: string;
    category?: string;
    language?: string;
    minSubscribers?: number;
    maxSubscribers?: number;
    minAvgViews?: number;
    maxAvgViews?: number;
    verified?: boolean;
    page?: number;
    pageSize?: number;
}

// Helper function to check if channel is verified
export const isChannelVerified = (channel: Channel): boolean => {
    return channel.verified === true || channel.status === "Verified";
};

// Helper function to get channel initials for placeholder
export const getChannelInitials = (title: string): string => {
    if (!title) return "CH";
    return title.substring(0, 2).toUpperCase();
};

// Helper function to get first tag from channel tags array
export const getPrimaryTag = (channel: Channel): string | undefined => {
    return channel.tags && channel.tags.length > 0 ? channel.tags[0] : undefined;
};

// Helper function to get all tags or empty array
export const getChannelTags = (channel: Channel): string[] => {
    return channel.tags || [];
};

// Helper function to get subscriber count from channel (handles different response formats)
export const getSubscriberCount = (channel: Channel): number => {
    return channel.stats?.subscriberCount ?? channel.subscriberCount ?? 0;
};

// Helper function to get avg views from channel (handles different response formats)
export const getAvgViews = (channel: Channel): number => {
    return channel.stats?.avgViewsPerPost ?? channel.avgViewsPerPost ?? 0;
};
