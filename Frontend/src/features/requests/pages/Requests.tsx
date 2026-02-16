import "../styles/Requests.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { off, on } from "@tma.js/sdk-react";
import { useNavigate } from "react-router";
import { MdArrowForward, MdArrowBack } from "react-icons/md";
import { HiMagnifyingGlass, HiXMark } from "react-icons/hi2";
import { requestAPI } from "@/shared/utils/api";
import { sortByRecencyDesc } from "@/shared/utils/sort";
import RequestsLoadingState from "../components/RequestsLoadingState";

type TabType = "all" | "listings" | "campaigns";

// Listing Application (Listings tab) - advertiser applying to listing
interface ListingApplication {
    id: string;
    status: string;
    proposedPriceInTon: number;
    message?: string;
    createdAt: string;
    listing: {
        id: string;
        title: string;
        channelUsername?: string;
        channelTitle?: string;
    };
    campaign: {
        id: string;
        title: string;
        brief: string;
        budgetInTon: number;
    };
    isSent?: boolean;
}

// Campaign Application (Campaigns tab) - channel applying to campaign
interface CampaignApplication {
    id: string;
    status: string;
    proposedPriceInTon: number;
    message?: string;
    createdAt: string;
    channel: {
        id: string;
        username: string;
        title: string;
        subscriberCount: number;
        avgViews: number;
    };
    campaign: {
        id: string;
        title: string;
        brief: string;
        budgetInTon: number;
    };
    isSent?: boolean;
}

