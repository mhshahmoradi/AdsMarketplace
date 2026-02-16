import "../styles/CreateCampaign.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import LottiePlayer from "@/shared/components/LottiePlayer";
import { lottieAnimations } from "@/shared/utils/lottie";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate, useParams } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import ChannelResponseOverlay from "@/shared/components/ChannelResponseOverlay";
import { requestAPI } from "@/shared/utils/api";
import { z } from "zod";
import { CampaignStatus, type UpdateCampaignResponse } from "../types/campaign";

type CreateCampaignResponse = {
    id: string;
    title: string;
    budgetInTon: number;
    status: CampaignStatus;
};

// Zod schema matching backend validator
const campaignSchema = z
    .object({
        title: z
            .string()
            .trim()
            .min(1, "Campaign title is required")
            .max(256, "Title must be 256 characters or less"),
        brief: z
            .string()
            .trim()
            .min(1, "Campaign brief is required")
            .max(4000, "Brief must be 4000 characters or less"),
        budgetInTon: z.number().positive("Budget must be greater than 0"),
        minSubscribers: z
            .number()
            .int()
            .nonnegative("Minimum subscribers must be 0 or greater")
            .optional(),
        minAvgViews: z
            .number()
            .int()
            .nonnegative("Minimum average views must be 0 or greater")
            .optional(),
        targetLanguages: z.string().trim().optional(),
        scheduleStart: z.date().optional(),
        scheduleEnd: z.date().optional(),
    })
    .superRefine((data, ctx) => {
        const now = new Date();

        if (!data.scheduleStart) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                message: "Start date is required",
                path: ["scheduleStart"],
            });
        }

        if (!data.scheduleEnd) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                message: "End date is required",
                path: ["scheduleEnd"],
            });
        }

        const startInPast = Boolean(data.scheduleStart && data.scheduleStart < now);
        const endInPast = Boolean(data.scheduleEnd && data.scheduleEnd < now);

        if (startInPast) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                message: "Start date cannot be in the past",
                path: ["scheduleStart"],
            });
        }

        if (endInPast) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                message: "End date cannot be in the past",
                path: ["scheduleEnd"],
            });
        }

        if (
            data.scheduleStart &&
            data.scheduleEnd &&
            !startInPast &&
            !endInPast &&
            data.scheduleEnd <= data.scheduleStart
        ) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                message: "End date must be after start date",
                path: ["scheduleEnd"],
            });
        }
    });

type CampaignFormData = z.infer<typeof campaignSchema>;

