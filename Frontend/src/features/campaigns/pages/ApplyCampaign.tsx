import "../styles/ApplyCampaign.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import ChannelResponseOverlay from "@/shared/components/ChannelResponseOverlay";
import LottiePlayer from "@/shared/components/LottiePlayer";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { requestAPI } from "@/shared/utils/api";
import { sortByRecencyDesc } from "@/shared/utils/sort";
import { type Channel, isChannelVerified, getChannelInitials } from "@/shared/types/channel";
import { getColorFromId } from "@/shared/utils/hash";
import { formatNumber } from "@/shared/utils/format";
import { z } from "zod";
import { MdVerified } from "react-icons/md";

// Validation schema for campaign application
const applicationSchema = z.object({
    channelId: z.string().min(1, "Please select a channel"),
    proposedPriceInTon: z.number().min(0, "Price must be non-negative"),
    note: z.string().max(1000, "Note must be less than 1000 characters").optional(),
});

const PageApplyCampaign: FC = () => {
    const navigate = useNavigate();
    const { campaignId } = useParams<{ campaignId: string }>();

    const [channels, setChannels] = useState<Channel[]>([]);
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [selectedChannelId, setSelectedChannelId] = useState<string>("");
    const [proposedPrice, setProposedPrice] = useState<string>("");
    const [note, setNote] = useState<string>("");
    const [errors, setErrors] = useState<{ [key: string]: string }>({});

    const [responseStatus, setResponseStatus] = useState<"success" | "error" | undefined>();
    const [responseTitle, setResponseTitle] = useState("");
    const [responseMessage, setResponseMessage] = useState("");

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate(`/campaign/${campaignId}/detail`);
    }, [navigate, campaignId]);

    useEffect(() => {
        const fetchChannels = async () => {
            setLoading(true);
            try {
                // Fetch user's channels
                const data = await requestAPI("/channels?onlyMine=true", {}, "GET");

                if (data && data !== false && data !== null) {
                    if (data.items && Array.isArray(data.items)) {
                        // Filter to only verified channels
                        const verifiedChannels = data.items.filter((channel: Channel) => {
                            return channel && isChannelVerified(channel);
                        });
                        console.log("Verified channels loaded:", verifiedChannels.length);
                        setChannels(sortByRecencyDesc(verifiedChannels));
                    } else {
                        console.error("Invalid channels response format", data);
                    }
                } else {
                    console.error("Failed to fetch channels");
                }
            } catch (err) {
                console.error("Error fetching channels:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchChannels();
    }, []);

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

    const handleChannelSelect = (channelId: string) => {
        invokeHapticFeedbackImpact("light");
        setSelectedChannelId(channelId);
        setErrors((prev) => ({ ...prev, channelId: "" }));
    };

    const validateForm = (): boolean => {
        // Validate that required fields are filled
        if (!selectedChannelId) {
            setErrors({ channelId: "Please select a channel" });
            return false;
        }

        if (!proposedPrice || proposedPrice.trim() === "") {
            setErrors({ proposedPriceInTon: "Proposed price is required" });
            return false;
        }

        // Parse price
        const priceValue = parseFloat(proposedPrice);

        const formData = {
            channelId: selectedChannelId,
            proposedPriceInTon: priceValue,
            note: note || undefined,
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
            const requestBody = {
                channelId: selectedChannelId,
                proposedPriceInTon: parseFloat(proposedPrice),
                note: note || "",
            };

            const response = await requestAPI(
                `/campaigns/${campaignId}/apply`,
                requestBody,
                "POST",
                true,
            );

            if (response && response !== false && response !== null) {
                // Check if response has applicationId (success indicator)
                if (response.applicationId || response.id) {
                    setResponseStatus("success");
                    setResponseTitle("Application Submitted!");
                    setResponseMessage(
                        "Your application has been successfully submitted to the campaign owner.",
                    );
                } else {
                    // Handle error response
                    setResponseStatus("error");
                    setResponseTitle("Application Failed");
                    setResponseMessage(
                        response.detail ||
                            response.message ||
                            "Failed to submit application. Please try again.",
                    );
                }
            } else {
                setResponseStatus("error");
                setResponseTitle("Application Failed");
                setResponseMessage("Failed to submit application. Please try again.");
            }
        } catch (error) {
            console.error("Error submitting application:", error);
            setResponseStatus("error");
            setResponseTitle("Application Failed");
            setResponseMessage("An error occurred while submitting your application.");
        } finally {
            setSubmitting(false);
        }
    };

    if (loading) {
        return (
            <>
                <div id="container-page-apply-campaign">
                    <div className="content-wrapper">
                        <div className="loading-state">
                            <h2>Loading your channels...</h2>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    if (channels.length === 0) {
        return (
            <>
                <div id="container-page-apply-campaign">
                    <div className="content-wrapper">
                        <div className="empty-state">
                            <h2>No Verified Channels</h2>
                            <p>
                                You need to have at least one verified channel to apply to
                                campaigns.
                            </p>
                            <button
                                className="btn-add-channel"
                                onClick={() => {
                                    invokeHapticFeedbackImpact("medium");
                                    navigate("/add-channel");
                                }}
                            >
                                Add Channel
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
                            src="/assets/lottie/duck_investigator.json"
                            autoplay={true}
                            loop={true}
                        />
                    </div>
                    <div className="page-header animate__animated animate__fadeIn">
                        <h1>Apply to Campaign</h1>
                        <p>Select a verified channel and submit your application</p>
                    </div>

                    {/* Channel Selection */}
                    <div className="form-section animate__animated animate__fadeIn">
                        <label className="form-label">
                            Select Channel <span className="required">*</span>
                        </label>
                        {errors.channelId && (
                            <span className="error-message">{errors.channelId}</span>
                        )}
                        <div className="channel-list-container">
                            {channels.map((channel) => {
                                const isSelected = selectedChannelId === channel.id;
                                const placeholderColor = getColorFromId(channel.id);
                                const initials = getChannelInitials(channel.title);

                                return (
                                    <div
                                        key={channel.id}
                                        className={`channel-item ${isSelected ? "selected" : ""}`}
                                        onClick={() => handleChannelSelect(channel.id)}
                                    >
                                        <div
                                            className="channel-avatar"
                                            style={{
                                                backgroundColor: `#${placeholderColor}`,
                                            }}
                                        >
                                            {initials}
                                        </div>
                                        <div className="channel-info">
                                            <div className="channel-title">
                                                {channel.title}
                                                {isChannelVerified(channel) && (
                                                    <MdVerified className="verified-icon" />
                                                )}
                                            </div>
                                            <div className="channel-username">
                                                @{channel.username}
                                            </div>
                                            <div className="channel-stats">
                                                {formatNumber(
                                                    channel.stats?.subscriberCount ||
                                                        channel.subscriberCount ||
                                                        0,
                                                )}{" "}
                                                subscribers
                                            </div>
                                        </div>
                                        <div className="channel-check">
                                            {isSelected && (
                                                <div className="selected-indicator">✓</div>
                                            )}
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>

                    {/* Proposed Price */}
                    <div className="form-section animate__animated animate__fadeIn">
                        <label className="form-label" htmlFor="proposed-price">
                            Proposed Price (TON) <span className="required">*</span>
                        </label>
                        {errors.proposedPriceInTon && (
                            <span className="error-message">{errors.proposedPriceInTon}</span>
                        )}
                        <input
                            id="proposed-price"
                            type="number"
                            className="form-input"
                            placeholder="Enter proposed price in TON"
                            value={proposedPrice}
                            onChange={(e) => {
                                setProposedPrice(e.target.value);
                                setErrors((prev) => ({ ...prev, proposedPriceInTon: "" }));
                            }}
                            min="0"
                            step="0.01"
                        />
                    </div>

                    {/* Note */}
                    <div className="form-section animate__animated animate__fadeIn">
                        <label className="form-label" htmlFor="note">
                            Note (Optional)
                        </label>
                        {errors.note && <span className="error-message">{errors.note}</span>}
                        <textarea
                            id="note"
                            className="form-textarea"
                            placeholder="Add a note to the campaign owner (optional)"
                            value={note}
                            onChange={(e) => {
                                setNote(e.target.value);
                                setErrors((prev) => ({ ...prev, note: "" }));
                            }}
                            rows={4}
                            maxLength={1000}
                        />
                        <span className="character-count">{note.length} / 1000 characters</span>
                    </div>

                    {/* Submit Button */}
                    <button
                        className={`btn-submit animate__animated animate__fadeIn ${submitting ? "submitting" : ""}`}
                        onClick={handleSubmit}
                        disabled={submitting}
                    >
                        {submitting ? "Submitting..." : "Submit Application"}
                    </button>
                </div>
            </div>
            <BottomBar />

            {responseStatus && (
                <ChannelResponseOverlay
                    status={responseStatus}
                    title={responseTitle}
                    message={responseMessage}
                    setStatus={setResponseStatus}
                    onSuccess={() => {
                        // Navigate back to campaign detail
                        navigate(`/campaign/${campaignId}/detail`);
                    }}
                />
            )}
        </>
    );
};

export default PageApplyCampaign;
