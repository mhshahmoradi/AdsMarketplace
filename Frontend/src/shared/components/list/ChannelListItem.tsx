import { memo } from "react";
import { formatNumber } from "@/shared/utils/format";
import { getColorFromId } from "@/shared/utils/hash";
import { getChannelInitials, getChannelTags, type Channel } from "@/shared/types/channel";
import "./ListItem.scss";

interface ChannelListItemProps {
    channel: Channel;
    onClick: () => void;
    className?: string;
}

export const ChannelListItem = memo(
    ({ channel, onClick, className = "" }: ChannelListItemProps) => {
        try {
            if (!channel) {
                console.error("ChannelListItem received null/undefined channel");
                return null;
            }

            const titleInitials = getChannelInitials(channel.title || "");
            const placeholderImage =
                channel.image ||
                `https://placehold.co/512x512/${getColorFromId(channel.id)}/ffffff?text=${titleInitials}`;

            const tags = getChannelTags(channel);
            const verified = channel.status === "Verified";

            return (
                <div className={`list-item channel-list-item ${className}`} onClick={onClick}>
                    <div className="list-item-image">
                        <img
                            src={placeholderImage}
                            alt={channel.title || "Channel"}
                            loading="lazy"
                        />
                    </div>

                    <div className="list-item-details">
                        <h3 className="list-item-title">
                            {channel.title || "Untitled Channel"}
                        </h3>
                        <span className="list-item-subtitle">
                            @{channel.username || "unknown"}
                        </span>
                        <div className="list-item-stats">
                            <span>{formatNumber(channel.subscriberCount || 0)} subs</span>
                            <span> • </span>
                            <span>{formatNumber(channel.avgViewsPerPost || 0)} views</span>
                        </div>
                        {(verified || tags.length > 0) && (
                            <div className="list-item-badges">
                                {verified && (
                                    <span className="badge badge-verified">✓ Verified</span>
                                )}
                                {tags.slice(0, 2).map((tag, index) => (
                                    <span key={index} className="badge badge-category">
                                        {tag}
                                    </span>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            );
        } catch (error) {
            console.error("Error rendering ChannelListItem:", error, channel);
            return null;
        }
    },
);

ChannelListItem.displayName = "ChannelListItem";
