import "@/features/requests/styles/Requests.scss";
import "../styles/Deals.scss";

import { off, on } from "@tma.js/sdk-react";
import { useCallback, useEffect, useMemo, useState, type FC } from "react";
import { HiMagnifyingGlass, HiXMark } from "react-icons/hi2";

import BottomBar from "@/shared/components/BottomBar";
import { useNavigate } from "react-router";
import { requestAPI } from "@/shared/utils/api";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import RequestsLoadingState from "@/features/requests/components/RequestsLoadingState";
import { getStatusColor, formatStatusLabel, normalizeStatus } from "../utils/dealDetail.utils";

type DealRole = "owner" | "advertiser";
type RoleFilter = "all" | DealRole;

interface DealChannel {
    id?: string | null;
    username?: string;
    title?: string;
    stats?: {
        subscriberCount?: number;
    };
}

interface Deal {
    id: string;
    status: string;
    agreedPriceInTon: number;
    scheduledPostTime?: string;
    postedAt?: string;
    createdAt: string;
    listingId?: string | null;
    channelId?: string | null;
    campaign?: {
        id: string;
        title: string;
        brief?: string;
        budgetInTon?: number;
    };
    listing?: {
        id?: string | null;
        title: string;
        channel?: DealChannel;
    } | null;
    channel?: DealChannel | null;
    role?: DealRole;
}

