import "./ChannelResponseOverlay.scss";

import { useEffect, type Dispatch, type FC, type SetStateAction } from "react";

import LottiePlayer from "./LottiePlayer";
import { createPortal } from "react-dom";
import {
    invokeHapticFeedbackImpact,
    invokeHapticFeedbackNotification,
} from "@/shared/utils/telegram";
import { lottieAnimations } from "@/shared/utils/lottie";
import JSConfetti from "js-confetti";

const jsConfetti = new JSConfetti();

export type ChannelResponseOverlayProps = {
    status: "success" | "error";
    title: string;
    message: string;
    setStatus: Dispatch<SetStateAction<"success" | "error" | undefined>>;
    onSuccess?: () => void;
};

const ChannelResponseOverlay: FC<ChannelResponseOverlayProps> = ({
    status,
    title,
    message,
    setStatus,
    onSuccess,
}) => {
    const animation = lottieAnimations[status === "success" ? "confetti" : "duckForbidden"];

    useEffect(() => {
        invokeHapticFeedbackNotification(status === "success" ? "success" : "error");

        if (status === "success") {
            jsConfetti.addConfetti();
        }

        return () => {
            jsConfetti.clearCanvas();
        };
    }, [status]);

    return createPortal(
        <div id="container-channel-response-overlay">
            <LottiePlayer
                src={animation.url}
                fallback={<span>{animation.emoji}</span>}
                autoplay
            />

            <h1>{title}</h1>

            <span>{message}</span>

            <button
                onClick={() => {
                    invokeHapticFeedbackImpact("medium");
                    setStatus(undefined);
                    if (status === "success" && onSuccess) {
                        onSuccess();
                    }
                }}
            >
                {status === "success" ? "Continue" : "Try Again"}
            </button>
        </div>,
        document.body,
    );
};

export default ChannelResponseOverlay;
