import {
    init,
    initData,
    isTMA,
    miniApp,
    on,
    themeParams,
    useLaunchParams,
    viewport,
} from "@tma.js/sdk-react";
import { isVersionAtLeast, postEvent } from "@/shared/utils/telegram";
import { useEffect, useState } from "react";

import { Flip } from "gsap/all";
import ModalSettings from "@/core/modals/Settings";
import PageError from "@/core/pages/Error";
import { RouterProvider } from "react-router";
import gsap from "gsap";
import { preloadLottieAnimations } from "@/shared/utils/preload";
import { router } from "@/core/router";
import { useGSAP } from "@gsap/react";
import { useTranslation } from "@/core/i18n/i18nProvider";

// Initialize SDK and initData once, outside of component
if (isTMA()) {
    init();
    initData.restore();
}

const sendInitDataToBackend = async () => {
    try {
        const rawInitData = initData.raw();

        if (!rawInitData) {
            console.warn("No initData available");
            throw new Error("No initData available");
        }

        const response = await fetch(
            `${import.meta.env.VITE_BACKEND_BASE_URL}/identity/login`,
            {
                method: "POST",
                headers: {
                    Authorization: rawInitData,
                    Accept: "*/*",
                },
            },
        );

        if (response.ok) {
            const data = await response.json();
            console.log("InitData sent successfully", data);

            // Store JWT token in localStorage
            if (data.token) {
                const jwt = data.token;
                localStorage.setItem("jwt", jwt);
                console.log("JWT token stored in localStorage");
            }

            return data;
        } else {
            console.error("Failed to send initData:", response.statusText);
            throw new Error(`Authentication failed: ${response.statusText}`);
        }
    } catch (error) {
        console.error("Error sending initData:", error);
        throw error;
    }
};

const handleTheme = (isDark: boolean) => {
    document.body.setAttribute("data-theme", isDark ? "dark" : "light");

    const headerColor = isDark
        ? getComputedStyle(document.documentElement).getPropertyValue("--black").trim()
        : getComputedStyle(document.documentElement).getPropertyValue("--white").trim();

    postEvent("web_app_set_header_color", {
        color: headerColor,
    });

    postEvent("web_app_set_background_color", {
        color: headerColor,
    });

    postEvent("web_app_set_bottom_bar_color", {
        color: headerColor,
    });
};

const App = () => {
    const [settingsModal, setSettingsModal] = useState(false);
    const [authError, setAuthError] = useState(false);
    const [isAuthenticating, setIsAuthenticating] = useState(true);

    if (isTMA()) {
        const lp = useLaunchParams();

        const initializeTMA = async () => {
            postEvent("web_app_ready");
            postEvent("iframe_ready");
            postEvent("web_app_expand");

            // Send initData to backend
            try {
                const data = await sendInitDataToBackend();
                console.log(data);
                setIsAuthenticating(false);
            } catch (error) {
                console.error("Authentication failed:", error);
                setAuthError(true);
                setIsAuthenticating(false);
                return;
            }

            postEvent("web_app_setup_back_button", {
                is_visible: false,
            });

            postEvent("web_app_setup_main_button", {
                is_visible: false,
            });

            postEvent("web_app_setup_secondary_button", {
                is_visible: false,
            });

            postEvent("web_app_setup_settings_button", {
                is_visible: false,
            });

            if (viewport.mount.isAvailable() && !viewport.isMounted()) {
                await viewport.mount();
                viewport.bindCssVars();

                // Expand viewport when keyboard opens to prevent overlap
                if (viewport.expand.isAvailable()) {
                    viewport.expand();
                }
            }

            if (miniApp.mount.isAvailable() && !miniApp.isMounted()) {
                miniApp.mount();
                miniApp.bindCssVars();

                handleTheme(miniApp.isDark());
                miniApp.isDark.sub(handleTheme);
            }

            if (themeParams.mount.isAvailable() && !themeParams.isMounted()) {
                themeParams.mount();
                themeParams.bindCssVars();
            }

            setTimeout(() => {
                const persistVariables = [
                    "tg-viewport-height",
                    "tg-viewport-safe-area-inset-top",
                    "tg-viewport-content-safe-area-inset-top",
                    "tg-viewport-safe-area-inset-bottom",
                    "tg-viewport-content-safe-area-inset-bottom",
                ];

                for (const name of persistVariables) {
                    (document.querySelector(":root") as HTMLElement).style.setProperty(
                        `--p${name}`,
                        (document.querySelector(":root") as HTMLElement).style.getPropertyValue(
                            `--${name}`,
                        ),
                    );
                }
            });

            if (isVersionAtLeast("6.1", lp.tgWebAppVersion)) {
                postEvent("web_app_setup_settings_button", {
                    is_visible: true,
                });

                on("settings_button_pressed", () => {
                    setSettingsModal(true);
                });
            }

            if (isVersionAtLeast("6.2", lp.tgWebAppVersion)) {
                postEvent("web_app_setup_closing_behavior", {
                    need_confirmation: false,
                });
            }

            if (isVersionAtLeast("7.7", lp.tgWebAppVersion)) {
                postEvent("web_app_setup_swipe_behavior", {
                    allow_vertical_swipe: true,
                });
            }

            if (isVersionAtLeast("8.0", lp.tgWebAppVersion)) {
                postEvent("web_app_toggle_orientation_lock", {
                    locked: true,
                });

                if (["ios", "android"].includes(lp.tgWebAppPlatform.toLowerCase())) {
                    postEvent("web_app_request_fullscreen");
                }
            }
        };

        gsap.registerPlugin(useGSAP, Flip);

        useEffect(() => {
            initializeTMA();

            document.addEventListener("contextmenu", (event) => {
                event.preventDefault();
            });

            return () => {
                miniApp.isDark.unsub(handleTheme);
            };
        }, []);

        setTimeout(() => {
            preloadLottieAnimations();
            import("@tonconnect/ui");
        }, 5e3);

        if (authError) {
            const { t } = useTranslation();
            return (
                <PageError
                    title={t("pages.errorInvalidEnv.title")}
                    description="Failed to authenticate with Telegram. Please restart the app."
                />
            );
        }

        if (isAuthenticating) {
            return null; // Or a loading spinner if you have one
        }

        return (
            <>
                <RouterProvider router={router} />
                <div id="container-modals" />
                <ModalSettings isOpen={settingsModal} setOpen={setSettingsModal} />
            </>
        );
    }

    const { t } = useTranslation();

    return (
        <PageError
            title={t("pages.errorInvalidEnv.title")}
            description={t("pages.errorInvalidEnv.description")}
        />
    );
};

export default App;
