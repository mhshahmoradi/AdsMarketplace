import "../styles/ChannelDetail.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { requestAPI } from "@/shared/utils/api";
import { MdVerified } from "react-icons/md";
import type { Channel } from "@/shared/types/channel";
import type { Listing } from "@/shared/types/listing";
import { formatNumber } from "@/shared/utils/format";
import { getColorFromId } from "@/shared/utils/hash";
import { sortByRecencyDesc } from "@/shared/utils/sort";
import { ChannelListingItem } from "@/shared/components/list/ChannelListingItem";
import {
    getChannelInitials,
    getChannelLanguageDistribution,
    getChannelTags,
} from "../types/channel";
import LanguageDistributionPieChart from "../components/LanguageDistributionPieChart";

const PageChannelDashboard: FC = () => {
    const navigate = useNavigate();
    const { channelId } = useParams<{ channelId: string }>();
    const [channel, setChannel] = useState<Channel | null>(null);
    const [loading, setLoading] = useState(true);
    const [listings, setListings] = useState<Listing[]>([]);
    const [loadingListings, setLoadingListings] = useState(false);

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate("/profile");
    }, [navigate]);

    const handleListingClick = (listingId: string) => {
        invokeHapticFeedbackImpact("light");
        navigate(`/listing/${listingId}`);
    };

    const handleListingStatusChange = useCallback((listingId: string, newStatus: string) => {
        setListings((prevListings) =>
            sortByRecencyDesc(
                prevListings.map((listing) =>
                    listing.id === listingId ? { ...listing, status: newStatus } : listing,
                ),
            ),
        );
    }, []);

    const handleVerifyChannel = () => {
        invokeHapticFeedbackImpact("medium");
        // Store channel info for verification page
        if (channel) {
            sessionStorage.setItem("channelToVerify", JSON.stringify(channel));
        }
        navigate("/channel-verification");
    };

    const handleCreateListing = () => {
        invokeHapticFeedbackImpact("medium");
        // Store channel info for listing creation page
        if (channel) {
            sessionStorage.setItem("channelForListing", JSON.stringify(channel));
        }
        navigate(`/channel/${channelId}/create-listing`);
    };

    useEffect(() => {
        const fetchChannelDetails = async () => {
            try {
                // Check if we have cached data from the add flow
                const cachedData = sessionStorage.getItem("lastAddedChannel");
                if (cachedData) {
                    const cached = JSON.parse(cachedData);
                    // Add channel response uses channelId, GET uses id
                    const cachedId = cached.channelId || cached.id;
                    if (cachedId === channelId) {
                        sessionStorage.removeItem("lastAddedChannel");
                    }
                }

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
        const fetchChannelListings = async () => {
            if (!channelId) return;

            setLoadingListings(true);
            try {
                const response = await requestAPI("/listings?OnlyMine=true", {}, "GET");

                if (response && response.items) {
                    // Filter listings for this specific channel
                    const channelListings = response.items.filter(
                        (listing: Listing) => listing.channelId === channelId,
                    );
                    setListings(sortByRecencyDesc(channelListings));
                } else if (Array.isArray(response)) {
                    // Handle if response is directly an array
                    const channelListings = response.filter(
                        (listing: Listing) => listing.channelId === channelId,
                    );
                    setListings(sortByRecencyDesc(channelListings));
                } else {
                    console.error("Failed to fetch channel listings");
                }
            } catch (error) {
                console.error("Error fetching channel listings:", error);
            } finally {
                setLoadingListings(false);
            }
        };

        if (channel) {
            fetchChannelListings();
        }
    }, [channelId, channel]);

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
                <div id="container-page-channel-dashboard">
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
            <div id="container-page-channel-dashboard">
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

                    {!isVerified && (
                        <div className="verification-warning animate__animated animate__fadeIn">
                            <div className="warning-content">
                                <span className="emoji">⚠️</span>
                                <p>
                                    Your channel is not verified. You should verify your channel
                                    to unlock all features.
                                </p>
                            </div>

                            <button
                                className="btn-channel-verify"
                                onClick={handleVerifyChannel}
                            >
                                <span>Verify Channel</span>
                            </button>
                        </div>
                    )}

                    {isVerified && (
                        <section className="channel-detail-section animate__animated animate__fadeIn">
                            <span className="section-label">Listings</span>
                            {loadingListings ? (
                                <div className="section-box channel-listings-loading">
                                    <p>Loading listings...</p>
                                </div>
                            ) : listings.length > 0 ? (
                                <div className="channel-listings-list">
                                    {listings.map((listing) => (
                                        <ChannelListingItem
                                            key={listing.id}
                                            listing={listing}
                                            onClick={() => handleListingClick(listing.id)}
                                            onPublishSuccess={handleListingStatusChange}
                                        />
                                    ))}
                                </div>
                            ) : (
                                <div className="section-box channel-listings-empty">
                                    <p>
                                        You haven't created any listings for this channel yet.
                                    </p>
                                </div>
                            )}
                        </section>
                    )}

                    {isVerified && (
                        <button
                            className="channel-primary-cta animate__animated animate__fadeIn"
                            onClick={handleCreateListing}
                        >
                            Create Listing
                        </button>
                    )}

                    <div className="channel-detail-spacer" />
                </div>
            </div>
            <BottomBar />
        </>
    );
};

export default PageChannelDashboard;
