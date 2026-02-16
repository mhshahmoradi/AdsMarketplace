import { useState } from "react";
import { type DealEvent } from "../types/dealDetail.types";
import { formatDateTime, formatStatusLabel } from "../utils/dealDetail.utils";

interface DealHistoryCardProps {
    sortedEvents: DealEvent[];
}

const DealHistoryCard = ({ sortedEvents }: DealHistoryCardProps) => {
    const [showPreviousEvents, setShowPreviousEvents] = useState(false);
    const visibleEvents = showPreviousEvents ? sortedEvents : sortedEvents.slice(0, 1);
    const hiddenEventsCount = Math.max(0, sortedEvents.length - 1);

    return (
        <div className="history-sections animate__animated animate__fadeIn">
            <div className="history-card">
                <h3>Status Events</h3>
                {visibleEvents.length > 0 ? (
                    visibleEvents.map((event) => (
                        <div key={event.id} className="history-item">
                            <div className="history-head">
                                <span>{formatDateTime(event.createdAt)}</span>
                                <span className="status">{event.eventType || "Event"}</span>
                            </div>
                            <div className="history-body">
                                <strong>
                                    {formatStatusLabel(event.fromStatus)} →{" "}
                                    {formatStatusLabel(event.toStatus)}
                                </strong>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="empty-history">No status events yet.</div>
                )}
                {hiddenEventsCount > 0 && (
                    <button
                        type="button"
                        className="history-load-action"
                        onClick={() => setShowPreviousEvents((current) => !current)}
                    >
                        {showPreviousEvents
                            ? "Hide previous events"
                            : `Load previous events (${hiddenEventsCount})`}
                    </button>
                )}
            </div>
        </div>
    );
};

export default DealHistoryCard;
