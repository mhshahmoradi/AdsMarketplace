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

interface RawLanguageDistribution {
    language: string;
    value: number;
}

export interface ChannelLanguageDistribution {
    language: string;
    value: number;
    percentage: number;
}

const toFiniteNumber = (value: unknown): number | null => {
    if (typeof value === "number" && Number.isFinite(value)) {
        return value;
    }

    if (typeof value === "string") {
        const parsed = Number(value);
        if (Number.isFinite(parsed)) {
            return parsed;
        }
    }

    return null;
};

const normalizeLanguageName = (value: string): string => {
    const trimmed = value.trim();
    if (!trimmed) return "";
    return /^[a-z]{2,3}$/i.test(trimmed) ? trimmed.toUpperCase() : trimmed;
};

const getLanguageFromRecord = (record: Record<string, unknown>): string | null => {
    const possibleName =
        record.language ?? record.lang ?? record.code ?? record.name ?? record.label;
    return typeof possibleName === "string" ? possibleName : null;
};

const getValueFromRecord = (record: Record<string, unknown>): number | null => {
    return (
        toFiniteNumber(record.value) ??
        toFiniteNumber(record.count) ??
        toFiniteNumber(record.percent) ??
        toFiniteNumber(record.percentage) ??
        toFiniteNumber(record.share)
    );
};

const extractLanguageDistributionFromColumns = (
    record: Record<string, unknown>,
): RawLanguageDistribution[] => {
    if (!Array.isArray(record.columns)) return [];

    const names =
        record.names && typeof record.names === "object" && !Array.isArray(record.names)
            ? (record.names as Record<string, unknown>)
            : {};
    const types =
        record.types && typeof record.types === "object" && !Array.isArray(record.types)
            ? (record.types as Record<string, unknown>)
            : {};

    const series: RawLanguageDistribution[] = [];

    record.columns.forEach((column) => {
        if (!Array.isArray(column) || column.length < 2) return;
        const [columnKey, ...values] = column;
        if (typeof columnKey !== "string") return;

        const normalizedColumnKey = columnKey.toLowerCase();
        const columnTypeRaw = types[columnKey];
        const columnType =
            typeof columnTypeRaw === "string" ? columnTypeRaw.toLowerCase() : undefined;

        if (normalizedColumnKey === "x" || columnType === "x") return;

        const sum = values.reduce((total, item) => {
            const value = toFiniteNumber(item);
            if (value === null) return total;
            return total + Math.max(0, value);
        }, 0);

        const labelFromNames = names[columnKey];
        const language =
            typeof labelFromNames === "string" && labelFromNames.trim()
                ? labelFromNames
                : columnKey;

        series.push({
            language,
            value: sum,
        });
    });

    return series;
};

const extractLanguageDistribution = (payload: unknown): RawLanguageDistribution[] => {
    if (!payload) return [];

    if (Array.isArray(payload)) {
        const points: RawLanguageDistribution[] = [];
        payload.forEach((item) => {
            if (!item || typeof item !== "object") return;

            const record = item as Record<string, unknown>;
            const language = getLanguageFromRecord(record);
            const value = getValueFromRecord(record);

            if (!language || value === null) return;
            points.push({
                language,
                value,
            });
        });

        return points;
    }

    if (typeof payload !== "object") return [];

    const record = payload as Record<string, unknown>;
    const seriesFromColumns = extractLanguageDistributionFromColumns(record);
    if (seriesFromColumns.length > 0) {
        return seriesFromColumns;
    }

    const ignoredMapKeys = new Set([
        "title",
        "columns",
        "types",
        "names",
        "colors",
        "hidden",
        "subchart",
        "strokewidth",
        "xtickformatter",
        "xtooltipformatter",
        "xrangeformatter",
        "ytickformatter",
        "ytooltipformatter",
        "tooltipsort",
        "stacked",
    ]);

    const directMapEntries = Object.entries(record)
        .filter(([language]) => !ignoredMapKeys.has(language.toLowerCase()))
        .map(([language, value]) => {
            const parsed = toFiniteNumber(value);
            return parsed === null
                ? null
                : ({
                      language,
                      value: parsed,
                  } as RawLanguageDistribution);
        })
        .filter((item): item is RawLanguageDistribution => item !== null);

    if (directMapEntries.length > 0) {
        return directMapEntries;
    }

    const nestedKeys = ["languages", "data", "items", "distribution", "graph"];
    for (const key of nestedKeys) {
        if (!(key in record)) continue;
        const nested = extractLanguageDistribution(record[key]);
        if (nested.length > 0) {
            return nested;
        }
    }

    return [];
};

export const getChannelLanguageDistribution = (
    channel: Channel,
): ChannelLanguageDistribution[] => {
    const raw = channel.stats?.languagesGraphJson;
    if (!raw) return [];

    let payload: unknown = raw;
    if (typeof raw === "string") {
        try {
            payload = JSON.parse(raw);
        } catch {
            return [];
        }
    }

    const extracted = extractLanguageDistribution(payload);
    if (extracted.length === 0) return [];

    const merged = new Map<string, RawLanguageDistribution>();
    extracted.forEach((item) => {
        const language = normalizeLanguageName(item.language);
        if (!language || item.value < 0) return;

        const key = language.toLowerCase();
        const existing = merged.get(key);
        if (existing) {
            existing.value += item.value;
            return;
        }

        merged.set(key, {
            language,
            value: item.value,
        });
    });

    const normalized = Array.from(merged.values())
        .filter((item) => Number.isFinite(item.value) && item.value > 0)
        .sort((a, b) => b.value - a.value);

    if (normalized.length === 0) return [];

    const total = normalized.reduce((sum, item) => sum + item.value, 0);

    return normalized.map((item) => ({
        ...item,
        percentage:
            total > 0 ? Math.round(((item.value / total) * 100 + Number.EPSILON) * 10) / 10 : 0,
    }));
};
