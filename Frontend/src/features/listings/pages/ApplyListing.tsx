import "../styles/ApplyListing.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import ChannelResponseOverlay from "@/shared/components/ChannelResponseOverlay";
import LottiePlayer from "@/shared/components/LottiePlayer";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { requestAPI } from "@/shared/utils/api";
import { sortByRecencyDesc } from "@/shared/utils/sort";
import type { Campaign } from "@/shared/types/campaign";
import { getColorFromId } from "@/shared/utils/hash";

import { z } from "zod";
import type { Listing, AdFormat } from "../types/listing";

// Validation schema for listing application
const applicationSchema = z.object({
    campaignId: z.string().min(1, "Please select a campaign"),
    adFormat: z.string().min(1, "Please select an ad format"),
    initialPriceInTon: z.number().min(0, "Price must be non-negative"),
    proposedSchedule: z.string().min(1, "Schedule is required"),
});

const PageApplyListing: FC = () => {
    const navigate = useNavigate();
    const { listingId } = useParams<{ listingId: string }>();

    const [campaigns, setCampaigns] = useState<Campaign[]>([]);
    const [listing, setListing] = useState<Listing | null>(null);
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [selectedCampaignId, setSelectedCampaignId] = useState<string>("");
    const [selectedAdFormat, setSelectedAdFormat] = useState<string>("");
    const [proposedPrice, setProposedPrice] = useState<string>("");
    const [proposedSchedule, setProposedSchedule] = useState<string>("");
    const [errors, setErrors] = useState<{ [key: string]: string }>({});

    const [responseStatus, setResponseStatus] = useState<"success" | "error" | undefined>();
    const [responseTitle, setResponseTitle] = useState("");
    const [responseMessage, setResponseMessage] = useState("");

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate(`/listing/${listingId}/detail`);
    }, [navigate, listingId]);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                // Fetch listing details
                const listingData = await requestAPI(`/listings/${listingId}`, {}, "GET");
                if (listingData && listingData !== false && listingData !== null) {
                    setListing(listingData);
                }

                // Fetch user's campaigns
                const campaignsData = await requestAPI(
                    "/campaigns/all?onlyMine=true",
                    {},
                    "GET",
                );

                if (campaignsData && campaignsData !== false && campaignsData !== null) {
                    if (campaignsData.items && Array.isArray(campaignsData.items)) {
                        // Filter to only open campaigns
                        const openCampaigns = campaignsData.items.filter(
                            (campaign: Campaign) => campaign && campaign.status === "Open",
                        );
                        console.log("Open campaigns loaded:", openCampaigns.length);
                        setCampaigns(sortByRecencyDesc(openCampaigns));
                    } else if (Array.isArray(campaignsData)) {
                        const openCampaigns = campaignsData.filter(
                            (campaign: Campaign) => campaign && campaign.status === "Open",
                        );
                        setCampaigns(sortByRecencyDesc(openCampaigns));
                    } else {
                        console.error("Invalid campaigns response format", campaignsData);
                    }
                } else {
                    console.error("Failed to fetch campaigns");
                }
            } catch (err) {
                console.error("Error fetching data:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
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

    const handleCampaignSelect = (campaignId: string) => {
        invokeHapticFeedbackImpact("light");
        setSelectedCampaignId(campaignId);
        setErrors((prev) => ({ ...prev, campaignId: "" }));
    };

    const handleAdFormatSelect = (formatType: string) => {
        invokeHapticFeedbackImpact("light");
        setSelectedAdFormat(formatType);
        setErrors((prev) => ({ ...prev, adFormat: "" }));

        // Auto-fill price from the selected format
        if (listing && listing.adFormats) {
            const format = listing.adFormats.find((f) => f.formatType === formatType);
            if (format) {
                setProposedPrice(format.priceInTon.toString());
            }
        }
    };

    const validateForm = (): boolean => {
        // Validate that required fields are filled
        if (!selectedCampaignId) {
            setErrors({ campaignId: "Please select a campaign" });
            return false;
        }

        if (!selectedAdFormat) {
            setErrors({ adFormat: "Please select an ad format" });
            return false;
        }

        if (!proposedPrice || proposedPrice.trim() === "") {
            setErrors({ initialPriceInTon: "Proposed price is required" });
            return false;
        }

        if (!proposedSchedule || proposedSchedule.trim() === "") {
            setErrors({ proposedSchedule: "Proposed schedule is required" });
            return false;
        }

        // Parse price
        const priceValue = parseFloat(proposedPrice);

        const formData = {
            campaignId: selectedCampaignId,
            adFormat: selectedAdFormat,
            initialPriceInTon: priceValue,
            proposedSchedule: proposedSchedule,
        };

        try {
            applicationSchema.parse(formData);
            setErrors({});
            return true;
        } catch (err) {
            if (err instanceof z.ZodError) {
                const newErrors: { [key: string]: string } = {};
                err.issues.forEach((issue) => {
                    const path = issue.path.join(".");
                    newErrors[path] = issue.message;
                });
                setErrors(newErrors);
            }
            return false;
        }
    };

    const handleSubmit = async () => {
        if (!validateForm()) {
            invokeHapticFeedbackImpact("light");
            return;
        }

        setSubmitting(true);
        invokeHapticFeedbackImpact("medium");

        try {
            const proposedScheduleIso = new Date(proposedSchedule).toISOString();
            const requestBody = {
                campaignId: selectedCampaignId,
                proposedPriceInTon: parseFloat(proposedPrice),
                message: `Ad format: ${selectedAdFormat}, proposed schedule: ${proposedScheduleIso}`,
            };

            const response = await requestAPI(
                `/listings/${listingId}/apply`,
                requestBody,
                "POST",
                true,
            );

            if (response && response !== false && response !== null) {
                // Check if response has id (success indicator)
                if (response.id || response.applicationId) {
                    setResponseStatus("success");
                    setResponseTitle("Deal Submitted!");
                    setResponseMessage(
                        "Your deal proposal has been successfully submitted to the listing owner.",
                    );
                } else {
                    // Handle error response
                    setResponseStatus("error");
                    setResponseTitle("Submission Failed");
                    setResponseMessage(
                        response.detail ||
                            response.message ||
                            "Failed to submit deal. Please try again.",
                    );
                }
            } else {
                setResponseStatus("error");
                setResponseTitle("Submission Failed");
                setResponseMessage("Failed to submit deal. Please try again.");
            }
        } catch (error) {
            console.error("Error submitting deal:", error);
            setResponseStatus("error");
            setResponseTitle("Submission Failed");
            setResponseMessage("An error occurred while submitting your deal proposal.");
        } finally {
            setSubmitting(false);
        }
    };

    const handleOverlaySuccess = () => {
        navigate(`/listing/${listingId}/detail`);
    };

    if (loading) {
        return (
            <>
                <div id="container-page-apply-campaign">
                    <div className="content-wrapper">
                        <div className="loading-state">
                            <h2>Loading your campaigns...</h2>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    if (campaigns.length === 0) {
        return (
            <>
                <div id="container-page-apply-campaign">
                    <div className="content-wrapper">
                        <div className="empty-state">
                            <h2>No Campaigns Available</h2>
                            <p>
                                You need to create an open campaign before applying to listings.
                            </p>
                            <button
                                className="btn-create-campaign"
                                onClick={() => {
                                    invokeHapticFeedbackImpact("medium");
                                    navigate("/create-campaign");
                                }}
                            >
                                Create Campaign
                            </button>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    return (
        <>
            <div id="container-page-apply-campaign">
                <div className="content-wrapper">
                    <div style={{ width: "8rem", height: "8rem", margin: "1rem auto 0" }}>
                        <LottiePlayer
                            src="/assets/lottie/duck_search.json"
                            autoplay={true}
                            loop={true}
                        />
                    </div>
                    <div className="text-container animate__animated animate__fadeIn">
                        <h1>Apply to Listing</h1>
                        <p>Select one of your campaigns to apply to this listing</p>
                    </div>

                    {/* Campaign Selection */}
                    <div className="form-container">
                        <div className="form-group">
                            <label htmlFor="campaign-select">
                                Select Campaign <span className="required">*</span>
                            </label>
                            {errors.campaignId && (
                                <span className="error-message">{errors.campaignId}</span>
                            )}
                            <div className="campaigns-grid">
                                {campaigns.map((campaign) => {
                                    const isSelected = selectedCampaignId === campaign.id;
                                    const placeholderColor = getColorFromId(campaign.id);
                                    const initials = campaign.title
                                        ? campaign.title.substring(0, 2).toUpperCase()
                                        : "CP";

                                    return (
                                        <div
                                            key={campaign.id}
                                            className={`campaign-card ${isSelected ? "selected" : ""}`}
                                            onClick={() => handleCampaignSelect(campaign.id)}
                                        >
                                            <div
                                                className="campaign-avatar"
                                                style={{
                                                    backgroundColor: `#${placeholderColor}`,
                                                }}
                                            >
                                                {initials}
                                            </div>
                                            <div className="campaign-info">
                                                <h3 className="campaign-title">
                                                    {campaign.title}
                                                </h3>
                                                <p className="campaign-budget">
                                                    {campaign.budgetInTon} TON
                                                </p>
                                            </div>
                                            {isSelected && (
                                                <div className="selected-indicator">✓</div>
                                            )}
                                        </div>
                                    );
                                })}
                            </div>
                        </div>

                        {/* Ad Format Selection */}
                        {listing && listing.adFormats && listing.adFormats.length > 0 && (
                            <div className="form-group">
                                <label htmlFor="format-select">
                                    Select Ad Format <span className="required">*</span>
                                </label>
                                {errors.adFormat && (
                                    <span className="error-message">{errors.adFormat}</span>
                                )}
                                <div className="ad-formats-grid">
                                    {listing.adFormats.map(
                                        (format: AdFormat, index: number) => {
                                            const isSelected =
                                                selectedAdFormat === format.formatType;
                                            return (
                                                <div
                                                    key={index}
                                                    className={`format-card ${isSelected ? "selected" : ""}`}
                                                    onClick={() =>
                                                        handleAdFormatSelect(format.formatType)
                                                    }
                                                >
                                                    <div className="format-name">
                                                        {format.formatType}
                                                    </div>
                                                    <div className="format-price">
                                                        {format.priceInTon} TON
                                                    </div>
                                                    {format.durationHours > 0 && (
                                                        <div className="format-duration">
                                                            {format.durationHours}h
                                                        </div>
                                                    )}
                                                    {isSelected && (
                                                        <div className="selected-indicator">
                                                            ✓
                                                        </div>
                                                    )}
                                                </div>
                                            );
                                        },
                                    )}
                                </div>
                            </div>
                        )}

                        {/* Proposed Price */}
                        <div className="form-group">
                            <label htmlFor="proposed-price">
                                Proposed Price (TON) <span className="required">*</span>
                            </label>
                            {errors.initialPriceInTon && (
                                <span className="error-message">
                                    {errors.initialPriceInTon}
                                </span>
                            )}
                            <input
                                type="number"
                                id="proposed-price"
                                value={proposedPrice}
                                onChange={(e) => {
                                    setProposedPrice(e.target.value);
                                    setErrors((prev) => ({ ...prev, initialPriceInTon: "" }));
                                }}
                                placeholder="Enter proposed price"
                                min="0"
                                step="0.01"
                                disabled={submitting}
                            />
                        </div>

                        {/* Proposed Schedule */}
                        <div className="form-group">
                            <label htmlFor="proposed-schedule">
                                Proposed Schedule <span className="required">*</span>
                            </label>
                            {errors.proposedSchedule && (
                                <span className="error-message">{errors.proposedSchedule}</span>
                            )}
                            <input
                                type="datetime-local"
                                id="proposed-schedule"
                                value={proposedSchedule}
                                onChange={(e) => {
                                    setProposedSchedule(e.target.value);
                                    setErrors((prev) => ({ ...prev, proposedSchedule: "" }));
                                }}
                                disabled={submitting}
                            />
                        </div>

                        {/* Submit Button */}
                        <button
                            className={`btn-submit ${submitting ? "disabled" : ""}`}
                            onClick={handleSubmit}
                            disabled={submitting}
                        >
                            {submitting ? "Submitting..." : "Submit Deal"}
                        </button>
                    </div>
                </div>
            </div>
            <BottomBar />

            {/* Response Overlay */}
            {responseStatus && (
                <ChannelResponseOverlay
                    status={responseStatus}
                    title={responseTitle}
                    message={responseMessage}
                    setStatus={setResponseStatus}
                    onSuccess={handleOverlaySuccess}
                />
            )}
        </>
    );
};

export default PageApplyListing;