const PageDeals: FC = () => {
    const navigate = useNavigate();
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState("");
    const [activeRoleFilter, setActiveRoleFilter] = useState<RoleFilter>("all");
    const [deals, setDeals] = useState<Deal[]>([]);

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

    useEffect(() => {
        const fetchDeals = async () => {
            setLoading(true);
            try {
                const params = new URLSearchParams();
                params.append("PageSize", "100");

                if (searchTerm.trim()) {
                    params.append("Search", searchTerm.trim());
                }

                const query = params.toString();
                const [ownerResponse, advertiserResponse] = await Promise.all([
                    requestAPI(`/deals?role=owner&${query}`, {}, "GET"),
                    requestAPI(`/deals?role=advertiser&${query}`, {}, "GET"),
                ]);

                const ownerDeals = Array.isArray(ownerResponse?.items)
                    ? ownerResponse.items
                    : [];
                const advertiserDeals = Array.isArray(advertiserResponse?.items)
                    ? advertiserResponse.items
                    : [];

                const taggedOwnerDeals = ownerDeals.map((deal: Deal) => ({
                    ...deal,
                    role: "owner" as const,
                }));
                const taggedAdvertiserDeals = advertiserDeals.map((deal: Deal) => ({
                    ...deal,
                    role: "advertiser" as const,
                }));

                const needsUserAction = (deal: Deal): boolean => {
                    const normalizedStatus = normalizeStatus(deal.status);

                    if (deal.role === "advertiser") {
                        return (
                            normalizedStatus === "agreed" ||
                            normalizedStatus === "awaitingpayment" ||
                            normalizedStatus === "paid" ||
                            normalizedStatus === "creativedraft"
                        );
                    }

                    if (deal.role === "owner") {
                        return normalizedStatus === "creativereview";
                    }

                    return false;
                };

                const getCreatedAtTime = (deal: Deal): number => {
                    return new Date(deal.createdAt ?? 0).getTime();
                };

                const allDeals = [...taggedOwnerDeals, ...taggedAdvertiserDeals].sort(
                    (a, b) => {
                        const actionRankA = needsUserAction(a) ? 0 : 1;
                        const actionRankB = needsUserAction(b) ? 0 : 1;
                        if (actionRankA !== actionRankB) {
                            return actionRankA - actionRankB;
                        }

                        return getCreatedAtTime(b) - getCreatedAtTime(a);
                    },
                );

                setDeals(allDeals);
            } catch (error) {
                console.error("Error fetching deals:", error);
                setDeals([]);
            } finally {
                setLoading(false);
            }
        };

        fetchDeals();
    }, [searchTerm]);

    const filteredDeals = useMemo(() => {
        if (activeRoleFilter === "all") {
            return deals;
        }
        return deals.filter((deal) => deal.role === activeRoleFilter);
    }, [activeRoleFilter, deals]);

    const formatDate = (dateString?: string): string => {
        if (!dateString) {
            return "Unknown date";
        }
        const date = new Date(dateString);
        return date.toLocaleDateString(undefined, {
            month: "short",
            day: "numeric",
            year: "numeric",
        });
    };

    const getDealEmoji = (status?: string): string => {
        const statusLower = status?.toLowerCase() || "";

        // Check if terminated
        if (statusLower.includes("cancel")) {
            return "👎"; // Dislike for cancelled
        } else if (
            statusLower.includes("completed") ||
            statusLower.includes("terminated") ||
            statusLower.includes("finished")
        ) {
            return "👍"; // Like for other terminations
        }

        // Not terminated - handshake
        return "🤝";
    };

    const handleDealClick = useCallback(
        (deal: Deal) => {
            invokeHapticFeedbackImpact("light");

            const role: DealRole = deal.role === "owner" ? "owner" : "advertiser";
            navigate(`/deals/${deal.id}?role=${role}`, { state: { role } });
        },
        [navigate],
    );

    const renderDealCard = (deal: Deal) => {
        const isOwner = deal.role === "owner";
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
        const channelTitle = channel?.title || "Untitled channel";
        const channelUsername = channel?.username;
        const subscriberCount = channel?.stats?.subscriberCount;
        const roleLabel = isOwner ? "Owner" : "Advertiser";

        return (
            <div
                key={`${deal.role}-${deal.id}`}
                className="request-card read-only"
                onClick={() => handleDealClick(deal)}
            >
                <div className="asset-container user-asset">
                    {isOwner ? (
                        <>
                            <div className="asset-badge">{hasListingAsset ? "📋" : "📺"}</div>
                            <div className="asset-info">
                                <div className="asset-label">
                                    {hasListingAsset ? "Your Listing" : "Your Channel"}
                                </div>
                                <div className="asset-title">
                                    {hasListingAsset ? listingTitle : channelTitle}
                                </div>
                                {hasListingAsset && channel?.title && (
                                    <div className="asset-meta">{channelTitle}</div>
                                )}
                                {channelUsername && (
                                    <div className="asset-meta">@{channelUsername}</div>
                                )}
                                {!hasListingAsset && typeof subscriberCount === "number" && (
                                    <div className="asset-meta">
                                        {subscriberCount.toLocaleString()} subscribers
                                    </div>
                                )}
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="asset-badge">🎯</div>
                            <div className="asset-info">
                                <div className="asset-label">Your Campaign</div>
                                <div className="asset-title">{campaignTitle}</div>
                                {campaignBrief && (
                                    <div className="asset-meta">{campaignBrief}</div>
                                )}
                                {campaignBudget !== undefined && (
                                    <div className="asset-meta">
                                        {campaignBudget} TON budget
                                    </div>
                                )}
                            </div>
                        </>
                    )}
                </div>

                <div className="request-center">
                    <div className="deal-emoji-container">{getDealEmoji(deal.status)}</div>
                    <div className={`status-badge ${getStatusColor(deal.status)}`}>
                        {formatStatusLabel(deal.status)}
                    </div>
                    <div className="price-info">{deal.agreedPriceInTon} TON</div>
                </div>

                <div className="asset-container other-asset">
                    {isOwner ? (
                        <>
                            <div className="asset-badge">🎯</div>
                            <div className="asset-info">
                                <div className="asset-label">Their Campaign</div>
                                <div className="asset-title">{campaignTitle}</div>
                                {campaignBrief && (
                                    <div className="asset-meta">{campaignBrief}</div>
                                )}
                                {campaignBudget !== undefined && (
                                    <div className="asset-meta">
                                        {campaignBudget} TON budget
                                    </div>
                                )}
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="asset-badge">{hasListingAsset ? "📋" : "📺"}</div>
                            <div className="asset-info">
                                <div className="asset-label">
                                    {hasListingAsset ? "Their Listing" : "Their Channel"}
                                </div>
                                <div className="asset-title">
                                    {hasListingAsset ? listingTitle : channelTitle}
                                </div>
                                {hasListingAsset && channel?.title && (
                                    <div className="asset-meta">{channelTitle}</div>
                                )}
                                {channelUsername && (
                                    <div className="asset-meta">@{channelUsername}</div>
                                )}
                                {!hasListingAsset && typeof subscriberCount === "number" && (
                                    <div className="asset-meta">
                                        {subscriberCount.toLocaleString()} subscribers
                                    </div>
                                )}
                            </div>
                        </>
                    )}
                </div>

                <div className="request-footer">
                    <div className="footer-left">
                        <div className="footer-date">{formatDate(deal.createdAt)}</div>
                        {deal.scheduledPostTime && (
                            <div className="footer-scheduled">
                                Scheduled {formatDate(deal.scheduledPostTime)}
                            </div>
                        )}
                    </div>
                    <div className="role-badge">{roleLabel}</div>
                </div>
            </div>
        );
    };

    return (
        <>
            <div id="container-page-requests" className="deals-page">
                <div className="content-wrapper">
                    <div className="page-header animate__animated animate__fadeIn">
                        <h1>Deals</h1>
                        <p>Track finalized agreements between listings and campaigns</p>
                    </div>

                    <div className="search-container animate__animated animate__fadeIn">
                        <div className="search-input-wrapper">
                            <HiMagnifyingGlass className="search-icon" />
                            <input
                                type="text"
                                className="search-input"
                                placeholder="Search deals..."
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

                    <div className="toggle-container primary-toggle animate__animated animate__fadeIn">
                        <button
                            className={`toggle-button ${activeRoleFilter === "all" ? "active" : ""}`}
                            onClick={() => setActiveRoleFilter("all")}
                        >
                            All
                        </button>
                        <button
                            className={`toggle-button ${activeRoleFilter === "owner" ? "active" : ""}`}
                            onClick={() => setActiveRoleFilter("owner")}
                        >
                            Owner
                        </button>
                        <button
                            className={`toggle-button ${activeRoleFilter === "advertiser" ? "active" : ""}`}
                            onClick={() => setActiveRoleFilter("advertiser")}
                        >
                            Advertiser
                        </button>
                    </div>

                    {loading ? (
                        <div className="animate__animated animate__fadeIn">
                            <RequestsLoadingState count={5} />
                        </div>
                    ) : (
                        <div className="animate__animated animate__fadeIn">
                            <div className="requests-list">
                                {filteredDeals.length > 0 ? (
                                    filteredDeals.map(renderDealCard)
                                ) : (
                                    <div className="empty-state">
                                        <div className="empty-icon">🤝</div>
                                        <h3>No deals found</h3>
                                        <p>
                                            {searchTerm
                                                ? "No deals match your search."
                                                : "No deals are available for this view yet."}
                                        </p>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>
            </div>
            <BottomBar />
        </>
    );
};

export default PageDeals;
