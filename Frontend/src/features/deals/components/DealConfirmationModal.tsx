import { type ConfirmationModalType, type DealBusyAction } from "../types/dealDetail.types";

interface DealConfirmationModalProps {
    confirmationModalType: ConfirmationModalType;
    confirmationReason: string;
    confirmationReasonError: string | null;
    busyAction: DealBusyAction;
    creativeLoading: boolean;
    onReasonChange: (value: string) => void;
    onClose: () => void;
    onConfirm: () => void;
}

const DealConfirmationModal = ({
    confirmationModalType,
    confirmationReason,
    confirmationReasonError,
    busyAction,
    creativeLoading,
    onReasonChange,
    onClose,
    onConfirm,
}: DealConfirmationModalProps) => {
    return (
        <div className="confirmation-overlay" onClick={onClose}>
            <div className="confirmation-dialog" onClick={(event) => event.stopPropagation()}>
                <h3>
                    {confirmationModalType === "creativeReject"
                        ? "Reject Creative?"
                        : "Cancel Deal?"}
                </h3>
                <p>
                    {confirmationModalType === "creativeReject"
                        ? "Are you sure you want to reject this creative? This action cannot be undone."
                        : "Are you sure you want to cancel this deal? This action cannot be undone."}
                </p>
                <div className="confirmation-field">
                    <label className="confirmation-label" htmlFor="deal-confirmation-reason">
                        {confirmationModalType === "creativeReject"
                            ? "Rejection Reason"
                            : "Cancellation Reason (optional)"}
                    </label>
                    <input
                        id="deal-confirmation-reason"
                        className={`confirmation-input ${
                            confirmationModalType === "creativeReject" &&
                            confirmationReasonError
                                ? "error"
                                : ""
                        }`}
                        type="text"
                        value={confirmationReason}
                        onChange={(event) => onReasonChange(event.target.value)}
                        placeholder={
                            confirmationModalType === "creativeReject"
                                ? "Write the rejection reason..."
                                : "Write cancellation reason..."
                        }
                        disabled={busyAction !== null}
                    />
                    {confirmationModalType === "creativeReject" && confirmationReasonError && (
                        <div className="confirmation-error">{confirmationReasonError}</div>
                    )}
                </div>
                <div className="confirmation-buttons">
                    <button
                        className="cancel-button"
                        onClick={onClose}
                        disabled={busyAction !== null}
                    >
                        Keep
                    </button>
                    <button
                        className="confirm-button"
                        onClick={onConfirm}
                        disabled={
                            busyAction !== null ||
                            (confirmationModalType === "creativeReject" && creativeLoading)
                        }
                    >
                        {confirmationModalType === "dealCancel" && busyAction === "cancel"
                            ? "Cancelling..."
                            : confirmationModalType === "creativeReject" &&
                                busyAction === "submitCreative"
                              ? "Rejecting..."
                              : confirmationModalType === "creativeReject"
                                ? "Reject"
                                : "Cancel Deal"}
                    </button>
                </div>
            </div>
        </div>
    );
};

export default DealConfirmationModal;
