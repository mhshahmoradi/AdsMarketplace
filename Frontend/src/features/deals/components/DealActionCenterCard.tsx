import { type MouseEvent } from "react";
import { HiArrowPath, HiCheckCircle, HiClock, HiXCircle } from "react-icons/hi2";
import {
    type ActionDescriptor,
    type ActionFeedback,
    type DealBusyAction,
    type PaymentFeedback,
    type PaymentStatusResponse,
} from "../types/dealDetail.types";
import { formatDateTime, formatStatusLabel } from "../utils/dealDetail.utils";

interface DealActionCenterCardProps {
    actionDescriptor: ActionDescriptor;
    actionFeedback: ActionFeedback | null;
    busyAction: DealBusyAction;
    shouldRenderPaymentBlock: boolean;
    dealAgreedPriceInTon: number;
    shouldShowGoProfileForWallet: boolean;
    canCreatePayment: boolean;
    canShowPayButton: boolean;
    isConnectionRestored: boolean;
    paymentUrl?: string;
    latestPaymentStatus: PaymentStatusResponse | null;
    paymentFeedback: PaymentFeedback | null;
    canCancelDeal: boolean;
    onOpenCancelDealModal: () => void;
    onGoToProfileForWallet: () => void;
    onCreatePayment: () => void;
    onPayLinkClick: (event: MouseEvent<HTMLAnchorElement>) => void;
}

const DealActionCenterCard = ({
    actionDescriptor,
    actionFeedback,
    busyAction,
    shouldRenderPaymentBlock,
    dealAgreedPriceInTon,
    shouldShowGoProfileForWallet,
    canCreatePayment,
    canShowPayButton,
    isConnectionRestored,
    paymentUrl,
    latestPaymentStatus,
    paymentFeedback,
    canCancelDeal,
    onOpenCancelDealModal,
    onGoToProfileForWallet,
    onCreatePayment,
    onPayLinkClick,
}: DealActionCenterCardProps) => {
    const shouldShowCreativeHandoffNotice =
        actionDescriptor.type === "creativeSubmit" ||
        actionDescriptor.type === "creativeReview";

    return (
        <div className="action-center animate__animated animate__fadeIn">
            <h2>{actionDescriptor.title}</h2>
            <p>{actionDescriptor.description}</p>

            {actionFeedback && (
                <div className={`action-feedback ${actionFeedback.type}`}>
                    {actionFeedback.type === "success" ? <HiCheckCircle /> : <HiXCircle />}
                    <span>{actionFeedback.message}</span>
                </div>
            )}

            {shouldRenderPaymentBlock && (
                <div className="events-payment-block">
                    <div className="events-payment-text">
                        <strong>Awaiting Payment</strong>
                        <span>
                            Complete {dealAgreedPriceInTon} TON escrow payment to continue.
                        </span>
                    </div>
                    {shouldShowGoProfileForWallet && (
                        <button
                            className="go-profile-action"
                            onClick={onGoToProfileForWallet}
                            disabled={busyAction !== null}
                        >
                            Wallet not connected. Go to Profile
                        </button>
                    )}
                    {canCreatePayment && (
                        <button
                            className="pay-now-action"
                            onClick={onCreatePayment}
                            disabled={
                                busyAction !== null ||
                                !isConnectionRestored ||
                                shouldShowGoProfileForWallet
                            }
                        >
                            {busyAction === "createPayment" ? (
                                <>
                                    <HiArrowPath className="spin" />
                                    Creating Payment...
                                </>
                            ) : !isConnectionRestored ? (
                                "Checking Wallet..."
                            ) : (
                                "Create Payment"
                            )}
                        </button>
                    )}
                    {canShowPayButton && (
                        <a
                            className={`pay-now-action pay-link-action ${
                                busyAction === "pollPayment" ? "disabled" : ""
                            }`}
                            href={paymentUrl || "#"}
                            onClick={onPayLinkClick}
                            aria-disabled={busyAction === "pollPayment"}
                        >
                            {busyAction === "pollPayment" ? (
                                <>
                                    <HiArrowPath className="spin" />
                                    Payment going through...
                                </>
                            ) : (
                                "Pay"
                            )}
                        </a>
                    )}
                    {latestPaymentStatus && (
                        <div className="events-payment-status">
                            <span>Status: {formatStatusLabel(latestPaymentStatus.status)}</span>
                            <span>
                                Expires: {formatDateTime(latestPaymentStatus.expiresAt)}
                            </span>
                            {latestPaymentStatus.confirmedAt && (
                                <span>
                                    Confirmed: {formatDateTime(latestPaymentStatus.confirmedAt)}
                                </span>
                            )}
                        </div>
                    )}
                    {paymentFeedback && (
                        <div className={`events-payment-feedback ${paymentFeedback.type}`}>
                            {paymentFeedback.type === "success" ? (
                                <HiCheckCircle />
                            ) : paymentFeedback.type === "error" ? (
                                <HiXCircle />
                            ) : (
                                <HiClock />
                            )}
                            <span>{paymentFeedback.message}</span>
                        </div>
                    )}
                </div>
            )}

            {shouldShowCreativeHandoffNotice && (
                <>
                    <div className="creative-handoff-note">
                        The Creative Draft and Review workflows take place in the Chat bot, you
                        should already have recived a message containing instructions
                    </div>
                    <div className="creative-handoff-note-muted">
                        You always chat with the counterparty using the <strong>/chat</strong>{" "}
                        command
                    </div>
                </>
            )}

            {actionDescriptor.type === "none" &&
                !shouldRenderPaymentBlock &&
                !shouldShowCreativeHandoffNotice && (
                    <div className="no-action-state">
                        <HiClock />
                        <span>No immediate action for your role at this stage.</span>
                    </div>
                )}

            {canCancelDeal && (
                <div className="cancel-block">
                    <button
                        className="danger-action"
                        onClick={onOpenCancelDealModal}
                        disabled={busyAction !== null}
                    >
                        {busyAction === "cancel" ? (
                            <>
                                <HiArrowPath className="spin" />
                                Cancelling...
                            </>
                        ) : (
                            "Cancel Deal"
                        )}
                    </button>
                </div>
            )}
        </div>
    );
};

export default DealActionCenterCard;
