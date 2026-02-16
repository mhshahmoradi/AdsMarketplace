import { TonConnectUIProvider } from "@tonconnect/ui-react";

import "./scss/tailwind.css";
import "./scss/app.scss";

import App from "./App";
import { I18nProvider } from "@/core/i18n/i18nProvider";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <TonConnectUIProvider manifestUrl="https://adsbot.pages.dev/tonconnect-manifest.json">
            <I18nProvider>
                <App />
            </I18nProvider>
        </TonConnectUIProvider>
    </StrictMode>,
);