const PageRequests: FC = () => {
    const navigate = useNavigate();
    const [activeTab, setActiveTab] = useState<TabType>("all");
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState("");
    const [listingApplications, setListingApplications] = useState<ListingApplication[]>([]);
    const [applications, setApplications] = useState<CampaignApplication[]>([]);
    const [processingRequestId, setProcessingRequestId] = useState<string | null>(null);
    const [confirmRejectId, setConfirmRejectId] = useState<string | null>(null);
    const [confirmRejectType, setConfirmRejectType] = useState<"listing" | "campaign" | null>(
        null,
    );

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        // Back button is handled by the bottom bar navigation
    }, []);

    useEffect(() => {
        postEvent("web_app_setup_back_button", {
            is_visible: false,
        });

        on("back_button_pressed", onBackButton);

        return () => {
            off("back_button_pressed", onBackButton);
        };
    }, [onBackButton]);

    // Fetch data when filter or search changes
    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const params = new URLSearchParams();
                params.append("PageSize", "100");
                if (searchTerm.trim()) {
                    params.append("Search", searchTerm.trim());
                }

                const fetchListingApplications = async () => {
                    const [sentResponse, receivedResponse] = await Promise.all([
                        requestAPI(
                            `/listing-applications?role=advertiser&${params.toString()}`,
                            {},
                            "GET",
                        ),
                        requestAPI(
                            `/listing-applications?role=owner&${params.toString()}`,
                            {},
                            "GET",
                        ),
                    ]);

                    const sentListingApplications =
                        sentResponse && sentResponse.items ? sentResponse.items : [];
                    const receivedListingApplications =
                        receivedResponse && receivedResponse.items
                            ? receivedResponse.items
                            : [];

                    // Tag listing applications with isSent flag
                    const taggedSent = sentListingApplications.map((d: ListingApplication) => ({
                        ...d,
                        isSent: true,
                    }));
                    const taggedReceived = receivedListingApplications.map(
                        (d: ListingApplication) => ({
                            ...d,
                            isSent: false,
                        }),
                    );

                    // Keep accepted and rejected items visible
                    const allListingApplications = sortByRecencyDesc([
                        ...taggedSent,
                        ...taggedReceived,
                    ]);
                    setListingApplications(allListingApplications);
                };

                const fetchCampaignApplications = async () => {
                    const [sentResponse, receivedResponse] = await Promise.all([
                        requestAPI(
                            `/campaign-applications?role=applicant&${params.toString()}`,
                            {},
                            "GET",
                        ),
                        requestAPI(
                            `/campaign-applications?role=campaign-owner&${params.toString()}`,
                            {},
                            "GET",
                        ),
                    ]);

                    const sentApps =
                        sentResponse && sentResponse.items ? sentResponse.items : [];
                    const receivedApps =
                        receivedResponse && receivedResponse.items
                            ? receivedResponse.items
                            : [];

                    // Tag applications with isSent flag
                    const taggedSent = sentApps.map((a: CampaignApplication) => ({
                        ...a,
                        isSent: true,
                    }));
                    const taggedReceived = receivedApps.map((a: CampaignApplication) => ({
                        ...a,
                        isSent: false,
                    }));

                    // Keep accepted and rejected items visible
                    const allApps = sortByRecencyDesc([...taggedSent, ...taggedReceived]);
                    setApplications(allApps);
                };

                if (activeTab === "listings") {
                    await fetchListingApplications();
                } else if (activeTab === "campaigns") {
                    await fetchCampaignApplications();
                } else {
                    await Promise.all([
                        fetchListingApplications(),
                        fetchCampaignApplications(),
                    ]);
                }
            } catch (error) {
                console.error("Error fetching requests:", error);
                setListingApplications([]);
                setApplications([]);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [activeTab, searchTerm]);

    const handleTabChange = (tab: TabType) => {
        if (tab !== activeTab) {
            invokeHapticFeedbackImpact("light");
            setActiveTab(tab);
        }
    };

    const getPageTitle = () => {
        if (activeTab === "listings") {
            return "Listing Requests";
        }
        if (activeTab === "campaigns") {
            return "Campaign Requests";
        }
        return "All Requests";
    };

    const getPageDescription = () => {
        if (activeTab === "listings") {
            return "Channels applying to buy ad placements on listings";
        }
        if (activeTab === "campaigns") {
            return "Channels applying to campaigns";
        }
        return "All listing and campaign requests";
    };

    const getStatusColor = (status: string): string => {
        const statusLower = status.toLowerCase();
        if (statusLower.includes("pending") || statusLower.includes("negotiating")) {
            return "pending";
        }
        if (
            statusLower.includes("approved") ||
            statusLower.includes("accepted") ||
            statusLower.includes("completed") ||
            statusLower.includes("posted")
        ) {
            return "approved";
        }
        if (
            statusLower.includes("rejected") ||
            statusLower.includes("cancelled") ||
            statusLower.includes("expired")
        ) {
            return "rejected";
        }
        return "pending";
    };

    const getStatusLabel = (status: string): string => {
        const statusLower = status.toLowerCase();
        return statusLower.slice(0, 1).toUpperCase() + statusLower.slice(1);
    };

    const formatDate = (dateString: string): string => {
        const date = new Date(dateString);
        return date.toLocaleDateString(undefined, {
            month: "short",
            day: "numeric",
            year: "numeric",
        });
    };

    const handleAcceptRequest = async (
        e: React.MouseEvent,
        applicationId: string,
        isListingRequest: boolean,
    ) => {
        e.stopPropagation(); // Prevent card click
        invokeHapticFeedbackImpact("medium");

        setProcessingRequestId(applicationId);

        try {
            const endpoint = `/applications/${applicationId}/review`;

            const requestBody = {
                decision: "Accepted",
            };

            const response = await requestAPI(endpoint, requestBody, "PATCH", true);

            if (response && (response.applicationId || response.id)) {
                invokeHapticFeedbackImpact("heavy");

                // Update the local state to reflect the accepted status
                if (isListingRequest) {
                    setListingApplications((prevApps) =>
                        sortByRecencyDesc(
                            prevApps.map((app) =>
                                app.id === applicationId
                                    ? {
                                          ...app,
                                          status: response.status || "Accepted",
                                      }
                                    : app,
                            ),
                        ),
                    );
                } else {
                    setApplications((prevApps) =>
                        sortByRecencyDesc(
                            prevApps.map((app) =>
                                app.id === applicationId
                                    ? {
                                          ...app,
                                          status: response.status || "Accepted",
                                      }
                                    : app,
                            ),
                        ),
                    );
                }
            } else {
                invokeHapticFeedbackImpact("soft");
                console.error("Failed to accept request");
            }
        } catch (error) {
            console.error("Error accepting request:", error);
            invokeHapticFeedbackImpact("soft");
        } finally {
            setProcessingRequestId(null);
        }
    };

    const handleRejectClick = (
        e: React.MouseEvent,
        applicationId: string,
        isListingRequest: boolean,
    ) => {
        e.stopPropagation(); // Prevent card click
        invokeHapticFeedbackImpact("light");
        setConfirmRejectId(applicationId);
        setConfirmRejectType(isListingRequest ? "listing" : "campaign");
    };

    const handleConfirmReject = async () => {
        if (!confirmRejectId || !confirmRejectType) return;

        invokeHapticFeedbackImpact("medium");
        setProcessingRequestId(confirmRejectId);

        try {
            const endpoint = `/applications/${confirmRejectId}/review`;

            const requestBody = {
                decision: "Rejected",
            };

            const response = await requestAPI(endpoint, requestBody, "PATCH", true);

            if (response && (response.applicationId || response.id)) {
                invokeHapticFeedbackImpact("heavy");

                // Update the local state to reflect the rejected status
                if (confirmRejectType === "listing") {
                    setListingApplications((prevApps) =>
                        sortByRecencyDesc(
                            prevApps.map((app) =>
                                app.id === confirmRejectId
                                    ? {
                                          ...app,
                                          status: response.status || "Rejected",
                                      }
                                    : app,
                            ),
                        ),
                    );
                } else {
                    setApplications((prevApps) =>
                        sortByRecencyDesc(
                            prevApps.map((app) =>
                                app.id === confirmRejectId
                                    ? {
                                          ...app,
                                          status: response.status || "Rejected",
                                      }
                                    : app,
                            ),
                        ),
                    );
                }

                // Close confirmation
                setConfirmRejectId(null);
                setConfirmRejectType(null);
            } else {
                invokeHapticFeedbackImpact("soft");
                console.error("Failed to reject request");
            }
        } catch (error) {
            console.error("Error rejecting request:", error);
            invokeHapticFeedbackImpact("soft");
        } finally {
            setProcessingRequestId(null);
        }
    };

    const handleCancelReject = () => {
        invokeHapticFeedbackImpact("light");
        setConfirmRejectId(null);
        setConfirmRejectType(null);
    };

    const handleListingApplicationClick = (application: ListingApplication) => {
        invokeHapticFeedbackImpact("light");
        if (application.isSent) {
            navigate(`/listing/${application.listing.id}/detail`);
            return;
        }

        navigate(`/campaign/${application.campaign.id}/detail`);
    };

    const handleApplicationClick = (application: CampaignApplication) => {
        invokeHapticFeedbackImpact("light");
        if (application.isSent) {
            navigate(`/campaign/${application.campaign.id}/detail`);
            return;
        }

        navigate(`/channel/${application.channel.id}/detail`);
    };

    const renderListingApplicationCard = (application: ListingApplication, keyPrefix = "") => {
        const isSent = application.isSent === true;
        const isAccepted = application.status?.toLowerCase() === "accepted";
        const isRejected = application.status?.toLowerCase() === "rejected";
        const hasOverlay = isRejected;

        return (
            <div
                key={`${keyPrefix}${application.id}`}
                className={`request-card ${hasOverlay ? "has-overlay" : ""} ${isAccepted ? "is-accepted" : ""}`}
                onClick={() => !hasOverlay && handleListingApplicationClick(application)}
            >
                {/* Card Overlay for Rejected */}
                {hasOverlay && (
                    <div className="card-overlay">
                        <div className="card-overlay-content">
                            <>
                                <div className="overlay-icon rejected">✕</div>
                                <h4>Request Rejected</h4>
                                <p>This request was rejected</p>
                            </>
                        </div>
                    </div>
                )}
                {/* Left Asset - User's asset */}
                <div className="asset-container user-asset">
                    {isSent ? (
                        <>
                            <div className="asset-badge">🎯</div>
                            <div className="asset-info">
                                <div className="asset-label">Your Campaign</div>
                                <div className="asset-title">{application.campaign.title}</div>
                                {application.campaign.brief && (
                                    <div className="asset-meta">
                                        {application.campaign.brief}
                                    </div>
                                )}
                                <div className="asset-meta">
                                    {application.campaign.budgetInTon} TON budget
                                </div>
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="asset-badge">📋</div>
                            <div className="asset-info">
                                <div className="asset-label">Your Listing</div>
                                <div className="asset-title">{application.listing.title}</div>
                                {application.listing.channelTitle && (
                                    <div className="asset-meta">
                                        {application.listing.channelTitle}
                                    </div>
                                )}
                                {application.message && (
                                    <div className="asset-meta">{application.message}</div>
                                )}
                            </div>
                        </>
                    )}
                </div>

                {/* Arrow & Status (Center) */}
                <div className="request-center">
                    <div className="arrow-container">
                        {isSent ? (
                            <MdArrowForward className="arrow-icon sent" />
                        ) : (
                            <MdArrowBack className="arrow-icon received" />
                        )}
                    </div>
                    <div className={`status-badge ${getStatusColor(application.status)}`}>
                        {getStatusLabel(application.status)}
                    </div>
                    <div className="price-info">{application.proposedPriceInTon} TON</div>
                </div>

                {/* Right Asset - Other party's asset */}
                <div className="asset-container other-asset">
                    {isSent ? (
                        <>
                            <div className="asset-badge">📋</div>
                            <div className="asset-info">
                                <div className="asset-label">Their Listing</div>
                                <div className="asset-title">{application.listing.title}</div>
                                {application.listing.channelTitle && (
                                    <div className="asset-meta">
                                        {application.listing.channelTitle}
                                    </div>
                                )}
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="asset-badge">🎯</div>
                            <div className="asset-info">
                                <div className="asset-label">Their Campaign</div>
                                <div className="asset-title">{application.campaign.title}</div>
                                {application.campaign.brief && (
                                    <div className="asset-meta">
                                        {application.campaign.brief}
                                    </div>
                                )}
                                <div className="asset-meta">
                                    {application.campaign.budgetInTon} TON budget
                                </div>
                            </div>
                        </>
                    )}
                </div>

                {/* Footer Info */}
                <div className="request-footer">
                    <div className="footer-left">
                        <div className="footer-date">{formatDate(application.createdAt)}</div>
                    </div>
                    {isAccepted && (
                        <div className="footer-right">
                            <div className="continue-deal-hint">
                                Continue this deal in the deals page
                            </div>
                        </div>
                    )}
                    {!isSent && !isAccepted && !isRejected && (
                        <div className="action-buttons">
                            <button
                                className="reject-button"
                                onClick={(e) => handleRejectClick(e, application.id, true)}
                                disabled={processingRequestId === application.id}
                            >
                                Reject
                            </button>
                            <button
                                className="accept-button"
                                onClick={(e) => handleAcceptRequest(e, application.id, true)}
                                disabled={processingRequestId === application.id}
                            >
                                {processingRequestId === application.id
                                    ? "Processing..."
                                    : "Accept"}
                            </button>
                        </div>
                    )}
                </div>
            </div>
        );
    };

    const renderApplicationCard = (application: CampaignApplication, keyPrefix = "") => {
        const isSent = application.isSent === true;
        const isAccepted = application.status?.toLowerCase() === "accepted";
        const isRejected = application.status?.toLowerCase() === "rejected";
        const hasOverlay = isRejected;

        return (
            <div
                key={`${keyPrefix}${application.id}`}
                className={`request-card ${hasOverlay ? "has-overlay" : ""} ${isAccepted ? "is-accepted" : ""}`}
                onClick={() => !hasOverlay && handleApplicationClick(application)}
            >
                {/* Card Overlay for Rejected */}
                {hasOverlay && (
                    <div className="card-overlay">
                        <div className="card-overlay-content">
                            <>
                                <div className="overlay-icon rejected">✕</div>
                                <h4>Request Rejected</h4>
                                <p>This request was rejected</p>
                            </>
                        </div>
                    </div>
                )}
                {/* Left Asset - User's asset */}
                <div className="asset-container user-asset">
                    {isSent ? (
                        <>
                            <div className="asset-badge">📺</div>
                            <div className="asset-info">
                                <div className="asset-label">Your Channel</div>
                                <div className="asset-title">{application.channel.title}</div>
                                <div className="asset-meta">
                                    @{application.channel.username}
                                </div>
                                <div className="asset-meta">
                                    {application.channel.subscriberCount.toLocaleString()}{" "}
                                    subscribers
                                </div>
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="asset-badge">🎯</div>
                            <div className="asset-info">
                                <div className="asset-label">Your Campaign</div>
                                <div className="asset-title">{application.campaign.title}</div>
                                {application.campaign.brief && (
                                    <div className="asset-meta">
                                        {application.campaign.brief}
                                    </div>
                                )}
                                <div className="asset-meta">
                                    {application.campaign.budgetInTon} TON budget
                                </div>
                            </div>
                        </>
                    )}
                </div>

                {/* Arrow & Status (Center) */}
                <div className="request-center">
                    <div className="arrow-container">
                        {isSent ? (
                            <MdArrowForward className="arrow-icon sent" />
                        ) : (
                            <MdArrowBack className="arrow-icon received" />
                        )}
                    </div>
                    <div className={`status-badge ${getStatusColor(application.status)}`}>
                        {getStatusLabel(application.status)}
                    </div>
                    <div className="price-info">{application.proposedPriceInTon} TON</div>
                </div>

                {/* Right Asset - Other party's asset */}
                <div className="asset-container other-asset">
                    {isSent ? (
                        <>
                            <div className="asset-badge">🎯</div>
                            <div className="asset-info">
                                <div className="asset-label">Their Campaign</div>
                                <div className="asset-title">{application.campaign.title}</div>
                                {application.campaign.brief && (
                                    <div className="asset-meta">
                                        {application.campaign.brief}
                                    </div>
                                )}
                                <div className="asset-meta">
                                    {application.campaign.budgetInTon} TON budget
                                </div>
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="asset-badge">📺</div>
                            <div className="asset-info">
                                <div className="asset-label">Their Channel</div>
                                <div className="asset-title">{application.channel.title}</div>
                                <div className="asset-meta">
                                    @{application.channel.username}
                                </div>
                                <div className="asset-meta">
                                    {application.channel.subscriberCount.toLocaleString()}{" "}
                                    subscribers
                                </div>
                            </div>
                        </>
                    )}
                </div>

                {/* Footer Info */}
                <div className="request-footer">
                    <div className="footer-left">
                        <div className="footer-date">{formatDate(application.createdAt)}</div>
                    </div>
                    {isAccepted && (
                        <div className="footer-right">
                            <div className="continue-deal-hint">
                                Continue this deal in the deals page
                            </div>
                        </div>
                    )}
                    {!isSent && !isAccepted && !isRejected && (
                        <div className="action-buttons">
                            <button
                                className="reject-button"
                                onClick={(e) => handleRejectClick(e, application.id, false)}
                                disabled={processingRequestId === application.id}
                            >
                                Reject
                            </button>
                            <button
                                className="accept-button"
                                onClick={(e) => handleAcceptRequest(e, application.id, false)}
                                disabled={processingRequestId === application.id}
                            >
                                {processingRequestId === application.id
                                    ? "Processing..."
                                    : "Accept"}
                            </button>
                        </div>
                    )}
                </div>
            </div>
        );
    };

    const renderRequestsList = () => {
        if (activeTab === "all") {
            const allRequests = sortByRecencyDesc([
                ...listingApplications.map((application) => ({
                    type: "listing" as const,
                    application,
                    createdAt: application.createdAt,
                })),
                ...applications.map((application) => ({
                    type: "campaign" as const,
                    application,
                    createdAt: application.createdAt,
                })),
            ]);

            if (allRequests.length === 0) {
                return (
                    <div className="requests-list">
                        <div className="empty-state">
                            <div className="empty-icon">📋</div>
                            <h3>No requests</h3>
                            <p>
                                {searchTerm
                                    ? "No requests match your search."
                                    : "No listing or campaign requests yet."}
                            </p>
                        </div>
                    </div>
                );
            }

            return (
                <div className="requests-list">
                    {allRequests.map((item) =>
                        item.type === "listing"
                            ? renderListingApplicationCard(item.application, "listing-")
                            : renderApplicationCard(item.application, "campaign-"),
                    )}
                </div>
            );
        }

        if (activeTab === "listings") {
            if (listingApplications.length === 0) {
                return (
                    <div className="requests-list">
                        <div className="empty-state">
                            <div className="empty-icon">📋</div>
                            <h3>No listing requests</h3>
                            <p>
                                {searchTerm
                                    ? "No requests match your search."
                                    : "No channels have applied to listings yet."}
                            </p>
                        </div>
                    </div>
                );
            }

            return (
                <div className="requests-list">
                    {listingApplications.map((application) =>
                        renderListingApplicationCard(application),
                    )}
                </div>
            );
        } else {
            if (applications.length === 0) {
                return (
                    <div className="requests-list">
                        <div className="empty-state">
                            <div className="empty-icon">📋</div>
                            <h3>No campaign requests</h3>
                            <p>
                                {searchTerm
                                    ? "No requests match your search."
                                    : "No channels have applied to campaigns yet."}
                            </p>
                        </div>
                    </div>
                );
            }

            return (
                <div className="requests-list">
                    {applications.map((application) => renderApplicationCard(application))}
                </div>
            );
        }
    };

    return (
        <>
            <div id="container-page-requests">
                <div className="content-wrapper">
                    {/* Page Header */}
                    <div className="page-header animate__animated animate__fadeIn">
                        <h1>Requests</h1>
                        <p>Manage your applications and proposals</p>
                    </div>

                    {/* Search Bar */}
                    <div className="search-container animate__animated animate__fadeIn">
                        <div className="search-input-wrapper">
                            <HiMagnifyingGlass className="search-icon" />
                            <input
                                type="text"
                                className="search-input"
                                placeholder="Search requests..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                            {searchTerm && (
                                <button
                                    className="clear-search-button"
                                    onClick={() => setSearchTerm("")}
                                >
                                    <HiXMark />
                                </button>
                            )}
                        </div>
                    </div>

                    {/* Toggle - All/Listings/Campaigns */}
                    <div className="toggle-container primary-toggle animate__animated animate__fadeIn">
                        <button
                            className={`toggle-button ${activeTab === "all" ? "active" : ""}`}
                            onClick={() => handleTabChange("all")}
                        >
                            All
                        </button>
                        <button
                            className={`toggle-button ${activeTab === "listings" ? "active" : ""}`}
                            onClick={() => handleTabChange("listings")}
                        >
                            Listings
                        </button>
                        <button
                            className={`toggle-button ${activeTab === "campaigns" ? "active" : ""}`}
                            onClick={() => handleTabChange("campaigns")}
                        >
                            Campaigns
                        </button>
                    </div>

                    {/* Current View Title */}
                    <div className="section-title animate__animated animate__fadeIn">
                        <h2>{getPageTitle()}</h2>
                        <p className="section-description">{getPageDescription()}</p>
                    </div>

                    {/* Requests List */}
                    {loading ? (
                        <div className="animate__animated animate__fadeIn">
                            <RequestsLoadingState count={5} />
                        </div>
                    ) : (
                        <div className="animate__animated animate__fadeIn">
                            {renderRequestsList()}
                        </div>
                    )}
                </div>
            </div>
            <BottomBar />

            {/* Confirmation Dialog for Reject */}
            {confirmRejectId && (
                <div className="confirmation-overlay" onClick={handleCancelReject}>
                    <div className="confirmation-dialog" onClick={(e) => e.stopPropagation()}>
                        <h3>Reject Request?</h3>
                        <p>
                            Are you sure you want to reject this request? This action cannot be
                            undone.
                        </p>
                        <div className="confirmation-buttons">
                            <button className="cancel-button" onClick={handleCancelReject}>
                                Cancel
                            </button>
                            <button className="confirm-button" onClick={handleConfirmReject}>
                                Reject
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
};

export default PageRequests;
