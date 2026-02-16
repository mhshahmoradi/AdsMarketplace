interface TemporalItem {
    createdAt?: string | null;
    updatedAt?: string | null;
}

const toTimestamp = (value?: string | null): number => {
    if (!value) return 0;
    const timestamp = new Date(value).getTime();
    return Number.isFinite(timestamp) ? timestamp : 0;
};

export const getRecencyTimestamp = <T extends TemporalItem>(item: T): number => {
    return Math.max(toTimestamp(item.updatedAt), toTimestamp(item.createdAt));
};

export const sortByRecencyDesc = <T extends TemporalItem>(items: T[]): T[] => {
    return [...items].sort((a, b) => getRecencyTimestamp(b) - getRecencyTimestamp(a));
};
