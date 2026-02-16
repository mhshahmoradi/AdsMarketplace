import "@/features/requests/styles/Requests.scss";
import "../styles/DealDetail.scss";

import { off, on } from "@tma.js/sdk-react";
import {
    useCallback,
    useEffect,
    useMemo,
    useRef,
    useState,
    type FC,
    type MouseEvent,
} from "react";
import { HiExclamationTriangle } from "react-icons/hi2";
import { useIsConnectionRestored, useTonWallet } from "@tonconnect/ui-react";
import { useLocation, useNavigate, useParams, useSearchParams } from "react-router";
import BottomBar from "@/shared/components/BottomBar";
import { requestAPI } from "@/shared/utils/api";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import DealActionCenterCard from "../components/DealActionCenterCard";
import DealConfirmationModal from "../components/DealConfirmationModal";
import DealHistoryCard from "../components/DealHistoryCard";
import DealStatusLifecycleCard from "../components/DealStatusLifecycleCard";
import DealSummaryCard from "../components/DealSummaryCard";
import {
    type ActionFeedback,
    type ConfirmationModalType,
    type DealBusyAction,
    type DealDetail,
    type DealLocationState,
    type DealRole,
    type PaymentCreateResponse,
    type PaymentFeedback,
    type PaymentStatusResponse,
} from "../types/dealDetail.types";
import {
    STATUS_LIFECYCLE,
    TERMINAL_STATUSES,
    formatDate,
    formatStatusLabel,
    getActionDescriptor,
    getPaymentSessionStorageKey,
    isPaymentSuccessfulStatus,
    isPaymentTerminalStatus,
    normalizeStatus,
    toTonkeeperHttpLink,
} from "../utils/dealDetail.utils";

