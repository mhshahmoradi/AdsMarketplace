import { memo, type MouseEvent } from "react";
import type { Listing } from "@/shared/types/listing";
import { getColorFromId } from "@/shared/utils/hash";
import { formatNumber } from "@/shared/utils/format";
import "./ListItem.scss";

interface ListingListItemProps {
    listing: Listing;
    onClick: () => void;
    onApplyClick?: () => void;
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

export const ListingListItem = memo(
    ({ listing, onClick, onApplyClick, className = "" }: ListingListItemProps) => {
        try {
            if (!listing) {
                console.error("ListingListItem received null/undefined listing");
                return null;
            }

            const seed = hashSeed(`${listing.id}-${listing.channelId}`);
            const titleInitials = listing.channelTitle
                ? listing.channelTitle.substring(0, 2).toUpperCase()
                : "CH";
            const placeholderImage = `https://placehold.co/512x512/${getColorFromId(listing.channelId)}/ffffff?text=${titleInitials}`;
            const listingWithPhoto = listing as Listing & {
                channelPhotoUrl?: string;
                channelPhoto?: string;
                channelAvatarUrl?: string;
                photoUrl?: string;
                isOwner?: boolean | null;
            };
            const channelAvatar =
                listingWithPhoto.channelPhotoUrl ||
                listingWithPhoto.channelPhoto ||
                listingWithPhoto.channelAvatarUrl ||
                listingWithPhoto.photoUrl;
            const isOwner = listingWithPhoto.isOwner === true;
            const primaryAdFormat = listing.adFormats?.[0];
            const premiumSubscribers = listing.premiumSubscriberCount ?? 0;
            const shouldShowSpecialTag = seed % 3 === 0;
            const adFormatType = primaryAdFormat?.formatType || "Post";
            const startingPrice = primaryAdFormat?.priceInTon ?? 0;
            const durationHours = primaryAdFormat?.durationHours ?? 24;
            const channelUsername = listing.channelUsername
                ? listing.channelUsername.startsWith("@")
                    ? listing.channelUsername
                    : `@${listing.channelUsername}`
                : "@channel";

            const handleApplyClick = (event: MouseEvent<HTMLButtonElement>) => {
                event.stopPropagation();
                onApplyClick?.();
            };

            return (
                <div className={`listing-list-item ${className}`.trim()} onClick={onClick}>
                    {shouldShowSpecialTag && <div className="listing-special-tag">SPECIAL</div>}

                    <div className="listing-card">
                        <div className="listing-channel-block">
                            <div className="listing-channel-avatar">
                                <img
                                    src={channelAvatar || placeholderImage}
                                    alt={listing.channelTitle}
                                    loading="lazy"
                                    onError={(event) => {
                                        if (event.currentTarget.src !== placeholderImage) {
                                            event.currentTarget.src = placeholderImage;
                                        }
                                    }}
                                />
                            </div>

                            <div className="listing-channel-info">
                                <h3 className="listing-channel-title">
                                    {listing.channelTitle}
                                </h3>
                                <p className="listing-channel-username">{channelUsername}</p>
                            </div>

                            <div className="listing-ad-format-chip">
                                <span className="listing-ad-format-icon">📋</span>
                                <span className="listing-ad-format-text">{adFormatType}</span>
                            </div>
                        </div>

                        <div className="listing-content-block">
                            <h4 className="listing-content-title">{listing.title}</h4>
                            {listing.description && (
                                <p className="listing-content-brief">{listing.description}</p>
                            )}
                        </div>

                        <div className="listing-stats-grid">
                            <div className="listing-stat">
                                <span className="listing-stat-icon">👥</span>
                                <span className="listing-stat-value">
                                    {formatNumber(listing.subscriberCount || 0)}
                                </span>
                                <span className="listing-stat-label">Subscribers</span>
                            </div>

                            <div className="listing-stat">
                                <span className="listing-stat-icon">👁️</span>
                                <span className="listing-stat-value">
                                    {formatNumber(listing.avgViews || 0)}
                                </span>
                                <span className="listing-stat-label">Avg views</span>
                            </div>

                            <div className="listing-stat">
                                <span className="listing-stat-icon">🎁</span>
                                <span className="listing-stat-value">
                                    {formatNumber(premiumSubscribers)}
                                </span>
                                <span className="listing-stat-label">Premium subs</span>
                            </div>

                            <div className="listing-price-block">
                                <span className="listing-price-label">Starting from</span>
                                <div className="listing-price-main">
                                    <span className="listing-price-icon">💵</span>
                                    <span className="listing-price-value">
                                        {formatNumber(startingPrice)}
                                    </span>
                                </div>
                                <span className="listing-price-meta">
                                    TON / {durationHours}hr
                                </span>
                            </div>
                        </div>

                        {!isOwner && onApplyClick && (
                            <div className="listing-cta-wrap">
                                <button
                                    type="button"
                                    className="listing-cta"
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
            console.error("Error rendering ListingListItem:", error, listing);
            return null;
        }
    },
);

ListingListItem.displayName = "ListingListItem";
