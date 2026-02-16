import "./Settings.scss";
import { memo, useMemo, useState, type Dispatch, type FC, type SetStateAction } from "react";

import { IoClose } from "react-icons/io5";
import { invokeHapticFeedbackImpact } from "@/shared/utils/telegram";

import { Drawer } from "vaul";
import { useSettingsStore } from "@/shared/stores/useSettingsStore";
import { useTranslation } from "@/core/i18n/i18nProvider";

import PaymentOverlay from "@/shared/components/PaymentOverlay";

const ModalSettings: FC<{
    isOpen: boolean;
    setOpen: Dispatch<SetStateAction<boolean>>;
}> = memo(({ isOpen, setOpen }) => {
    const { t } = useTranslation();
    const { settings } = useSettingsStore();

    const [paymentOverlay, setPaymentOverlay] = useState<"success" | "failed" | undefined>(
        undefined,
    );

    const renderPaymentOverlay = useMemo(() => {
        if (!paymentOverlay) return;
        return <PaymentOverlay status={paymentOverlay} setStatus={setPaymentOverlay} />;
    }, [paymentOverlay]);

    const renderContent = useMemo(() => {
        return (
            <div className="container-modal-settings">
                <span
                    className="btn-close-modal"
                    onClick={() => {
                        invokeHapticFeedbackImpact("light");
                        setOpen(false);
                    }}
                >
                    <IoClose />
                </span>

                <div>
                    <header>
                        <h2>{t("modals.settings.title")}</h2>
                    </header>
                    <section>We'll be back in order pretty soon.</section>
                </div>
            </div>
        );
    }, [settings, t]);

    return (
        <>
            <Drawer.Root open={isOpen} onOpenChange={setOpen}>
                <Drawer.Portal>
                    <Drawer.Overlay className="vaul-overlay" style={{ zIndex: "10005" }} />
                    <Drawer.Content
                        className="vaul-content"
                        style={{ zIndex: "10005" }}
                        aria-describedby={undefined}
                    >
                        <Drawer.Title style={{ display: "none" }}>
                            {t("modals.settings.title")}
                        </Drawer.Title>

                        <div>{renderContent}</div>
                    </Drawer.Content>
                </Drawer.Portal>
            </Drawer.Root>

            <>{renderPaymentOverlay}</>
        </>
    );
});

export default ModalSettings;
