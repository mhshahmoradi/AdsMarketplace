import "../styles/CampaignDetail.scss";

import { useCallback, useEffect, useState, type FC } from "react";
import { MdEdit, MdExpandMore, MdExpandLess } from "react-icons/md";

import BottomBar from "@/shared/components/BottomBar";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { requestAPI } from "@/shared/utils/api";
import { getCampaignStatusColor, type Campaign } from "../types/campaign";
import { formatNumber, formatDateTime } from "@/shared/utils/format";
import { getColorFromId } from "@/shared/utils/hash";

const PageCampaignDashboard: FC = () => {
    const navigate = useNavigate();
    const { campaignId } = useParams<{ campaignId: string }>();
    const [campaign, setCampaign] = useState<Campaign | null>(null);
    const [loading, setLoading] = useState(true);
    const [briefExpanded, setBriefExpanded] = useState(false);

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate("/profile");
    }, [navigate]);

    const handleEdit = () => {
        invokeHapticFeedbackImpact("medium");
        navigate(`/campaign/${campaignId}/edit`);
    };

    useEffect(() => {
        const fetchCampaignDetails = async () => {
            try {
                const cachedData = sessionStorage.getItem("lastCreatedCampaign");
                if (cachedData) {
                    const cached = JSON.parse(cachedData);
                    if (cached.id === campaignId) {
                        sessionStorage.removeItem("lastCreatedCampaign");
                    }
                }

                const data = await requestAPI(`/campaigns/${campaignId}`, {}, "GET");

                if (data && data !== false && data !== null) {
                    setCampaign(data);
                } else {
                    console.error("Failed to fetch campaign details");
                }
                setLoading(false);
            } catch (error) {
                console.error("Error fetching campaign details:", error);
                setLoading(false);
            }
        };

        fetchCampaignDetails();
    }, [campaignId]);

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

    if (loading || !campaign) {
        return (
            <>
                <div id="container-page-campaign-dashboard-redesign">
                    <div className="campaign-detail-content">
                        <div className="campaign-loading-state animate__animated animate__fadeIn">
                            <h2>Loading campaign details...</h2>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    const placeholderColor = getColorFromId(campaign.id);
    const titleInitials = campaign.title ? campaign.title.substring(0, 2).toUpperCase() : "CP";
    const campaignData = campaign as Campaign & {
        minSubscribers?: number | null;
        minAvgViews?: number | null;
        targetLanguages?: string | null;
        scheduleStart?: string | null;
        scheduleEnd?: string | null;
        applications?: Array<unknown> | null;
    };
    const minSubscribersText =
        campaignData.minSubscribers == null ? "Any" : formatNumber(campaignData.minSubscribers);
    const minAvgViewsText =
        campaignData.minAvgViews == null ? "Any" : formatNumber(campaignData.minAvgViews);
    const applicationsText = formatNumber(campaignData.applications?.length ?? 0);
    const targetLanguageText =
        campaignData.targetLanguages && campaignData.targetLanguages.trim()
            ? campaignData.targetLanguages
            : "Any";
    const startDateText = campaignData.scheduleStart
        ? formatDateTime(campaignData.scheduleStart)
        : "Any";
    const endDateText = campaignData.scheduleEnd
        ? formatDateTime(campaignData.scheduleEnd)
        : "Any";
    const budgetText = formatNumber(campaign.budgetInTon ?? 0);
    const status = campaign.status || "Draft";
    const shouldShowBriefToggle = (campaign.brief || "").length > 220;

    return (
        <>
            <div id="container-page-campaign-dashboard-redesign">
                <div className="campaign-detail-content">
                    <button className="btn-campaign-edit" onClick={handleEdit}>
                        <MdEdit />
                    </button>

                    <div className="campaign-header-card animate__animated animate__fadeIn">
                        <div className="campaign-header-avatar">
                            <div
                                className="profile-placeholder"
                                style={{ backgroundColor: `#${placeholderColor}` }}
                            >
                                {titleInitials}
                            </div>
                        </div>

                        <div className="campaign-header-meta">
                            <h1>{campaign.title}</h1>
                            <p className="campaign-header-status">
                                Status:{" "}
                                <span style={{ color: getCampaignStatusColor(status) }}>
                                    {status}
                                </span>
                            </p>
                        </div>
                    </div>

                    <div className="campaign-stats-strip animate__animated animate__fadeIn">
                        <div className="campaign-stat-item">
                            <span className="stat-icon">👥</span>
                            <strong className="stat-value">{minSubscribersText}</strong>
                            <span className="stat-label">Min subs</span>
                        </div>
                        <div className="campaign-stat-item">
                            <span className="stat-icon">👁️</span>
                            <strong className="stat-value">{minAvgViewsText}</strong>
                            <span className="stat-label">Min avg</span>
                        </div>
                        <div className="campaign-stat-item">
                            <span className="stat-icon">📬</span>
                            <strong className="stat-value">{applicationsText}</strong>
                            <span className="stat-label">Applications</span>
                        </div>
                    </div>

                    <section className="campaign-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Brief</span>
                        <div className="section-box">
                            <div className={`brief-content ${briefExpanded ? "expanded" : ""}`}>
                                <p>{campaign.brief || "No brief provided."}</p>
                            </div>
                            {shouldShowBriefToggle && (
                                <button
                                    className="btn-brief-toggle"
                                    onClick={() => {
                                        invokeHapticFeedbackImpact("light");
                                        setBriefExpanded(!briefExpanded);
                                    }}
                                >
                                    {briefExpanded ? (
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

                    <section className="campaign-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Requirements</span>
                        <div className="section-box section-requirements">
                            <div className="requirement-row">
                                <span className="requirement-key">
                                    <span className="requirement-icon">🌐</span>
                                    <span>Language</span>
                                </span>
                                <span className="requirement-value">{targetLanguageText}</span>
                            </div>
                            <div className="requirement-row">
                                <span className="requirement-key">
                                    <span className="requirement-icon">📅</span>
                                    <span>Start date</span>
                                </span>
                                <span className="requirement-value">{startDateText}</span>
                            </div>
                            <div className="requirement-row">
                                <span className="requirement-key">
                                    <span className="requirement-icon">⏳</span>
                                    <span>End date</span>
                                </span>
                                <span className="requirement-value">{endDateText}</span>
                            </div>
                        </div>
                    </section>

                    <section className="campaign-detail-section animate__animated animate__fadeIn">
                        <span className="section-label">Budget</span>
                        <div className="budget-card">
                            <span className="budget-icon">💵</span>
                            <p className="budget-value">{budgetText} TON</p>
                        </div>
                    </section>

                    <div className="campaign-detail-spacer" />
                </div>
            </div>
            <BottomBar />
        </>
    );
};

export default PageCampaignDashboard;
