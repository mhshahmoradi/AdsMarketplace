import "../styles/ListingDetail.scss";

import { useCallback, useEffect, useState, type FC } from "react";
import { MdEdit, MdExpandMore, MdExpandLess } from "react-icons/md";

import BottomBar from "@/shared/components/BottomBar";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { requestAPI } from "@/shared/utils/api";
import { getListingStatusColor, type Listing } from "../types/listing";
import { formatDate, formatNumber } from "@/shared/utils/format";
import { getColorFromId } from "@/shared/utils/hash";

const PageListingOwnerDetail: FC = () => {
    const navigate = useNavigate();
    const { listingId } = useParams<{ listingId: string }>();
    const [listing, setListing] = useState<Listing | null>(null);
    const [loading, setLoading] = useState(true);
    const [descriptionExpanded, setDescriptionExpanded] = useState(false);
    const [localStatus, setLocalStatus] = useState("Draft");
    const [isPublishing, setIsPublishing] = useState(false);
    const [isPausing, setIsPausing] = useState(false);

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        if (listing?.channelId) {
            navigate(`/channel/${listing.channelId}`);
        } else {
            navigate("/profile");
        }
    }, [navigate, listing?.channelId]);

    const handleEdit = () => {
        invokeHapticFeedbackImpact("medium");
        navigate(`/listing/${listingId}/edit`);
    };

    const handleStatusChange = useCallback(
        async (newStatus: string) => {
            if (!listing || isPublishing || isPausing) return;

            const isDraftStatus = localStatus.toLowerCase() === "draft";
            const isPublishAction = newStatus === "Published" && isDraftStatus;

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
                    setListing((prevListing) =>
                        prevListing ? { ...prevListing, status: updatedStatus } : prevListing,
                    );
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
        },
        [listing, isPublishing, isPausing, localStatus],
    );

    useEffect(() => {
        const fetchListingDetails = async () => {
            try {
                // Fetch listing details from API - owner view
                const data = await requestAPI(`/listings/${listingId}`, {}, "GET");

                if (data && data !== false && data !== null) {
                    setListing(data);
                    setLocalStatus(data.status || "Draft");
                } else {
                    console.error("Failed to fetch listing details");
                }
                setLoading(false);
            } catch (error) {
                console.error("Error fetching listing details:", error);
                setLoading(false);
            }
        };

        fetchListingDetails();
    }, [listingId]);

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

    if (loading || !listing) {
        return (
            <>
                <div id="container-page-listing-owner-detail">
                    <div className="listing-detail-content">
                        <div className="listing-loading-state animate__animated animate__fadeIn">
                            <h2>Loading listing details...</h2>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    const placeholderColor = getColorFromId(listing.channelId);
    const titleInitials = listing.channelTitle
        ? listing.channelTitle.substring(0, 2).toUpperCase()
        : "CH";
    const listingWithPhoto = listing as Listing & {
        channelPhotoUrl?: string;
        channelPhoto?: string;
        channelAvatarUrl?: string;
        photoUrl?: string;
    };
    const channelAvatar =
        listingWithPhoto.channelPhotoUrl ||
        listingWithPhoto.channelPhoto ||
        listingWithPhoto.channelAvatarUrl ||
        listingWithPhoto.photoUrl;
    const premiumSubscribers = listing.premiumSubscriberCount ?? 0;
    const primaryAdFormat = listing.adFormats?.[0];
    const channelUsername = listing.channelUsername
        ? listing.channelUsername.startsWith("@")
            ? listing.channelUsername
            : `@${listing.channelUsername}`
        : "@channel";
    const listedAtText = listing.createdAt ? formatDate(listing.createdAt) : "N/A";
    const shouldShowDescriptionToggle = (listing.description || "").length > 220;
    const whatWeProvide = {
        type: primaryAdFormat?.formatType || "Post",
        duration: primaryAdFormat?.durationHours ?? 24,
        price: primaryAdFormat?.priceInTon ?? 0,
    };
    const normalizedStatus = localStatus.toLowerCase();
    const isDraft = normalizedStatus === "draft";
    const isPublished = normalizedStatus === "published";
    const isPaused = normalizedStatus === "paused";
    const canPublish = isDraft && !isPublishing;
    const canPauseOrResume = (isPublished || isPaused) && !isPausing;
    const statusToneClass = isDraft
        ? "status-draft"
        : isPublished
          ? "status-published"
          : isPaused
            ? "status-paused"
            : "status-archived";

    return (
        <>
            <div id="container-page-listing-owner-detail">
                <div className="listing-detail-content">
                    <button className="btn-detail-edit" onClick={handleEdit}>
                        <MdEdit />
                    </button>

                    <div className="listing-channel-header animate__animated animate__fadeIn">
                        <div className="listing-header-avatar">
                            {channelAvatar ? (
                                <img
                                    src={channelAvatar}
                                    alt={listing.channelTitle}
                                    loading="lazy"
                                />
                            ) : (
                                <div
                                    className="profile-placeholder"
                                    style={{ backgroundColor: `#${placeholderColor}` }}
                                >
                                    {titleInitials}
                                </div>
                            )}
                        </div>

                        <div className="listing-header-meta">
                            <h1>{listing.channelTitle}</h1>
                            <p className="listing-channel-address">{channelUsername}</p>
                            <p className="listing-listed-at">Listed at {listedAtText}</p>
                        </div>
                    </div>

                    <div className="listing-stats-strip animate__animated animate__fadeIn">
                        <div className="listing-stat-chip">
                            <span className="listing-stat-icon">👥</span>
                            <strong className="listing-stat-value">
                                {formatNumber(listing.subscriberCount || 0)}
                            </strong>
                            <span className="listing-stat-text">Subscribers</span>
                        </div>
                        <div className="listing-stat-chip">
                            <span className="listing-stat-icon">👁️</span>
                            <strong className="listing-stat-value">
                                {formatNumber(listing.avgViews || 0)}
                            </strong>
                            <span className="listing-stat-text">Avg views</span>
                        </div>
                        <div className="listing-stat-chip">
                            <span className="listing-stat-icon">🎁</span>
                            <strong className="listing-stat-value">
                                {formatNumber(premiumSubscribers)}
                            </strong>
                            <span className="listing-stat-text">Premium subs</span>
                        </div>
                    </div>

                    <section className="listing-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Status</span>
                        <div className="section-box listing-owner-status-box">
                            <div className="listing-owner-status-row">
                                <span className="listing-owner-status-key">Current</span>
                                <span
                                    className={`listing-owner-status-value ${statusToneClass}`}
                                >
                                    <span
                                        className="listing-owner-status-dot"
                                        style={{
                                            backgroundColor: getListingStatusColor(localStatus),
                                        }}
                                    />
                                    <span>{localStatus}</span>
                                </span>
                            </div>

                            <div className="listing-owner-action-row">
                                {isDraft ? (
                                    <button
                                        className={`listing-owner-status-btn ${isPublishing ? "loading" : ""}`}
                                        onClick={() => handleStatusChange("Published")}
                                        disabled={!canPublish}
                                    >
                                        {isPublishing ? "Publishing..." : "Publish"}
                                    </button>
                                ) : isPublished || isPaused ? (
                                    <button
                                        className={`listing-owner-status-btn ${isPaused ? "paused" : "published"} ${isPausing ? "loading" : ""}`}
                                        onClick={() =>
                                            handleStatusChange(
                                                isPaused ? "Published" : "Paused",
                                            )
                                        }
                                        disabled={!canPauseOrResume}
                                    >
                                        {isPausing
                                            ? isPaused
                                                ? "Unpausing..."
                                                : "Pausing..."
                                            : isPaused
                                              ? "Unpause"
                                              : "Pause"}
                                    </button>
                                ) : (
                                    <button
                                        className="listing-owner-status-btn archived"
                                        disabled
                                    >
                                        {localStatus}
                                    </button>
                                )}
                            </div>
                        </div>
                    </section>

                    <section className="listing-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Title</span>
                        <div className="section-box">
                            <p className="section-title-text">
                                {listing.title || "Untitled listing"}
                            </p>
                        </div>
                    </section>

                    <section className="listing-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Description</span>
                        <div className="section-box">
                            <div
                                className={`description-content ${descriptionExpanded ? "expanded" : ""}`}
                            >
                                <p>{listing.description || "No description provided."}</p>
                            </div>
                            {shouldShowDescriptionToggle && (
                                <button
                                    className="btn-description-toggle"
                                    onClick={() => {
                                        invokeHapticFeedbackImpact("light");
                                        setDescriptionExpanded(!descriptionExpanded);
                                    }}
                                >
                                    {descriptionExpanded ? (
                                        <>
                                            Show less <MdExpandLess />
                                        </>
                                    ) : (
                                        <>
                                            Show more <MdExpandMore />
                                        </>
                                    )}
                                </button>
                            )}
                        </div>
                    </section>

                    <section className="listing-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">What we provide</span>
                        <div className="provide-card">
                            <span className="provide-icon">📋</span>
                            <p className="provide-type">{whatWeProvide.type}</p>
                            <p className="provide-duration">For {whatWeProvide.duration}h</p>
                            <div className="provide-price">
                                {formatNumber(whatWeProvide.price)} TON
                            </div>
                        </div>
                    </section>

                    <div className="listing-detail-spacer" />
                </div>
            </div>
            <BottomBar />
        </>
    );
};

export default PageListingOwnerDetail;
