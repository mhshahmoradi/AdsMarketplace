export const formatNumber = (num: number | null | undefined): string => {
    if (num == null || typeof num !== "number" || isNaN(num)) return "0";
    if (num >= 1000000) return (num / 1000000).toFixed(1) + "M";
    if (num >= 1000) return (num / 1000).toFixed(1) + "K";
    return Math.floor(num).toString();
};

const parseValidDate = (dateString: string): Date | null => {
    const date = new Date(dateString);
    return Number.isNaN(date.getTime()) ? null : date;
};

export const formatDate = (dateString: string): string => {
    const date = parseValidDate(dateString);
    if (!date) {
        return "N/A";
    }

    return date.toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
    });
};

export const formatDateTime = (dateString: string): string => {
    const date = parseValidDate(dateString);
    if (!date) {
        return "N/A";
    }

    return date.toLocaleString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "numeric",
        minute: "2-digit",
        hour12: true,
    });
};

export const normalizeUsername = (username: string): string => {
    if (!username) return "";
    return username.startsWith("@") ? username : "@" + username;
};
export const cleanUsername = (username: string): string => {
    if (!username) return "";
    return username.startsWith("@") ? username.slice(1) : username;
};