const PageCreateCampaign: FC = () => {
    const navigate = useNavigate();
    const { campaignId } = useParams<{ campaignId?: string }>();
    const [isEditMode, setIsEditMode] = useState(false);
    const [isLoadingCampaign, setIsLoadingCampaign] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [responseStatus, setResponseStatus] = useState<"success" | "error" | undefined>(
        undefined,
    );
    const [responseTitle, setResponseTitle] = useState("");
    const [responseMessage, setResponseMessage] = useState("");

    // Form fields
    const [title, setTitle] = useState("");
    const [brief, setBrief] = useState("");
    const [budgetInTon, setBudgetInTon] = useState("");
    const [minSubscribers, setMinSubscribers] = useState("");
    const [minAvgViews, setMinAvgViews] = useState("");
    const [targetLanguages, setTargetLanguages] = useState("");
    const [scheduleStart, setScheduleStart] = useState("");
    const [scheduleEnd, setScheduleEnd] = useState("");

    // Field-level errors
    const [errors, setErrors] = useState<Partial<Record<keyof CampaignFormData, string>>>({});

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        if (isEditMode) {
            navigate(`/campaign/${campaignId}`);
        } else {
            navigate("/create");
        }
    }, [navigate, isEditMode, campaignId]);

    const validateForm = (): CampaignFormData | null => {
        try {
            // Prepare form data for validation
            const formData = {
                title: title,
                brief: brief,
                budgetInTon: budgetInTon ? parseFloat(budgetInTon) : 0,
                minSubscribers: minSubscribers ? parseInt(minSubscribers) : undefined,
                minAvgViews: minAvgViews ? parseInt(minAvgViews) : undefined,
                targetLanguages: targetLanguages || undefined,
                scheduleStart: scheduleStart ? new Date(scheduleStart) : undefined,
                scheduleEnd: scheduleEnd ? new Date(scheduleEnd) : undefined,
            };

            // Validate with Zod
            const validatedData = campaignSchema.parse(formData);
            setErrors({}); // Clear errors on successful validation
            return validatedData;
        } catch (error) {
            if (error instanceof z.ZodError) {
                // Map all errors to their respective fields
                const newErrors: Partial<Record<keyof CampaignFormData, string>> = {};
                error.issues.forEach((err: z.ZodIssue) => {
                    const path = err.path[0] as keyof CampaignFormData;
                    newErrors[path] = err.message;
                });
                setErrors(newErrors);
                invokeHapticFeedbackImpact("light");
            } else {
                setErrors({});
            }
            return null;
        }
    };

    const handleSubmit = async () => {
        const validatedData = validateForm();
        if (!validatedData) {
            return;
        }

        invokeHapticFeedbackImpact("medium");
        setIsSubmitting(true);

        try {
            const payload = {
                title: validatedData.title,
                brief: validatedData.brief,
                budgetInTon: validatedData.budgetInTon,
                minSubscribers: validatedData.minSubscribers,
                minAvgViews: validatedData.minAvgViews,
                targetLanguages: validatedData.targetLanguages,
                scheduleStart: validatedData.scheduleStart?.toISOString(),
                scheduleEnd: validatedData.scheduleEnd?.toISOString(),
            };

            const endpoint = isEditMode ? `/campaigns/${campaignId}` : "/campaigns";
            const method = isEditMode ? "PUT" : "POST";
            const result = await requestAPI(endpoint, payload, method, true);

            // Check if result is an error
            const isError =
                !result ||
                result === false ||
                result === null ||
                (typeof result === "object" && "detail" in result) ||
                (typeof result === "object" &&
                    "status" in result &&
                    typeof result.status === "number" &&
                    result.status >= 400);

            if (result && !isError) {
                let campaignTitle = "";
                let campaignBudget = 0;

                if (isEditMode) {
                    // PUT response has different structure
                    const updateData = result as UpdateCampaignResponse;
                    campaignTitle = updateData.title;
                    campaignBudget = updateData.budgetInTon;

                    // Store the updated campaign
                    sessionStorage.setItem("lastUpdatedCampaign", JSON.stringify(updateData));

                    console.log("Campaign updated successfully:", updateData);
                } else {
                    // POST response
                    const data = result as CreateCampaignResponse;
                    campaignTitle = data.title;
                    campaignBudget = data.budgetInTon;

                    // Store the created campaign
                    sessionStorage.setItem("lastCreatedCampaign", JSON.stringify(data));

                    console.log("Campaign created successfully:", data);
                }

                setResponseStatus("success");
                setResponseTitle(isEditMode ? "Campaign Updated!" : "Campaign Created!");
                setResponseMessage(
                    isEditMode
                        ? `${campaignTitle} has been successfully updated with a budget of ${campaignBudget} TON`
                        : `${campaignTitle} has been successfully created with a budget of ${campaignBudget} TON`,
                );
            } else {
                console.error(`Failed to ${isEditMode ? "update" : "create"} campaign`, result);

                setResponseStatus("error");
                setResponseTitle(
                    isEditMode ? "Failed to Update Campaign" : "Failed to Create Campaign",
                );
                setResponseMessage(
                    result && typeof result === "object" && "detail" in result
                        ? result.detail
                        : `Unable to ${isEditMode ? "update" : "create"} the campaign. Please check your input and try again.`,
                );
            }
        } catch (error) {
            console.error(`Error ${isEditMode ? "updating" : "creating"} campaign:`, error);

            setResponseStatus("error");
            setResponseTitle("Connection Error");
            setResponseMessage("Failed to connect to the server. Please try again later.");
        } finally {
            setIsSubmitting(false);
        }
    };

    useEffect(() => {
        const fetchCampaignForEdit = async () => {
            if (campaignId) {
                setIsEditMode(true);
                setIsLoadingCampaign(true);
                try {
                    const data = await requestAPI(`/campaigns/${campaignId}`, {}, "GET");
                    if (data && data !== false && data !== null) {
                        // Populate form with existing data
                        setTitle(data.title || "");
                        setBrief(data.brief || "");
                        setBudgetInTon(data.budgetInTon?.toString() || "");
                        setMinSubscribers(data.minSubscribers?.toString() || "");
                        setMinAvgViews(data.minAvgViews?.toString() || "");
                        setTargetLanguages(data.targetLanguages || "");

                        // Format dates for input fields
                        if (data.scheduleStart) {
                            const startDate = new Date(data.scheduleStart);
                            setScheduleStart(startDate.toISOString().slice(0, 16));
                        }
                        if (data.scheduleEnd) {
                            const endDate = new Date(data.scheduleEnd);
                            setScheduleEnd(endDate.toISOString().slice(0, 16));
                        }
                    }
                } catch (error) {
                    console.error("Error fetching campaign for edit:", error);
                } finally {
                    setIsLoadingCampaign(false);
                }
            }
        };

        fetchCampaignForEdit();

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
    }, [onBackButton, campaignId]);

    return (
        <>
            <div id="container-page-create-campaign">
                <div className="content-wrapper">
                    <div className="lottie-container animate__animated animate__fadeIn">
                        <LottiePlayer
                            src={lottieAnimations.duck_sale.url}
                            fallback={<span>{lottieAnimations.duck_sale.emoji}</span>}
                            autoplay
                            loop
                        />
                    </div>

                    {isLoadingCampaign ? (
                        <div className="loading-state animate__animated animate__fadeIn">
                            <h2>Loading campaign...</h2>
                        </div>
                    ) : (
                        <>
                            <div className="text-container animate__animated animate__fadeIn">
                                <h1>{isEditMode ? "Edit Campaign" : "Create Campaign"}</h1>
                                <p>
                                    {isEditMode
                                        ? "Update your campaign details below."
                                        : "Fill in the details to launch your advertising campaign."}
                                </p>
                            </div>

                            <div className="form-container animate__animated animate__fadeIn">
                                <div className="form-group">
                                    <label htmlFor="campaign-title">Campaign Title *</label>
                                    <input
                                        id="campaign-title"
                                        type="text"
                                        placeholder="Enter campaign title"
                                        value={title}
                                        onChange={(e) => setTitle(e.target.value)}
                                        disabled={isSubmitting}
                                        className={errors.title ? "error" : ""}
                                    />
                                    {errors.title && (
                                        <span className="error-message">{errors.title}</span>
                                    )}
                                </div>

                                <div className="form-group">
                                    <label htmlFor="campaign-brief">
                                        Brief / Description *
                                    </label>
                                    <textarea
                                        id="campaign-brief"
                                        placeholder="Describe your campaign..."
                                        value={brief}
                                        onChange={(e) => setBrief(e.target.value)}
                                        disabled={isSubmitting}
                                        rows={3}
                                        className={errors.brief ? "error" : ""}
                                    />
                                    {errors.brief && (
                                        <span className="error-message">{errors.brief}</span>
                                    )}
                                </div>

                                <div className="form-group">
                                    <label htmlFor="campaign-budget">Budget (TON) *</label>
                                    <input
                                        id="campaign-budget"
                                        type="number"
                                        placeholder="0.00"
                                        value={budgetInTon}
                                        onChange={(e) => setBudgetInTon(e.target.value)}
                                        disabled={isSubmitting}
                                        min="0"
                                        step="0.01"
                                        className={errors.budgetInTon ? "error" : ""}
                                    />
                                    {errors.budgetInTon && (
                                        <span className="error-message">
                                            {errors.budgetInTon}
                                        </span>
                                    )}
                                </div>

                                <div className="form-row">
                                    <div className="form-group">
                                        <label htmlFor="min-subscribers">Min Subscribers</label>
                                        <input
                                            id="min-subscribers"
                                            type="number"
                                            placeholder="1000"
                                            value={minSubscribers}
                                            onChange={(e) => setMinSubscribers(e.target.value)}
                                            disabled={isSubmitting}
                                            min="0"
                                            className={errors.minSubscribers ? "error" : ""}
                                        />
                                        {errors.minSubscribers && (
                                            <span className="error-message">
                                                {errors.minSubscribers}
                                            </span>
                                        )}
                                    </div>

                                    <div className="form-group">
                                        <label htmlFor="min-avg-views">Min Avg Views</label>
                                        <input
                                            id="min-avg-views"
                                            type="number"
                                            placeholder="500"
                                            value={minAvgViews}
                                            onChange={(e) => setMinAvgViews(e.target.value)}
                                            disabled={isSubmitting}
                                            min="0"
                                            className={errors.minAvgViews ? "error" : ""}
                                        />
                                        {errors.minAvgViews && (
                                            <span className="error-message">
                                                {errors.minAvgViews}
                                            </span>
                                        )}
                                    </div>
                                </div>

                                <div className="form-group">
                                    <label htmlFor="target-languages">Target Languages</label>
                                    <input
                                        id="target-languages"
                                        type="text"
                                        placeholder="e.g., English, Spanish"
                                        value={targetLanguages}
                                        onChange={(e) => setTargetLanguages(e.target.value)}
                                        disabled={isSubmitting}
                                        className={errors.targetLanguages ? "error" : ""}
                                    />
                                    {errors.targetLanguages && (
                                        <span className="error-message">
                                            {errors.targetLanguages}
                                        </span>
                                    )}
                                </div>

                                <div className="form-row form-row-dates">
                                    <div className="form-group">
                                        <label htmlFor="schedule-start">Start Date *</label>
                                        <input
                                            id="schedule-start"
                                            type="datetime-local"
                                            value={scheduleStart}
                                            onChange={(e) => setScheduleStart(e.target.value)}
                                            disabled={isSubmitting}
                                            className={errors.scheduleStart ? "error" : ""}
                                        />
                                        {errors.scheduleStart && (
                                            <span className="error-message">
                                                {errors.scheduleStart}
                                            </span>
                                        )}
                                    </div>

                                    <div className="form-group">
                                        <label htmlFor="schedule-end">End Date *</label>
                                        <input
                                            id="schedule-end"
                                            type="datetime-local"
                                            value={scheduleEnd}
                                            onChange={(e) => setScheduleEnd(e.target.value)}
                                            disabled={isSubmitting}
                                            className={errors.scheduleEnd ? "error" : ""}
                                        />
                                        {errors.scheduleEnd && (
                                            <span className="error-message">
                                                {errors.scheduleEnd}
                                            </span>
                                        )}
                                    </div>
                                </div>

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
                                              ? "Update Campaign"
                                              : "Create Campaign"}
                                    </span>
                                </button>
                            </div>
                        </>
                    )}
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
                        if (isEditMode) {
                            navigate(`/campaign/${campaignId}`);
                        } else {
                            navigate("/profile");
                        }
                    }}
                />
            )}
        </>
    );
};

export default PageCreateCampaign;
