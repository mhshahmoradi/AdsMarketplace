import { type ActionDescriptor, type DealRole } from "../types/dealDetail.types";

export const STATUS_LIFECYCLE = [
    "Agreed",
    "AwaitingPayment",
    "Paid",
    "CreativeDraft",
    "CreativeReview",
    "Scheduled",
    "Posted",
    "Verified",
    "Released",
];

export const TERMINAL_STATUSES = ["Refunded", "Cancelled", "Expired"];

export const PAYMENT_SUCCESS_STATUSES = [
    "paid",
    "confirmed",
    "completed",
    "success",
    "succeeded",
];

export const PAYMENT_FAILURE_STATUSES = [
    "failed",
    "failure",
    "expired",
    "cancelled",
    "canceled",
    "refunded",
    "rejected",
];

export const getPaymentSessionStorageKey = (dealId: string) => `deal_payment_session_${dealId}`;

export const normalizeStatus = (status?: string): string => {
    return (status || "").replace(/[\s_-]/g, "").toLowerCase();
};

export const formatStatusLabel = (status?: string): string => {
    if (!status) return "Unknown";
    return status
        .replace(/_/g, " ")
        .replace(/([a-z])([A-Z])/g, "$1 $2")
        .replace(/\s+/g, " ")
        .trim();
};

export const isPaymentSuccessfulStatus = (status?: string): boolean => {
    return PAYMENT_SUCCESS_STATUSES.includes(normalizeStatus(status));
};

export const isPaymentFailureStatus = (status?: string): boolean => {
    return PAYMENT_FAILURE_STATUSES.includes(normalizeStatus(status));
};

export const isPaymentTerminalStatus = (status?: string): boolean => {
    return isPaymentSuccessfulStatus(status) || isPaymentFailureStatus(status);
};

export const toTonkeeperHttpLink = (url: string): string | null => {
    const normalizedPath = url
        .trim()
        .replace(/^(ton|tonkeeper):\/\//i, "")
        .replace(/^\/+/, "");

    if (!normalizedPath || normalizedPath === url.trim()) {
        return null;
    }

    return `https://app.tonkeeper.com/${normalizedPath}`;
};

export const formatDate = (value?: string): string => {
    if (!value) return "N/A";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "N/A";
    return date.toLocaleDateString(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
    });
};

export const formatDateTime = (value?: string): string => {
    if (!value) return "N/A";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "N/A";
    return date.toLocaleString(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
};

export const getStatusColor = (status?: string): string => {
    const normalized = normalizeStatus(status);

    if (
        normalized === "paid" ||
        normalized === "schedule" ||
        normalized === "scheduled" ||
        normalized === "posted" ||
        normalized === "verified" ||
        normalized === "released"
    ) {
        return "approved";
    }

    if (normalized === "cancelled" || normalized === "refunded" || normalized === "expired") {
        return "rejected";
    }

    return "pending";
};

export const getActionDescriptor = (
    status: string | undefined,
    role: DealRole,
): ActionDescriptor => {
    const normalized = normalizeStatus(status);

    if (normalized === "paid") {
        if (role === "advertiser") {
            return {
                type: "creativeSubmit",
                title: "Submit Draft",
                description: "Send your creative text for owner review.",
                buttonLabel: "Submit Draft",
            };
        }
        return {
            type: "none",
            title: "No Action Required",
            description: "Waiting for advertiser to submit the draft.",
        };
    }

    if (normalized === "creativedraft") {
        if (role === "advertiser") {
            return {
                type: "creativeSubmit",
                title: "Submit Creative",
                description: "Submit your creative text for the channel owner review.",
                buttonLabel: "Submit Creative",
            };
        }
        return {
            type: "none",
            title: "No Action Required",
            description: "Waiting for advertiser to submit the creative.",
        };
    }

    if (normalized === "posted") {
        return {
            type: "none",
            title: "No Action Required",
            description: "Waiting for advertiser confirmation.",
        };
    }

    if (normalized === "awaitingpayment") {
        if (role === "advertiser") {
            return {
                type: "payment",
                title: "Complete Payment",
                description: "Complete escrow payment to continue the deal workflow.",
                buttonLabel: "Pay",
            };
        }
        return {
            type: "none",
            title: "No Action Required",
            description: "Waiting for advertiser payment.",
        };
    }

    if (normalized === "agreed") {
        if (role === "advertiser") {
            return {
                type: "payment",
                title: "Complete Payment",
                description: "Deal is agreed. Complete payment to proceed.",
                buttonLabel: "Pay",
            };
        }
        return {
            type: "none",
            title: "No Action Required",
            description: "Deal is agreed. Waiting for payment to proceed.",
        };
    }

    if (normalized === "creativereview") {
        if (role === "owner") {
            return {
                type: "creativeReview",
                title: "Review Creative",
                description:
                    "Review the submitted creative text and choose to accept or reject.",
            };
        }
        return {
            type: "none",
            title: "No Action Required",
            description: "Waiting for the channel owner to review the creative.",
        };
    }

    if (normalized === "released") {
        return {
            type: "none",
            title: "No Action Required",
            description: "Deal completed and funds released.",
        };
    }

    if (normalized === "verified") {
        return {
            type: "none",
            title: "No Action Required",
            description: "Posting is verified. Waiting for release processing.",
        };
    }

    if (normalized === "refunded" || normalized === "cancelled" || normalized === "expired") {
        return {
            type: "none",
            title: "Terminal State",
            description: "This deal is closed and cannot be changed.",
        };
    }

    return {
        type: "none",
        title: "No Action Required",
        description: "No action is currently required for this status.",
    };
};
