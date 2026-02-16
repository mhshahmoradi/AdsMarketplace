import { memo, useState } from "react";
import { getListingStatusColor, type Listing } from "@/shared/types/listing";
import { requestAPI } from "@/shared/utils/api";
import { invokeHapticFeedbackImpact } from "@/shared/utils/telegram";
import "./ListItem.scss";

interface ChannelListingItemProps {
    listing: Listing;
    onClick: () => void;
    onPublishSuccess?: (listingId: string, newStatus: string) => void;
    className?: string;
}

export const ChannelListingItem = memo(
    ({ listing, onClick, onPublishSuccess, className = "" }: ChannelListingItemProps) => {
        const [isPublishing, setIsPublishing] = useState(false);
        const [isPausing, setIsPausing] = useState(false);
        const [localStatus, setLocalStatus] = useState(listing.status || "Draft");

        try {
            if (!listing) {
                console.error("ChannelListingItem received null/undefined listing");
                return null;
            }

            const handleStatusChange = async (newStatus: string, e: React.MouseEvent) => {
                e.stopPropagation();

                if (isPublishing || isPausing) return;

                const isPublishAction = newStatus === "Published" && isDraft;

                if (isPublishAction) {
                    setIsPublishing(true);
                } else {
                    setIsPausing(true);
                }

                invokeHapticFeedbackImpact("medium");

                try {
                    const response = await requestAPI(
                        `/listings/${listing.id}/status`,
                        { status: newStatus },
                        "PATCH",
                    );

                    if (response && response.id) {
                        const updatedStatus = response.status || newStatus;
                        setLocalStatus(updatedStatus);

                        if (onPublishSuccess) {
                            onPublishSuccess(listing.id, updatedStatus);
                        }

                        invokeHapticFeedbackImpact("heavy");
                    } else {
                        console.error("Status change failed: Invalid response", response);
                        invokeHapticFeedbackImpact("soft");
                    }
                } catch (error) {
                    console.error("Error changing listing status:", error);
                    invokeHapticFeedbackImpact("soft");
                } finally {
                    if (isPublishAction) {
                        setIsPublishing(false);
                    } else {
                        setIsPausing(false);
                    }
                }
            };

            const isDraft = localStatus.toLowerCase() === "draft";
            const isPublished = localStatus.toLowerCase() === "published";
            const isPaused = localStatus.toLowerCase() === "paused";
            const statusToneClass = isDraft
                ? "status-draft"
                : isPublished
                  ? "status-published"
                  : isPaused
                    ? "status-paused"
                    : "status-archived";
            const canPublish = isDraft && !isPublishing;
            const canPause = (isPublished || isPaused) && !isPausing;
            const primaryAdFormat = listing.adFormats?.[0];
            const adFormatType = primaryAdFormat?.formatType || "Post";
            const listingPrice = primaryAdFormat?.priceInTon ?? listing.minPrice ?? 0;
            const listingDuration = primaryAdFormat?.durationHours ?? 24;

            return (
                <div className={`channel-listing-item-compact ${className}`} onClick={onClick}>
                    <div className="channel-listing-card">
                        <div className="channel-listing-head">
                            <div className="channel-listing-main">
                                <h3 className="listing-title">
                                    {listing.title || "Untitled listing"}
                                </h3>
                                {listing.description && (
                                    <p className="listing-description">{listing.description}</p>
                                )}
                            </div>

                            <div className="channel-listing-badges">
                                <span className="channel-listing-format-chip">
                                    <span className="channel-listing-format-icon">📋</span>
                                    <span className="channel-listing-format-text">
                                        {adFormatType}
                                    </span>
                                </span>

                                <span
                                    className={`channel-listing-status-chip ${statusToneClass}`}
                                >
                                    <span
                                        className="channel-listing-status-dot"
                                        style={{
                                            backgroundColor: getListingStatusColor(localStatus),
                                        }}
                                    />
                                    <span className="channel-listing-status-text">
                                        {localStatus}
                                    </span>
                                </span>
                            </div>
                        </div>

                        <div className="channel-listing-meta-row">
                            <span className="channel-listing-meta-item">
                                <span className="channel-listing-meta-icon">💵</span>
                                <span className="channel-listing-meta-value">
                                    {listingPrice} TON
                                </span>
                            </span>
                            <span className="channel-listing-meta-item">
                                <span className="channel-listing-meta-icon">⏱️</span>
                                <span className="channel-listing-meta-value">
                                    {listingDuration}hr
                                </span>
                            </span>
                        </div>

                        <div className="channel-listing-action-wrap">
                            {isDraft ? (
                                <button
                                    className={`btn-publish-compact ${isPublishing ? "publishing" : ""}`}
                                    onClick={(e) => handleStatusChange("Published", e)}
                                    disabled={!canPublish}
                                >
                                    {isPublishing ? (
                                        <span>Publishing...</span>
                                    ) : (
                                        <span>Publish</span>
                                    )}
                                </button>
                            ) : isPublished || isPaused ? (
                                <button
                                    className={`btn-publish-compact ${isPausing ? "pausing" : ""} ${isPaused ? "paused" : "published"}`}
                                    onClick={(e) =>
                                        handleStatusChange(isPaused ? "Published" : "Paused", e)
                                    }
                                    disabled={!canPause}
                                >
                                    {isPausing ? (
                                        <span>{isPaused ? "Resuming..." : "Pausing..."}</span>
                                    ) : isPaused ? (
                                        <span>Unpause</span>
                                    ) : (
                                        <span>Pause</span>
                                    )}
                                </button>
                            ) : (
                                <button className="btn-publish-compact archived" disabled>
                                    <span>✓ {localStatus}</span>
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            );
        } catch (error) {
            console.error("Error rendering ChannelListingItem:", error, listing);
            return null;
        }
    },
);

ChannelListingItem.displayName = "ChannelListingItem";
