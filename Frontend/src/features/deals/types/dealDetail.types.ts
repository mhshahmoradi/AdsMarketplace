export type DealRole = "owner" | "advertiser";

export interface DealEvent {
    id: string;
    fromStatus?: string;
    toStatus?: string;
    eventType?: string;
    createdAt: string;
}

export interface DealChannel {
    id?: string | null;
    username?: string;
    title?: string;
    description?: string;
    status?: string;
    stats?: {
        subscriberCount?: number;
        avgViewsPerPost?: number;
        avgReachPerPost?: number;
        fetchedAt?: string;
    };
}

export interface DealDetail {
    id: string;
    listingId?: string | null;
    channelId?: string | null;
    campaignId: string;
    advertiserUserId: string;
    channelOwnerUserId: string;
    status?: string;
    agreedPriceInTon: number;
    adFormat?: string;
    scheduledPostTime?: string;
    escrowAddress?: string;
    postedAt?: string;
    verifiedAt?: string;
    createdAt: string;
    expiresAt?: string;
    campaign?: {
        id: string;
        title?: string;
        brief?: string;
        budgetInTon?: number;
        status?: string;
        scheduleStart?: string;
        scheduleEnd?: string;
    };
    listing?: {
        id?: string | null;
        title?: string;
        description?: string;
        status?: string;
        channel?: DealChannel;
    } | null;
    channel?: DealChannel | null;
    events?: DealEvent[];
}

export interface DealLocationState {
    role?: DealRole;
}

export interface PaymentCreateResponse {
    paymentId: string;
    invoiceReference: string;
    paymentUrl: string;
    amountInTon: number;
    currency: string;
    expiresAt: string;
}

export interface PaymentStatusResponse {
    paymentId: string;
    dealId: string;
    status: string;
    invoiceReference?: string;
    paymentUrl?: string;
    amountInTon: number;
    currency: string;
    transactionHash?: string;
    paidByAddress?: string;
    actualAmountInTon?: number;
    createdAt: string;
    expiresAt: string;
    confirmedAt?: string;
}

export interface DealCreativeResponse {
    dealId: string;
    status?: string;
    creativeText?: string;
    submittedAt?: string;
    reviewedAt?: string;
    rejectionReason?: string;
}

export type ActionType = "none" | "payment" | "creativeSubmit" | "creativeReview";
export type CreativeReviewDecision = "accept" | "reject";
export type CreativeReviewApiDecision = "accepted" | "rejected";
export type ConfirmationModalType = "creativeReject" | "dealCancel";
export type DealBusyAction = "cancel" | "createPayment" | "pollPayment" | "submitCreative" | null;

export interface ActionDescriptor {
    type: ActionType;
    title: string;
    description: string;
    buttonLabel?: string;
}

export interface ActionFeedback {
    type: "success" | "error";
    message: string;
}

export interface PaymentFeedback {
    type: "success" | "error" | "info";
    message: string;
}
