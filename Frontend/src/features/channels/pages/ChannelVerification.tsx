import "../styles/ChannelVerification.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import LottiePlayer from "@/shared/components/LottiePlayer";
import ChannelResponseOverlay from "@/shared/components/ChannelResponseOverlay";
import { lottieAnimations } from "@/shared/utils/lottie";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import { requestAPI } from "@/shared/utils/api";

const SUPPORT_PHONE_NUMBER = "+1 701 645 1204";
const SUPPORT_PHONE_NUMBER_FOR_COPY = SUPPORT_PHONE_NUMBER.replace(/\s+/g, "");
const PageChannelVerification: FC = () => {
    const navigate = useNavigate();
    const [isVerifying, setIsVerifying] = useState(false);
    const [modalStatus, setModalStatus] = useState<"success" | "error" | undefined>(undefined);
    const [modalTitle, setModalTitle] = useState("");
    const [modalMessage, setModalMessage] = useState("");

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate(-1);
    }, [navigate]);

    const handleAddBot = () => {
        invokeHapticFeedbackImpact("medium");
        const botUsername =
            import.meta.env.VITE_BOT_USERNAME?.replace("@", "") || "ads_ekhsh_bot";
        window.open(
            `https://t.me/${botUsername}?startchannel=true&admin=post_messages`,
            "_blank",
        );
    };

    const handleCopyPhoneNumber = async () => {
        invokeHapticFeedbackImpact("light");

        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(SUPPORT_PHONE_NUMBER_FOR_COPY);
                return;
            }

            const textArea = document.createElement("textarea");
            textArea.value = SUPPORT_PHONE_NUMBER_FOR_COPY;
            textArea.setAttribute("readonly", "");
            textArea.style.position = "absolute";
            textArea.style.left = "-9999px";
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand("copy");
            document.body.removeChild(textArea);
        } catch (error) {
            console.error("Failed to copy phone number:", error);
        }
    };

    const handleVerify = async () => {
        if (isVerifying) return;

        // Get channel data from sessionStorage
        const channelData = sessionStorage.getItem("channelToVerify");
        if (!channelData) {
            setModalStatus("error");
            setModalTitle("Error");
            setModalMessage("No channel information found. Please go back and try again.");
            return;
        }

        let channel;
        try {
            channel = JSON.parse(channelData);
        } catch (error) {
            setModalStatus("error");
            setModalTitle("Error");
            setModalMessage("Invalid channel data. Please go back and try again.");
            return;
        }

        if (!channel.id) {
            setModalStatus("error");
            setModalTitle("Error");
            setModalMessage("Channel ID not found. Please go back and try again.");
            return;
        }

        invokeHapticFeedbackImpact("medium");
        setIsVerifying(true);

        try {
            const response = await requestAPI(`/channels/${channel.id}/verify`, {}, "POST");

            if (response && response !== false && response !== null) {
                if (response.status === "Verified") {
                    setModalStatus("success");
                    setModalTitle("Channel Verified!");
                    setModalMessage(
                        "Your channel has been successfully verified. You can now use all features.",
                    );
                    // Clear the stored channel data
                    sessionStorage.removeItem("channelToVerify");
                } else if (response.title || response.detail) {
                    // API returned an error response
                    setModalStatus("error");
                    setModalTitle("Verification Failed");
                    setModalMessage(
                        response.detail ||
                            "The bot was not found in your channel. Please add the bot as an admin and try again.",
                    );
                } else {
                    // Unknown response format
                    setModalStatus("error");
                    setModalTitle("Verification Failed");
                    setModalMessage(
                        "Unable to verify your channel. Please make sure the bot and the user are added as an an admin",
                    );
                }
            } else {
                // Request failed
                setModalStatus("error");
                setModalTitle("Verification Failed");
                setModalMessage(
                    "Unable to verify your channel. Please check your connection and try again.",
                );
            }
        } catch (error) {
            console.error("Verification error:", error);
            setModalStatus("error");
            setModalTitle("Verification Failed");
            setModalMessage(
                "An error occurred while verifying your channel. Please try again later.",
            );
        } finally {
            setIsVerifying(false);
        }
    };

    const handleModalSuccess = () => {
        // Navigate back to channel details page
        navigate(-1);
    };

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

    return (
        <>
            <div id="container-page-channel-verification">
                <div className="content-wrapper">
                    <div className="lottie-container animate__animated animate__fadeIn">
                        <LottiePlayer
                            src={lottieAnimations.verify.url}
                            fallback={<span>{lottieAnimations.verify.emoji}</span>}
                            autoplay
                            loop
                        />
                    </div>

                    <div className="text-container animate__animated animate__fadeIn">
                        <h1>Whoops! You're not Verified</h1>
                        <p>Before verifying, complete these steps:</p>
                        <ol className="verification-steps">
                            <li>
                                <span className="step-number-box">1</span>
                                <span className="step-text">
                                    Add{" "}
                                    <strong>
                                        @{import.meta.env.VITE_BOT_USERNAME || "ads_ekhsh_bot"}
                                    </strong>{" "}
                                    to your channel as an admin.
                                </span>
                            </li>
                            <li>
                                <span className="step-number-box">2</span>
                                <span className="step-text">
                                    Add this user to your channel as an admin (No permission
                                    needed).
                                </span>
                            </li>
                        </ol>
                        <span className="verification-phone-number-wrap">
                            <span className="verification-phone-copy-hint">Tap to copy</span>
                            <button
                                type="button"
                                className="verification-phone-number"
                                onClick={() => void handleCopyPhoneNumber()}
                            >
                                {SUPPORT_PHONE_NUMBER}
                            </button>
                        </span>

                        <p className="verification-note">Then click verify below to confirm.</p>
                    </div>

                    <div className="buttons-container animate__animated animate__fadeIn">
                        <button className="btn-add-bot" onClick={handleAddBot}>
                            <span>Add Bot to Channel</span>
                        </button>

                        <button
                            className={`btn-verify ${isVerifying ? "disabled" : ""}`}
                            onClick={handleVerify}
                            disabled={isVerifying}
                        >
                            <span>{isVerifying ? "Verifying..." : "Verify Channel"}</span>
                        </button>
                    </div>
                </div>
            </div>

            {modalStatus && (
                <ChannelResponseOverlay
                    status={modalStatus}
                    title={modalTitle}
                    message={modalMessage}
                    setStatus={setModalStatus}
                    onSuccess={handleModalSuccess}
                />
            )}

            <BottomBar />
        </>
    );
};

export default PageChannelVerification;
