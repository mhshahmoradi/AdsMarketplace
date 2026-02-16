import { memo, type MouseEvent } from "react";
import { getCampaignStatusColor, type Campaign } from "@/shared/types/campaign";
import { getColorFromId } from "@/shared/utils/hash";
import { formatDate, formatDateTime, formatNumber } from "@/shared/utils/format";
import "./ListItem.scss";

interface CampaignListItemProps {
    campaign: Campaign;
    onClick: () => void;
    onApplyClick?: () => void;
    showScheduleTime?: boolean;
    className?: string;
}

const hashSeed = (value: string): number => {
    let hash = 0;
    for (let i = 0; i < value.length; i += 1) {
        hash = (hash << 5) - hash + value.charCodeAt(i);
        hash |= 0;
    }
    return Math.abs(hash);
};

export const CampaignListItem = memo(
    ({
        campaign,
        onClick,
        onApplyClick,
        showScheduleTime = false,
        className = "",
    }: CampaignListItemProps) => {
        try {
            if (!campaign) {
                console.error("CampaignListItem received null/undefined campaign");
                return null;
            }

            const seed = hashSeed(campaign.id);
            const titleInitials = campaign.title
                ? campaign.title.substring(0, 2).toUpperCase()
                : "CA";
            const placeholderImage = `https://placehold.co/512x512/${getColorFromId(campaign.id)}/ffffff?text=${titleInitials}`;
            const campaignData = campaign as Campaign & {
                minSubscribers?: number | null;
                minAvgViews?: number | null;
                targetLanguages?: string | null;
                scheduleStart?: string | null;
                scheduleEnd?: string | null;
                isOwner?: boolean | null;
            };
            const isOwner = campaignData.isOwner === true;
            const shouldShowSpecialTag = seed % 3 === 0;
            const minSubscribersText =
                campaignData.minSubscribers == null
                    ? "Any"
                    : formatNumber(campaignData.minSubscribers);
            const minAvgViewsText =
                campaignData.minAvgViews == null
                    ? "Any"
                    : formatNumber(campaignData.minAvgViews);
            const targetLanguageText =
                campaignData.targetLanguages && campaignData.targetLanguages.trim()
                    ? campaignData.targetLanguages
                    : "Any";
            const startDateText = campaignData.scheduleStart
                ? showScheduleTime
                    ? formatDateTime(campaignData.scheduleStart)
                    : formatDate(campaignData.scheduleStart)
                : "Any";
            const endDateText = campaignData.scheduleEnd
                ? showScheduleTime
                    ? formatDateTime(campaignData.scheduleEnd)
                    : formatDate(campaignData.scheduleEnd)
                : "Any";
            const budget = campaign.budgetInTon ?? 0;
            const status = campaign.status || "Draft";

            const handleApplyClick = (event: MouseEvent<HTMLButtonElement>) => {
                event.stopPropagation();
                onApplyClick?.();
            };

            return (
                <div className={`campaign-list-item ${className}`.trim()} onClick={onClick}>
                    {shouldShowSpecialTag && (
                        <div className="campaign-special-tag">SPECIAL</div>
                    )}

                    <div className="campaign-card">
                        <div className="campaign-header-block">
                            <div className="campaign-header-avatar">
                                <img
                                    src={placeholderImage}
                                    alt={campaign.title}
                                    loading="lazy"
                                    onError={(event) => {
                                        if (event.currentTarget.src !== placeholderImage) {
                                            event.currentTarget.src = placeholderImage;
                                        }
                                    }}
                                />
                            </div>

                            <div className="campaign-header-info">
                                <h3 className="campaign-header-title">{campaign.title}</h3>
                            </div>

                            <div
                                className="campaign-status-chip"
                                style={{ color: getCampaignStatusColor(status) }}
                            >
                                <span className="campaign-status-icon">🧭</span>
                                <span className="campaign-status-text">{status}</span>
                            </div>
                        </div>

                        <div className="campaign-content-block">
                            {campaign.brief && (
                                <p className="campaign-content-brief">{campaign.brief}</p>
                            )}
                        </div>

                        <div className="campaign-stats-grid">
                            <div className="campaign-stat">
                                <span className="campaign-stat-icon">👥</span>
                                <span className="campaign-stat-value">
                                    {minSubscribersText}
                                </span>
                                <span className="campaign-stat-label">Min subs</span>
                            </div>

                            <div className="campaign-stat">
                                <span className="campaign-stat-icon">👁️</span>
                                <span className="campaign-stat-value">{minAvgViewsText}</span>
                                <span className="campaign-stat-label">Min avg views</span>
                            </div>

                            <div className="campaign-stat">
                                <span className="campaign-stat-icon">🌐</span>
                                <span className="campaign-stat-value">
                                    {targetLanguageText}
                                </span>
                                <span className="campaign-stat-label">Language</span>
                            </div>

                            <div className="campaign-budget-block">
                                <span className="campaign-budget-label">Budget</span>
                                <div className="campaign-budget-main">
                                    <span className="campaign-budget-icon">💵</span>
                                    <span className="campaign-budget-value">
                                        {formatNumber(budget)}
                                    </span>
                                </div>
                                <span className="campaign-budget-meta">TON</span>
                            </div>
                        </div>

                        <div className="campaign-schedule-row">
                            <div className="campaign-schedule-item">
                                <span className="campaign-schedule-icon">📅</span>
                                <span className="campaign-schedule-label">Start</span>
                                <span className="campaign-schedule-value">{startDateText}</span>
                            </div>
                            <div className="campaign-schedule-item">
                                <span className="campaign-schedule-icon">⏳</span>
                                <span className="campaign-schedule-label">End</span>
                                <span className="campaign-schedule-value">{endDateText}</span>
                            </div>
                        </div>

                        {!isOwner && onApplyClick && (
                            <div className="campaign-cta-wrap">
                                <button
                                    type="button"
                                    className="campaign-cta"
                                    onClick={handleApplyClick}
                                >
                                    APPLY
                                </button>
                            </div>
                        )}
                    </div>
                </div>
            );
        } catch (error) {
            console.error("Error rendering CampaignListItem:", error, campaign);
            return null;
        }
    },
);

CampaignListItem.displayName = "CampaignListItem";
