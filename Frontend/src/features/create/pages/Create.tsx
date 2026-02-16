import "../styles/Create.scss";

import { memo, useCallback, useEffect, type FC } from "react";

import BottomBar from "@/shared/components/BottomBar";
import LottiePlayer from "@/shared/components/LottiePlayer";
import { lottieAnimations } from "@/shared/utils/lottie";
import { invokeHapticFeedbackImpact, postEvent } from "@/shared/utils/telegram";
import { useNavigate } from "react-router";
import { off, on } from "@tma.js/sdk-react";

const CreateOption: FC<{
    title: string;
    onClick: () => void;
    lottieKey: keyof typeof lottieAnimations;
}> = memo(({ title, onClick, lottieKey }) => {
    const animation = lottieAnimations[lottieKey];

    return (
        <div className="container-create-option" onClick={onClick}>
            <LottiePlayer
                src={animation.url}
                fallback={<span>{animation.emoji}</span>}
                autoplay
                loop
            />
            <h2>{title}</h2>
        </div>
    );
});

const PageCreate: FC = () => {
    const navigate = useNavigate();

    const onBackButton = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        navigate("/");
    }, [navigate]);

    const handleAddChannel = () => {
        invokeHapticFeedbackImpact("medium");
        navigate("/add-channel");
    };

    const handlePostCampaign = () => {
        invokeHapticFeedbackImpact("medium");
        navigate("/create-campaign");
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
            <div id="container-page-create">
                <CreateOption
                    title="Add your channel"
                    onClick={handleAddChannel}
                    lottieKey="charts"
                />
                <CreateOption
                    title="Post a campaign"
                    onClick={handlePostCampaign}
                    lottieKey="megaPhone"
                />
            </div>
            <BottomBar />
        </>
    );
};

export default PageCreate;
