import "../styles/CreateListing.scss";

import { useCallback, useEffect, useState, type FC } from "react";
import { useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { z } from "zod";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { requestAPI } from "@/shared/utils/api";
import BottomBar from "@/shared/components/BottomBar";
import ChannelResponseOverlay from "@/shared/components/ChannelResponseOverlay";
import type { Channel } from "@/shared/types/channel";
import {
    AdFormatType,
    AdFormatNames,
    type CreateListingRequest,
    type CreateListingResponse,
    type UpdateListingRequest,
    type UpdateListingResponse,
} from "../types/listing";
import type { APIErrorResponse } from "@/shared/types/api";

import { lottieAnimations } from "@/shared/utils/lottie";
import LottiePlayer from "@/shared/components/LottiePlayer";

// Validation schema
const listingSchema = z.object({
    title: z
        .string()
        .min(1, "Title is required")
        .max(256, "Title must not exceed 256 characters"),
    description: z
        .string()
        .min(1, "Description is required")
        .max(4000, "Description must not exceed 4000 characters"),
    postPrice: z.number().min(0.01, "Price must be greater than 0"),
    postDuration: z.number().int().min(1, "Duration must be at least 1 hour"),
    postTerms: z
        .string()
        .min(1, "Terms are required")
        .max(1000, "Terms must not exceed 1000 characters"),
});

type ListingFormData = z.infer<typeof listingSchema>;

const PageCreateListing: FC = () => {
    const navigate = useNavigate();
    const { channelId, listingId } = useParams<{ channelId?: string; listingId?: string }>();
    const [channel, setChannel] = useState<Channel | null>(null);
    const [isEditMode, setIsEditMode] = useState(false);
    const [isLoadingListing, setIsLoadingListing] = useState(false);

    // Form state
    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [selectedFormat, setSelectedFormat] = useState<string>(AdFormatType.Post);
    const [postPrice, setPostPrice] = useState("");
    const [postDuration, setPostDuration] = useState("24");
    const [postTerms, setPostTerms] = useState("");

    // UI state
    const [errors, setErrors] = useState<Partial<Record<keyof ListingFormData, string>>>({});
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [overlayStatus, setOverlayStatus] = useState<"success" | "error" | undefined>(
        undefined,
    );
    const [overlayTitle, setOverlayTitle] = useState("");
    const [overlayMessage, setOverlayMessage] = useState("");

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        if (isEditMode) {
            navigate(`/listing/${listingId}`);
        } else {
            navigate(`/channel/${channelId}`);
        }
    }, [navigate, channelId, listingId, isEditMode]);

    useEffect(() => {
        const fetchListingForEdit = async () => {
            if (listingId) {
                setIsEditMode(true);
                setIsLoadingListing(true);
                try {
                    const data = await requestAPI(`/listings/${listingId}`, {}, "GET");
                    if (data && data !== false && data !== null) {
                        // Populate form with existing data
                        setTitle(data.title || "");
                        setDescription(data.description || "");

                        // Extract first ad format (assuming Post format)
                        if (data.adFormats && data.adFormats.length > 0) {
                            const postFormat = data.adFormats[0];
                            setPostPrice(postFormat.priceInTon?.toString() || "");
                            setPostDuration(postFormat.durationHours?.toString() || "24");
                            setPostTerms(postFormat.terms || "");
                        }

                        // Set channel info if available
                        if (data.channelId) {
                            setChannel({
                                id: data.channelId,
                                username: data.channelUsername || "",
                                title: data.channelTitle || "",
                            } as Channel);
                        }
                    }
                } catch (error) {
                    console.error("Error fetching listing for edit:", error);
                } finally {
                    setIsLoadingListing(false);
                }
            } else {
                // Create mode - Try to get channel from session storage
                const cachedChannel = sessionStorage.getItem("channelForListing");
                if (cachedChannel) {
                    setChannel(JSON.parse(cachedChannel));
                }
            }
        };

        fetchListingForEdit();

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
    }, [onBackButton, listingId]);

    const validateForm = (): boolean => {
        try {
            const formData: ListingFormData = {
                title,
                description,
                postPrice: parseFloat(postPrice),
                postDuration: parseInt(postDuration),
                postTerms,
            };

            listingSchema.parse(formData);
            setErrors({});
            return true;
        } catch (error) {
            if (error instanceof z.ZodError) {
                const newErrors: Partial<Record<keyof ListingFormData, string>> = {};
                error.issues.forEach((err: z.ZodIssue) => {
                    const path = err.path[0] as keyof ListingFormData;
                    newErrors[path] = err.message;
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

        const effectiveChannelId = channelId || channel?.id;
        if (!effectiveChannelId && !isEditMode) {
            console.error("Channel ID is missing");
            return;
        }

        setIsSubmitting(true);
        invokeHapticFeedbackImpact("medium");

        try {
            let requestBody: CreateListingRequest | UpdateListingRequest;

            if (isEditMode) {
                // PUT request - exclude channelId, include status
                requestBody = {
                    title,
                    description,
                    status: "Draft", // Status for PUT
                    adFormats: [
                        {
                            formatType: AdFormatType.Post,
                            priceInTon: parseFloat(postPrice),
                            durationHours: parseInt(postDuration),
                            terms: postTerms,
                        },
                    ],
                } as UpdateListingRequest;
            } else {
                // POST request - include channelId
                requestBody = {
                    channelId: effectiveChannelId!,
                    title,
                    description,
                    adFormats: [
                        {
                            formatType: AdFormatType.Post,
                            priceInTon: parseFloat(postPrice),
                            durationHours: parseInt(postDuration),
                            terms: postTerms,
                        },
                    ],
                } as CreateListingRequest;
            }

            const endpoint = isEditMode ? `/listings/${listingId}` : "/listings";
            const method = isEditMode ? "PUT" : "POST";
            const response = await requestAPI(endpoint, requestBody, method, true);

            if (response && response !== false && response !== null) {
                let listingTitle = title;

                if (isEditMode) {
                    // PUT response has different structure
                    const updateData = response as UpdateListingResponse;
                    listingTitle = updateData.title;

                    // Store the updated listing
                    sessionStorage.setItem("lastUpdatedListing", JSON.stringify(updateData));
                } else {
                    // POST response
                    const listingData = response as CreateListingResponse;
                    listingTitle = listingData.title;

                    // Store the created listing
                    sessionStorage.setItem("lastCreatedListing", JSON.stringify(listingData));
                }

                // Show success overlay
                setOverlayStatus("success");
                setOverlayTitle(isEditMode ? "Listing Updated!" : "Listing Created!");
                setOverlayMessage(
                    isEditMode
                        ? `Listing "${listingTitle}" has been updated successfully!`
                        : `Listing "${listingTitle}" has been created successfully!`,
                );
            } else {
                throw new Error("Invalid response from server");
            }
        } catch (error) {
            console.error(`Error ${isEditMode ? "updating" : "creating"} listing:`, error);

            let errorMessage = `Failed to ${isEditMode ? "update" : "create"} listing. Please try again.`;

            if (error && typeof error === "object") {
                const errorObj = error as APIErrorResponse;
                if (errorObj.title) {
                    errorMessage = errorObj.title;
                } else if (errorObj.detail) {
                    errorMessage = errorObj.detail;
                } else if ((error as Error).message) {
                    errorMessage = (error as Error).message;
                }
            }

            setOverlayStatus("error");
            setOverlayTitle("Error");
            setOverlayMessage(errorMessage);
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleOverlaySuccess = () => {
        if (isEditMode) {
            navigate(`/listing/${listingId}`);
        } else {
            navigate(`/channel/${channelId}`);
        }
    };

    if (isLoadingListing) {
        return (
            <>
                <div id="container-page-create-listing">
                    <div className="content-wrapper">
                        <div className="loading-state animate__animated animate__fadeIn">
                            <h2>Loading listing...</h2>
                        </div>
                    </div>
                </div>
                <BottomBar />
            </>
        );
    }

    return (
        <>
            <div id="container-page-create-listing">
                <div className="content-wrapper">
                    <div className="lottie-container animate__animated animate__fadeIn">
                        <LottiePlayer
                            src={lottieAnimations.add.url}
                            fallback={<span>{lottieAnimations.add.emoji}</span>}
                            autoplay
                            loop
                        />
                    </div>
                    <div className="text-container animate__animated animate__fadeIn">
                        <h1>{isEditMode ? "Edit Listing" : "Create Listing"}</h1>
                        {channel && (
                            <p>
                                For channel: <strong>@{channel.username}</strong>
                            </p>
                        )}
                    </div>

                    <div className="form-container">
                        {/* Form Fields */}
                        {/* Title */}
                        <div className="form-group animate__animated animate__fadeIn">
                            <label htmlFor="title">
                                Listing Title <span className="required">*</span>
                            </label>
                            <input
                                type="text"
                                id="title"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                                placeholder="e.g., Premium Post Placement"
                                disabled={isSubmitting}
                                className={errors.title ? "error" : ""}
                            />
                            {errors.title && (
                                <span className="error-message">{errors.title}</span>
                            )}
                        </div>

                        {/* Description */}
                        <div className="form-group animate__animated animate__fadeIn">
                            <label htmlFor="description">
                                Description <span className="required">*</span>
                            </label>
                            <input
                                type="text"
                                id="description"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                placeholder="Describe what advertisers can expect..."
                                disabled={isSubmitting}
                                className={errors.description ? "error" : ""}
                            />
                            {errors.description && (
                                <span className="error-message">{errors.description}</span>
                            )}
                        </div>

                        {/* Ad Format Selection */}
                        <div className="form-group animate__animated animate__fadeIn">
                            <label>Ad Format</label>
                            <div className="format-options">
                                <button
                                    type="button"
                                    className={`format-option ${selectedFormat === AdFormatType.Post ? "active" : ""}`}
                                    onClick={() => {
                                        setSelectedFormat(AdFormatType.Post);
                                        invokeHapticFeedbackImpact("light");
                                    }}
                                    disabled={isSubmitting}
                                >
                                    <span className="format-icon">📝</span>
                                    <span className="format-name">
                                        {AdFormatNames[AdFormatType.Post]}
                                    </span>
                                </button>

                                <button
                                    type="button"
                                    className="format-option disabled"
                                    disabled
                                    title="Coming soon"
                                >
                                    <span className="format-icon">📸</span>
                                    <span className="format-name">
                                        {AdFormatNames[AdFormatType.Story]}
                                    </span>
                                    <span className="coming-soon">Soon</span>
                                </button>

                                <button
                                    type="button"
                                    className="format-option disabled"
                                    disabled
                                    title="Coming soon"
                                >
                                    <span className="format-icon">🔁</span>
                                    <span className="format-name">
                                        {AdFormatNames[AdFormatType.Repost]}
                                    </span>
                                    <span className="coming-soon">Soon</span>
                                </button>
                            </div>
                        </div>

                        {/* Post Format Details */}
                        {selectedFormat === AdFormatType.Post && (
                            <>
                                <div className="form-group animate__animated animate__fadeIn">
                                    <label htmlFor="postPrice">
                                        Price (TON) <span className="required">*</span>
                                    </label>
                                    <input
                                        type="number"
                                        id="postPrice"
                                        value={postPrice}
                                        onChange={(e) => setPostPrice(e.target.value)}
                                        placeholder="0.00"
                                        step="0.01"
                                        min="0.01"
                                        disabled={isSubmitting}
                                        className={errors.postPrice ? "error" : ""}
                                    />
                                    {errors.postPrice && (
                                        <span className="error-message">
                                            {errors.postPrice}
                                        </span>
                                    )}
                                </div>

                                <div className="form-group animate__animated animate__fadeIn">
                                    <label htmlFor="postDuration">
                                        Duration (Hours) <span className="required">*</span>
                                    </label>
                                    <input
                                        type="number"
                                        id="postDuration"
                                        value={postDuration}
                                        onChange={(e) => setPostDuration(e.target.value)}
                                        placeholder="24"
                                        min="1"
                                        disabled={isSubmitting}
                                        className={errors.postDuration ? "error" : ""}
                                    />
                                    {errors.postDuration && (
                                        <span className="error-message">
                                            {errors.postDuration}
                                        </span>
                                    )}
                                    <span className="field-hint">
                                        How long will the post stay visible?
                                    </span>
                                </div>

                                <div className="form-group animate__animated animate__fadeIn">
                                    <label htmlFor="postTerms">
                                        Terms & Conditions <span className="required">*</span>
                                    </label>
                                    <input
                                        type="text"
                                        id="postTerms"
                                        value={postTerms}
                                        onChange={(e) => setPostTerms(e.target.value)}
                                        placeholder="e.g., Content must be approved before posting, refunds available within 24 hours..."
                                        disabled={isSubmitting}
                                        className={errors.postTerms ? "error" : ""}
                                    />
                                    {errors.postTerms && (
                                        <span className="error-message">
                                            {errors.postTerms}
                                        </span>
                                    )}
                                </div>
                            </>
                        )}

                        {/* Submit Button */}
                        <button
                            className={`btn-create ${isSubmitting ? "disabled" : ""}`}
                            onClick={handleSubmit}
                            disabled={isSubmitting}
                        >
                            <span>
                                {isSubmitting
                                    ? isEditMode
                                        ? "Updating..."
                                        : "Creating..."
                                    : isEditMode
                                      ? "Update Listing"
                                      : "Create Listing"}
                            </span>
                        </button>
                    </div>
                </div>
            </div>
            <BottomBar />

            {overlayStatus && (
                <ChannelResponseOverlay
                    status={overlayStatus}
                    title={overlayTitle}
                    message={overlayMessage}
                    setStatus={setOverlayStatus}
                    onSuccess={handleOverlaySuccess}
                />
            )}
        </>
    );
};

export default PageCreateListing;
