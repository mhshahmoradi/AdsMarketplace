import "../styles/CreateChannel.scss";

import { useCallback, useEffect, useState, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import LottiePlayer from "@/shared/components/LottiePlayer";
import { lottieAnimations } from "@/shared/utils/lottie";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate } from "react-router";
import { off, on } from "@tma.js/sdk-react";
import ChannelResponseOverlay from "@/shared/components/ChannelResponseOverlay";
import { requestAPI } from "@/shared/utils/api";

type AddChannelResponse = {
    channelId: string;
    tgChannelId: number;
    username: string;
    title: string;
    status: string;
};

const PageAddChannel: FC = () => {
    const navigate = useNavigate();
    const [channelUsername, setChannelUsername] = useState("@");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [responseStatus, setResponseStatus] = useState<"success" | "error" | undefined>(
        undefined,
    );
    const [responseTitle, setResponseTitle] = useState("");
    const [responseMessage, setResponseMessage] = useState("");

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate("/create");
    }, [navigate]);

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value;
        const normalizedUsername = value.replace(/^@+/, "");
        setChannelUsername(`@${normalizedUsername}`);
    };

    const handleSubmit = async () => {
        if (!channelUsername || channelUsername === "@") {
            setResponseStatus("error");
            setResponseTitle("Invalid Input");
            setResponseMessage("Please enter a valid channel username");
            return;
        }

        invokeHapticFeedbackImpact("medium");
        setIsSubmitting(true);
        1;
        try {
            // Remove @ before sending to backend
            const usernameToSend = channelUsername.startsWith("@")
                ? channelUsername.slice(1)
                : channelUsername;

            const result = await requestAPI(
                "/channels",
                {
                    username: usernameToSend,
                },
                "POST",
                true,
            );

            // Check if result is an error (has detail field or numeric status)
            const isError =
                !result ||
                result === false ||
                result === null ||
                (typeof result === "object" && "detail" in result) ||
                (typeof result === "object" &&
                    "status" in result &&
                    typeof result.status === "number");

            if (result && !isError) {
                const data = result as AddChannelResponse;
                console.log("Channel added successfully:", data);

                setResponseStatus("success");
                setResponseTitle("Channel Added!");
                setResponseMessage(
                    `${data.title || data.username} has been successfully added to your dashboard`,
                );

                // Store channel data for details page
                sessionStorage.setItem("lastAddedChannel", JSON.stringify(data));
            } else {
                console.error("Failed to add channel", result);

                setResponseStatus("error");
                setResponseTitle("Failed to Add Channel");
                setResponseMessage(
                    result && typeof result === "object" && "detail" in result
                        ? result.detail
                        : "Unable to add the channel. Please make sure you are the owner of said channel",
                );
            }
        } catch (error) {
            console.error("Error adding channel:", error);

            setResponseStatus("error");
            setResponseTitle("Connection Error");
            setResponseMessage("Failed to connect to the server. Please try again later.");
        } finally {
            setIsSubmitting(false);
        }
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
            <div id="container-page-create-channel">
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
                        <h1>Add Your Channel</h1>
                        <p>
                            Add your channel here, afterwards you'll have access to your
                            dashboard
                        </p>
                    </div>

                    <div className="input-container animate__animated animate__fadeIn">
                        <label htmlFor="channel-username">
                            <input
                                id="channel-username"
                                type="text"
                                placeholder="@channel_username"
                                value={channelUsername}
                                onChange={handleInputChange}
                                disabled={isSubmitting}
                            />
                        </label>
                    </div>

                    <button
                        className={`btn-add ${isSubmitting ? "disabled" : ""}`}
                        onClick={handleSubmit}
                        disabled={isSubmitting}
                    >
                        <span>{isSubmitting ? "Adding..." : "Add Channel"}</span>
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
                        const channelData = sessionStorage.getItem("lastAddedChannel");
                        if (channelData) {
                            const channel = JSON.parse(channelData);
                            // Use channelId from Add Channel API response
                            navigate(`/channel/${channel.channelId || channel.id}`);
                        }
                    }}
                />
            )}
        </>
    );
};

export default PageAddChannel;
