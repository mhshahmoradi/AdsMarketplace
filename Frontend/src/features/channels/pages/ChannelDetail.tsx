import "../styles/ChannelDetail.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useLocation, useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { requestAPI } from "@/shared/utils/api";
import { MdVerified } from "react-icons/md";
import type { Channel } from "@/shared/types/channel";
import { formatNumber } from "@/shared/utils/format";
import { getColorFromId } from "@/shared/utils/hash";
import {
    getChannelInitials,
    getChannelLanguageDistribution,
    getChannelTags,
} from "../types/channel";
import LanguageDistributionPieChart from "../components/LanguageDistributionPieChart";

const PageChannelDetail: FC = () => {
    const navigate = useNavigate();
    const location = useLocation() as { state?: { backTo?: string } };
    const { channelId } = useParams<{ channelId: string }>();
    const [channel, setChannel] = useState<Channel | null>(null);
    const [loading, setLoading] = useState(true);

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        if (location.state?.backTo) {
            navigate(location.state.backTo);
            return;
        }
        navigate("/");
    }, [navigate, location.state?.backTo]);

    useEffect(() => {
        const fetchChannelDetails = async () => {
            try {
                // Fetch channel details from API
                const data = await requestAPI(`/channels/${channelId}`, {}, "GET");

                if (data && data !== false && data !== null) {
                    setChannel({
                        ...data,
                        verified: data.status === "Verified",
                    });
                } else {
                    console.error("Failed to fetch channel details");
                }
                setLoading(false);
            } catch (error) {
                console.error("Error fetching channel details:", error);
                setLoading(false);
            }
        };

        fetchChannelDetails();
    }, [channelId]);

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

    if (loading || !channel) {
        return (
            <>
                <div id="container-page-channel-detail">
                    <div className="channel-detail-content">
                        <div className="channel-loading-state animate__animated animate__fadeIn">
                            <h2>Loading channel details...</h2>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    const tags = getChannelTags(channel);
    const language = channel.stats?.primaryLanguage || "Unknown";
    const isVerified = channel.verified || channel.status === "Verified";
    const placeholderColor = getColorFromId(channel.id);
    const titleInitials = getChannelInitials(channel.title);
    const channelUsername = channel.username
        ? channel.username.startsWith("@")
            ? channel.username
            : `@${channel.username}`
        : "@channel";
    const subscribers = channel.stats?.subscriberCount ?? channel.subscriberCount ?? 0;
    const avgViews = channel.stats?.avgViewsPerPost ?? channel.avgViewsPerPost ?? 0;
    const premiumSubscribers = channel.stats?.premiumSubscriberCount ?? 0;
    const languageDistribution = getChannelLanguageDistribution(channel);

    return (
        <>
            <div id="container-page-channel-detail">
                <div className="channel-detail-content">
                    <div className="channel-header-card animate__animated animate__fadeIn">
                        <div className="channel-header-avatar">
                            {channel.photoUrl ? (
                                <img
                                    src={channel.photoUrl}
                                    alt={channel.title}
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

                        <div className="channel-header-meta">
                            <h1>{channel.title}</h1>
                            <p className="channel-header-address">{channelUsername}</p>
                        </div>

                        {isVerified ? (
                            <span className="channel-verified-chip channel-header-badge">
                                <MdVerified />
                                <span>Verified</span>
                            </span>
                        ) : (
                            <span className="channel-unverified-chip channel-header-badge">
                                🟠 <span>Unverified</span>
                            </span>
                        )}
                    </div>

                    <div className="channel-stats-strip animate__animated animate__fadeIn">
                        <div className="channel-stat-item">
                            <span className="stat-icon">👥</span>
                            <strong className="stat-value">{formatNumber(subscribers)}</strong>
                            <span className="stat-label">Subscribers</span>
                        </div>
                        <div className="channel-stat-item">
                            <span className="stat-icon">👁️</span>
                            <strong className="stat-value">{formatNumber(avgViews)}</strong>
                            <span className="stat-label">Avg views</span>
                        </div>
                        <div className="channel-stat-item">
                            <span className="stat-icon">🎁</span>
                            <strong className="stat-value">
                                {formatNumber(premiumSubscribers)}
                            </strong>
                            <span className="stat-label">Premium subs</span>
                        </div>
                    </div>

                    <section className="channel-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Channel info</span>
                        <div className="section-box channel-tags-wrap">
                            {tags.length > 0
                                ? tags.map((tag, index) => (
                                      <span key={`${tag}-${index}`} className="channel-chip">
                                          {tag}
                                      </span>
                                  ))
                                : null}
                            <span className="channel-chip channel-chip-language">
                                🌐 {language}
                            </span>
                        </div>
                    </section>

                    <section className="channel-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Language distribution</span>
                        <div className="section-box channel-language-graph">
                            {languageDistribution.length > 0 ? (
                                <LanguageDistributionPieChart
                                    distribution={languageDistribution}
                                />
                            ) : (
                                <p className="language-graph-empty">
                                    No language data available yet.
                                </p>
                            )}
                        </div>
                    </section>

                    <div className="channel-detail-spacer" />
                </div>
            </div>
            <BottomBar />
        </>
    );
};

export default PageChannelDetail;
