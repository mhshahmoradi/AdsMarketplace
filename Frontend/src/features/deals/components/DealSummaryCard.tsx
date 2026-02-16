import { useNavigate } from "react-router";
import { invokeHapticFeedbackImpact } from "@/shared/utils/telegram";

interface DealSummaryCardProps {
    isOwner: boolean;
    hasListingAsset: boolean;
    listingTitle: string;
    listingId?: string | null;
    channelAssetTitle: string;
    channelId?: string | null;
    channelTitle?: string;
    channelUsername?: string;
    subscriberCount?: number;
    campaignId?: string | null;
    campaignTitle: string;
    campaignBrief?: string;
    campaignBudget?: number;
    backTo?: string;
}

const DealSummaryCard = ({
    isOwner,
    hasListingAsset,
    listingTitle,
    listingId,
    channelAssetTitle,
    channelId,
    channelTitle,
    channelUsername,
    subscriberCount,
    campaignId,
    campaignTitle,
    campaignBrief,
    campaignBudget,
    backTo,
}: DealSummaryCardProps) => {
    const navigate = useNavigate();

    const handleOpenAsset = (path?: string | null) => {
        if (!path) return;
        invokeHapticFeedbackImpact("light");
        if (backTo) {
            navigate(path, { state: { backTo } });
            return;
        }
        navigate(path);
    };

    const userAssetPath = isOwner
        ? hasListingAsset
            ? listingId
                ? `/listing/${listingId}/detail`
                : null
            : channelId
              ? `/channel/${channelId}/detail`
              : null
        : campaignId
          ? `/campaign/${campaignId}/detail`
          : null;

    const otherAssetPath = isOwner
        ? campaignId
            ? `/campaign/${campaignId}/detail`
            : null
        : hasListingAsset
          ? listingId
              ? `/listing/${listingId}/detail`
              : null
          : channelId
            ? `/channel/${channelId}/detail`
            : null;

    return (
        <div className="request-card static-card animate__animated animate__fadeIn">
            <button
                type="button"
                className="asset-container user-asset asset-link"
                disabled={!userAssetPath}
                onClick={() => handleOpenAsset(userAssetPath)}
            >
                {isOwner ? (
                    <>
                        <div className="asset-badge">{hasListingAsset ? "📋" : "📺"}</div>
                        <div className="asset-info">
                            <div className="asset-label">
                                {hasListingAsset ? "Your Listing" : "Your Channel"}
                            </div>
                            <div className="asset-title">
                                {hasListingAsset ? listingTitle : channelAssetTitle}
                            </div>
                            {hasListingAsset && channelTitle && (
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
                            {campaignBrief && <div className="asset-meta">{campaignBrief}</div>}
                            {campaignBudget !== undefined && (
                                <div className="asset-meta">{campaignBudget} TON budget</div>
                            )}
                        </div>
                    </>
                )}
            </button>

            <button
                type="button"
                className="asset-container other-asset asset-link"
                disabled={!otherAssetPath}
                onClick={() => handleOpenAsset(otherAssetPath)}
            >
                {isOwner ? (
                    <>
                        <div className="asset-badge">🎯</div>
                        <div className="asset-info">
                            <div className="asset-label">Their Campaign</div>
                            <div className="asset-title">{campaignTitle}</div>
                            {campaignBrief && <div className="asset-meta">{campaignBrief}</div>}
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
                                {hasListingAsset ? listingTitle : channelAssetTitle}
                            </div>
                            {hasListingAsset && channelTitle && (
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
            </button>
        </div>
    );
};

export default DealSummaryCard;