const PageDealDetail: FC = () => {
    const navigate = useNavigate();
    const tonWallet = useTonWallet();
    const isConnectionRestored = useIsConnectionRestored();
    const { dealId } = useParams<{ dealId: string }>();
    const [searchParams] = useSearchParams();
    const location = useLocation() as { state?: DealLocationState };

    const [loading, setLoading] = useState(true);
    const [deal, setDeal] = useState<DealDetail | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [busyAction, setBusyAction] = useState<DealBusyAction>(null);
    const [actionFeedback, setActionFeedback] = useState<ActionFeedback | null>(null);
    const [paymentFeedback, setPaymentFeedback] = useState<PaymentFeedback | null>(null);
    const [paymentSession, setPaymentSession] = useState<PaymentCreateResponse | null>(null);
    const [latestPaymentStatus, setLatestPaymentStatus] =
        useState<PaymentStatusResponse | null>(null);
    const paymentPollingTimeoutRef = useRef<number | null>(null);
    const paymentPollingRunRef = useRef(0);

    const [confirmationModalType, setConfirmationModalType] =
        useState<ConfirmationModalType | null>(null);
    const [confirmationReason, setConfirmationReason] = useState("");
    const [confirmationReasonError, setConfirmationReasonError] = useState<string | null>(null);

    const roleFromQuery = searchParams.get("role");
    const roleFromState = location.state?.role;

    const activeRole: DealRole =
        roleFromQuery === "owner" || roleFromQuery === "advertiser"
            ? roleFromQuery
            : roleFromState === "owner" || roleFromState === "advertiser"
              ? roleFromState
              : "owner";

    const fetchDeal = useCallback(async () => {
        if (!dealId) {
            setError("Deal ID is missing.");
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);

        try {
            const response = await requestAPI(`/deals/${dealId}`, {}, "GET");
            if (response && response !== false && response !== null) {
                setDeal(response);
            } else {
                setError("Failed to load deal details.");
                setDeal(null);
            }
        } catch (fetchError) {
            console.error("Error fetching deal details:", fetchError);
            setError("Failed to load deal details.");
            setDeal(null);
        } finally {
            setLoading(false);
        }
    }, [dealId]);

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate("/deals");
    }, [navigate]);

    useEffect(() => {
        fetchDeal();
    }, [fetchDeal]);

    useEffect(() => {
        postEvent("web_app_setup_back_button", {
            is_visible: true,
        });

        on("back_button_pressed", onBackButton);

        return () => {
            postEvent("web_app_setup_back_button", {
                is_visible: false,
            });
            off("back_button_pressed", onBackButton);
        };
    }, [onBackButton]);

    useEffect(() => {
        if (!dealId) return;

        const key = getPaymentSessionStorageKey(dealId);
        const raw = sessionStorage.getItem(key);
        if (!raw) return;

        try {
            const parsed = JSON.parse(raw) as PaymentCreateResponse;
            if (parsed && parsed.paymentId && parsed.paymentUrl && parsed.expiresAt) {
                const expiry = new Date(parsed.expiresAt).getTime();
                if (Number.isFinite(expiry) && expiry > Date.now()) {
                    setPaymentSession(parsed);
                } else {
                    sessionStorage.removeItem(key);
                }
            }
        } catch {
            sessionStorage.removeItem(key);
        }
    }, [dealId]);

    const sortedEvents = useMemo(() => {
        const events = deal?.events || [];
        return [...events].sort(
            (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
        );
    }, [deal?.events]);

    const actionDescriptor = useMemo(() => {
        return getActionDescriptor(deal?.status, activeRole);
    }, [activeRole, deal?.status]);

    const normalizedStatusRaw = normalizeStatus(deal?.status);
    const normalizedStatus =
        normalizedStatusRaw === "schedule" ? "scheduled" : normalizedStatusRaw;
    const normalizedLifecycle = STATUS_LIFECYCLE.map((status) => normalizeStatus(status));
    const normalizedTerminal = TERMINAL_STATUSES.map((status) => normalizeStatus(status));
    const isTerminalStatus = normalizedTerminal.includes(normalizedStatus);
    const lifecycleIndex = normalizedLifecycle.indexOf(normalizedStatus);

    const canCancelDeal =
        !!deal && !["released", "refunded", "cancelled", "expired"].includes(normalizedStatus);
    const canManagePayment =
        !!deal &&
        activeRole === "advertiser" &&
        (normalizedStatus === "agreed" || normalizedStatus === "awaitingpayment");
    const canCreatePayment = canManagePayment && !paymentSession;
    const canShowPayButton = canManagePayment && !!paymentSession?.paymentUrl;
    const shouldRenderPaymentBlock =
        !!deal &&
        activeRole === "advertiser" &&
        (canManagePayment ||
            !!paymentSession ||
            busyAction === "pollPayment" ||
            !!paymentFeedback ||
            !!latestPaymentStatus);
    const shouldShowGoProfileForWallet = canCreatePayment && isConnectionRestored && !tonWallet;

    const handleCancelDeal = async (reason = "") => {
        if (!deal) return;

        setBusyAction("cancel");
        setActionFeedback(null);
        invokeHapticFeedbackImpact("medium");

        try {
            const response = await requestAPI(
                `/deals/${deal.id}/cancel`,
                { reason: reason.trim() || undefined },
                "POST",
                true,
            );

            if (response && response.dealId) {
                invokeHapticFeedbackImpact("heavy");
                setActionFeedback({
                    type: "success",
                    message: "Deal cancelled successfully.",
                });
                await fetchDeal();
            } else {
                setActionFeedback({
                    type: "error",
                    message: response?.detail || response?.message || "Failed to cancel deal.",
                });
            }
        } catch (cancelError) {
            console.error("Error cancelling deal:", cancelError);
            setActionFeedback({
                type: "error",
                message: "An error occurred while cancelling the deal.",
            });
        } finally {
            setBusyAction(null);
        }
    };

    const handleOpenCancelDealModal = () => {
        if (!canCancelDeal || busyAction !== null) return;
        invokeHapticFeedbackImpact("light");
        setConfirmationModalType("dealCancel");
        setConfirmationReason("");
        setConfirmationReasonError(null);
    };

    const handleCloseConfirmationModal = () => {
        if (busyAction !== null) return;
        invokeHapticFeedbackImpact("light");
        setConfirmationModalType(null);
        setConfirmationReason("");
        setConfirmationReasonError(null);
    };

    const handleConfirmModalAction = async () => {
        if (!confirmationModalType) return;

        const cancellationReason = confirmationReason.trim();
        setConfirmationModalType(null);
        setConfirmationReason("");
        setConfirmationReasonError(null);
        await handleCancelDeal(cancellationReason);
    };

    const openPaymentLink = (paymentUrl: string) => {
        const link = paymentUrl.trim();
        if (/^https?:\/\//i.test(link)) {
            postEvent("web_app_open_link", { url: link });
            return;
        }

        const tonkeeperHttpLink = toTonkeeperHttpLink(link);
        if (tonkeeperHttpLink) {
            postEvent("web_app_open_link", { url: tonkeeperHttpLink });
            return;
        }

        window.location.assign(link);
    };

    const clearPaymentSession = () => {
        if (dealId) {
            sessionStorage.removeItem(getPaymentSessionStorageKey(dealId));
        }
        setPaymentSession(null);
    };

    const stopPaymentStatusPolling = () => {
        paymentPollingRunRef.current += 1;
        if (paymentPollingTimeoutRef.current !== null) {
            window.clearTimeout(paymentPollingTimeoutRef.current);
            paymentPollingTimeoutRef.current = null;
        }
        setBusyAction((current) => (current === "pollPayment" ? null : current));
    };

    useEffect(() => {
        return () => {
            paymentPollingRunRef.current += 1;
            if (paymentPollingTimeoutRef.current !== null) {
                window.clearTimeout(paymentPollingTimeoutRef.current);
                paymentPollingTimeoutRef.current = null;
            }
        };
    }, []);

    const startPaymentStatusPolling = (paymentId: string) => {
        paymentPollingRunRef.current += 1;
        const runId = paymentPollingRunRef.current;
        if (paymentPollingTimeoutRef.current !== null) {
            window.clearTimeout(paymentPollingTimeoutRef.current);
            paymentPollingTimeoutRef.current = null;
        }

        let initialStatus: string | null = null;

        setBusyAction("pollPayment");
        setPaymentFeedback({
            type: "info",
            message: "Payment going through...",
        });

        const pollStatus = async () => {
            if (paymentPollingRunRef.current !== runId) return;

            try {
                const response = (await requestAPI(
                    `/payments/${paymentId}`,
                    {},
                    "GET",
                )) as PaymentStatusResponse | null;

                if (paymentPollingRunRef.current !== runId) return;

                if (!response || !response.paymentId) {
                    setPaymentFeedback({
                        type: "error",
                        message: "Failed to fetch payment status. Please try again.",
                    });
                    stopPaymentStatusPolling();
                    return;
                }

                setLatestPaymentStatus(response);

                const currentStatus = normalizeStatus(response.status);
                const statusChanged = initialStatus !== null && currentStatus !== initialStatus;
                if (initialStatus === null) {
                    initialStatus = currentStatus;
                }

                if (statusChanged || isPaymentTerminalStatus(currentStatus)) {
                    const isSuccess = isPaymentSuccessfulStatus(currentStatus);
                    setPaymentFeedback({
                        type: isSuccess ? "success" : "error",
                        message: isSuccess
                            ? "Payment successful."
                            : `Payment finished with status ${formatStatusLabel(response.status)}.`,
                    });

                    if (isPaymentTerminalStatus(currentStatus)) {
                        clearPaymentSession();
                    }

                    if (isSuccess) {
                        invokeHapticFeedbackImpact("heavy");
                    } else {
                        invokeHapticFeedbackImpact("light");
                    }

                    stopPaymentStatusPolling();
                    await fetchDeal();
                    return;
                }

                paymentPollingTimeoutRef.current = window.setTimeout(() => {
                    void pollStatus();
                }, 300);
            } catch (statusError) {
                if (paymentPollingRunRef.current !== runId) return;
                console.error("Error checking payment status:", statusError);
                setPaymentFeedback({
                    type: "error",
                    message: "An error occurred while checking payment status.",
                });
                stopPaymentStatusPolling();
            }
        };

        void pollStatus();
    };

    const handleCreatePayment = async () => {
        if (!deal || !canCreatePayment) return;
        if (!isConnectionRestored) return;
        if (!tonWallet) {
            setPaymentFeedback({
                type: "info",
                message: "Connect your wallet in Profile first.",
            });
            return;
        }

        setBusyAction("createPayment");
        setPaymentFeedback(null);
        setLatestPaymentStatus(null);
        invokeHapticFeedbackImpact("medium");

        try {
            const response = (await requestAPI(
                "/payments/create",
                {
                    dealId: deal.id,
                    currency: "TON",
                },
                "POST",
                true,
            )) as PaymentCreateResponse | null;

            if (!response || !response.paymentId || !response.paymentUrl) {
                setPaymentFeedback({
                    type: "error",
                    message: "Failed to create payment. Please try again.",
                });
                return;
            }

            const expectedAmount = deal.agreedPriceInTon;
            const receivedAmount = Number(response.amountInTon);
            if (
                !Number.isFinite(receivedAmount) ||
                Math.abs(receivedAmount - expectedAmount) > 1e-6
            ) {
                setPaymentFeedback({
                    type: "error",
                    message:
                        "Payment amount mismatch. Please refresh and try again or contact support.",
                });
                return;
            }

            setPaymentSession(response);
            sessionStorage.setItem(
                getPaymentSessionStorageKey(deal.id),
                JSON.stringify(response),
            );

            invokeHapticFeedbackImpact("heavy");
            setPaymentFeedback({
                type: "info",
                message: "Payment created. Tap Pay to open the payment link.",
            });
        } catch (paymentError) {
            console.error("Error creating payment:", paymentError);
            setPaymentFeedback({
                type: "error",
                message: "An error occurred while creating payment.",
            });
        } finally {
            setBusyAction(null);
        }
    };

    const handlePayLinkClick = (event: MouseEvent<HTMLAnchorElement>) => {
        if (!paymentSession?.paymentId || !paymentSession.paymentUrl) {
            event.preventDefault();
            return;
        }

        if (busyAction === "pollPayment") {
            event.preventDefault();
            return;
        }

        const expiry = new Date(paymentSession.expiresAt).getTime();
        const isSessionValid = Number.isFinite(expiry) && expiry > Date.now();
        if (!isSessionValid) {
            event.preventDefault();
            clearPaymentSession();
            setPaymentFeedback({
                type: "error",
                message: "Payment link expired. Create a new payment.",
            });
            return;
        }

        event.preventDefault();
        invokeHapticFeedbackImpact("medium");
        openPaymentLink(paymentSession.paymentUrl);
        startPaymentStatusPolling(paymentSession.paymentId);
    };

    const handleGoToProfileForWallet = () => {
        invokeHapticFeedbackImpact("light");
        navigate("/profile");
    };

    if (loading) {
        return (
            <>
                <div id="container-page-deal-detail">
                    <div className="content-wrapper">
                        <div className="loading-state animate__animated animate__fadeIn">
                            <h2>Loading deal details...</h2>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    if (!deal || error) {
        return (
            <>
                <div id="container-page-deal-detail">
                    <div className="content-wrapper">
                        <div className="error-state animate__animated animate__fadeIn">
                            <HiExclamationTriangle />
                            <h2>Deal unavailable</h2>
                            <p>{error || "This deal could not be loaded."}</p>
                            <button
                                className="primary-action"
                                onClick={() => {
                                    invokeHapticFeedbackImpact("light");
                                    fetchDeal();
                                }}
                            >
                                Retry
                            </button>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    const isOwner = activeRole === "owner";
    const isChannelCampaignDeal = deal.listingId == null;
    const isListingCampaignDeal = deal.channelId == null;
    const ownerAssetType =
        isChannelCampaignDeal && !isListingCampaignDeal
            ? "channel"
            : isListingCampaignDeal
              ? "listing"
              : deal.listingId
                ? "listing"
                : "channel";
    const hasListingAsset = ownerAssetType === "listing";
    const channel = deal.channel ?? deal.listing?.channel;
    const campaignTitle = deal.campaign?.title || "Untitled campaign";
    const campaignBrief = deal.campaign?.brief;
    const campaignBudget = deal.campaign?.budgetInTon;
    const listingTitle = deal.listing?.title || "Untitled listing";
    const listingAssetId = deal.listing?.id ?? deal.listingId ?? null;
    const channelTitle = channel?.title;
    const channelAssetId = channel?.id ?? deal.channelId ?? null;
    const channelAssetTitle = channelTitle || "Untitled channel";
    const channelUsername = channel?.username;
    const subscriberCount = channel?.stats?.subscriberCount;
    const campaignAssetId = deal.campaign?.id ?? deal.campaignId;
    const dealBackTarget = `/deals/${deal.id}?role=${activeRole}`;

    return (
        <>
            <div id="container-page-deal-detail">
                <div className="content-wrapper">
                    <div className="page-header animate__animated animate__fadeIn">
                        <h1>Deal Detail</h1>
                        <p>
                            Deal #{deal.id.slice(0, 8)} • {formatDate(deal.createdAt)}
                        </p>
                    </div>

                    <DealSummaryCard
                        isOwner={isOwner}
                        hasListingAsset={hasListingAsset}
                        listingTitle={listingTitle}
                        listingId={listingAssetId}
                        channelAssetTitle={channelAssetTitle}
                        channelId={channelAssetId}
                        channelTitle={channelTitle}
                        channelUsername={channelUsername}
                        subscriberCount={subscriberCount}
                        campaignId={campaignAssetId}
                        campaignTitle={campaignTitle}
                        campaignBrief={campaignBrief}
                        campaignBudget={campaignBudget}
                        backTo={dealBackTarget}
                    />

                    <DealStatusLifecycleCard
                        dealStatus={deal.status}
                        isTerminalStatus={isTerminalStatus}
                        lifecycleIndex={lifecycleIndex}
                    />

                    <DealActionCenterCard
                        actionDescriptor={actionDescriptor}
                        actionFeedback={actionFeedback}
                        busyAction={busyAction}
                        shouldRenderPaymentBlock={shouldRenderPaymentBlock}
                        dealAgreedPriceInTon={deal.agreedPriceInTon}
                        shouldShowGoProfileForWallet={shouldShowGoProfileForWallet}
                        canCreatePayment={canCreatePayment}
                        canShowPayButton={canShowPayButton}
                        isConnectionRestored={isConnectionRestored}
                        paymentUrl={paymentSession?.paymentUrl}
                        latestPaymentStatus={latestPaymentStatus}
                        paymentFeedback={paymentFeedback}
                        canCancelDeal={canCancelDeal}
                        onOpenCancelDealModal={handleOpenCancelDealModal}
                        onGoToProfileForWallet={handleGoToProfileForWallet}
                        onCreatePayment={handleCreatePayment}
                        onPayLinkClick={handlePayLinkClick}
                    />

                    <DealHistoryCard sortedEvents={sortedEvents} />
                </div>
            </div>
            <BottomBar />

            {confirmationModalType && (
                <DealConfirmationModal
                    confirmationModalType={confirmationModalType}
                    confirmationReason={confirmationReason}
                    confirmationReasonError={confirmationReasonError}
                    busyAction={busyAction}
                    creativeLoading={false}
                    onReasonChange={(value) => {
                        setConfirmationReason(value);
                        if (confirmationReasonError) {
                            setConfirmationReasonError(null);
                        }
                    }}
                    onClose={handleCloseConfirmationModal}
                    onConfirm={() => {
                        void handleConfirmModalAction();
                    }}
                />
            )}
        </>
    );
};

export default PageDealDetail;
