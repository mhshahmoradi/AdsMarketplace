import "../styles/Profile.scss";

import { useState, useMemo, useEffect, useCallback, type FC } from "react";
import { useNavigate } from "react-router";
import { useLaunchParams } from "@tma.js/sdk-react";
import {
    TonConnectButton,
    useIsConnectionRestored,
    useTonAddress,
    useTonConnectUI,
    useTonWallet,
} from "@tonconnect/ui-react";
import { Drawer } from "vaul";
import BottomBar from "@/shared/components/BottomBar";
import ChannelResponseOverlay from "@/shared/components/ChannelResponseOverlay";
import LottiePlayer from "@/shared/components/LottiePlayer";
import { FaCircleUser } from "react-icons/fa6";
import { IoClose } from "react-icons/io5";
import { invokeHapticFeedbackImpact } from "@/shared/utils/telegram";
import { requestAPI } from "@/shared/utils/api";
import { lottieAnimations } from "@/shared/utils/lottie";
import { sortByRecencyDesc } from "@/shared/utils/sort";
import { type Campaign } from "@/shared/types/campaign";
import type { Channel } from "@/shared/types/channel";
import {
    ChannelListItem,
    CampaignListItem,
    LoadingState,
    EmptyState,
    ErrorState,
} from "@/shared/components/list";
import { useListStateStore } from "@/shared/stores/useListStateStore";

type TabType = "channels" | "campaigns";

type WalletBalanceResponse = {
    balance: number;
    updatedAt: string;
};

const MIN_WITHDRAW_AMOUNT = 0.1;

const PageProfile: FC = () => {
    const lp = useLaunchParams();
    const navigate = useNavigate();
    const activeTab = useListStateStore((state) => state.profileActiveTab);
    const [tonConnectUI] = useTonConnectUI();
    const wallet = useTonWallet();
    const userFriendlyAddress = useTonAddress();
    const isConnectionRestored = useIsConnectionRestored();

    const [loading, setLoading] = useState(false);
    const [channels, setChannels] = useState<Channel[]>([]);
    const [campaigns, setCampaigns] = useState<Campaign[]>([]);
    const [error, setError] = useState(false);
    const [walletBalance, setWalletBalance] = useState(0);
    const [walletBalanceUpdatedAt, setWalletBalanceUpdatedAt] = useState("");
    const [walletBalanceLoading, setWalletBalanceLoading] = useState(false);
    const [walletBalanceError, setWalletBalanceError] = useState("");
    const [withdrawModalOpen, setWithdrawModalOpen] = useState(false);
    const [withdrawAmount, setWithdrawAmount] = useState(0);
    const [customAmountModalOpen, setCustomAmountModalOpen] = useState(false);
    const [customAmountInput, setCustomAmountInput] = useState("");
    const [customAmountError, setCustomAmountError] = useState("");
    const [withdrawing, setWithdrawing] = useState(false);
    const [responseStatus, setResponseStatus] = useState<"success" | "error" | undefined>();
    const [responseTitle, setResponseTitle] = useState("");
    const [responseMessage, setResponseMessage] = useState("");

    const handleTabChange = (tab: TabType) => {
        if (tab !== activeTab) {
            invokeHapticFeedbackImpact("light");
            useListStateStore.getState().setProfileActiveTab(tab);
        }
    };

    const handleChannelClick = useCallback(
        (channelId: string) => {
            invokeHapticFeedbackImpact("light");
            navigate(`/channel/${channelId}`);
        },
        [navigate],
    );

    const handleCampaignClick = useCallback(
        (campaignId: string) => {
            invokeHapticFeedbackImpact("light");
            navigate(`/campaign/${campaignId}`);
        },
        [navigate],
    );

    const walletName = useMemo(() => {
        if (!wallet) {
            return "";
        }
        if ("name" in wallet && wallet.name) {
            return wallet.name;
        }
        return wallet.device.appName || "TON Wallet";
    }, [wallet]);

    const shortWalletAddress = useMemo(() => {
        if (!userFriendlyAddress) {
            return "";
        }
        if (userFriendlyAddress.length <= 12) {
            return userFriendlyAddress;
        }
        return `${userFriendlyAddress.slice(0, 6)}...${userFriendlyAddress.slice(-4)}`;
    }, [userFriendlyAddress]);

    const walletStatusText = useMemo(() => {
        if (!wallet) {
            return "";
        }
        return `Connected to ${walletName}${shortWalletAddress ? ` (${shortWalletAddress})` : ""}.`;
    }, [wallet, walletName, shortWalletAddress]);

    const connectedWalletAddress = useMemo(() => {
        if (userFriendlyAddress) {
            return userFriendlyAddress;
        }
        if (wallet?.account?.address) {
            return wallet.account.address;
        }
        return "";
    }, [userFriendlyAddress, wallet]);

    const shortConnectedWalletAddress = useMemo(() => {
        if (!connectedWalletAddress) {
            return "";
        }
        if (connectedWalletAddress.length <= 18) {
            return connectedWalletAddress;
        }
        return `${connectedWalletAddress.slice(0, 10)}...${connectedWalletAddress.slice(-8)}`;
    }, [connectedWalletAddress]);

    const walletBalanceUpdatedText = useMemo(() => {
        if (!walletBalanceUpdatedAt) {
            return "";
        }
        const parsedDate = new Date(walletBalanceUpdatedAt);
        if (Number.isNaN(parsedDate.getTime())) {
            return "";
        }
        return parsedDate.toLocaleString();
    }, [walletBalanceUpdatedAt]);

    const formatTonValue = useCallback((value: number) => {
        const normalized = Number.isFinite(value) ? value : 0;
        return new Intl.NumberFormat("en-US", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        }).format(Math.max(0, normalized));
    }, []);

    const hasWithdrawableBalance = useMemo(
        () => walletBalance >= MIN_WITHDRAW_AMOUNT,
        [walletBalance],
    );

    const handleDisconnectWallet = useCallback(async () => {
        invokeHapticFeedbackImpact("light");
        try {
            await tonConnectUI.disconnect();
        } catch (err) {
            console.error("Failed to disconnect wallet:", err);
        }
    }, [tonConnectUI]);

    const handleReconnectWallet = useCallback(async () => {
        invokeHapticFeedbackImpact("light");
        try {
            if (wallet) {
                await tonConnectUI.disconnect();
            }
            await tonConnectUI.openModal();
        } catch (err) {
            console.error("Failed to reconnect wallet:", err);
        }
    }, [tonConnectUI, wallet]);

    const fetchWalletBalance = useCallback(async () => {
        if (!connectedWalletAddress) {
            return;
        }

        setWalletBalanceLoading(true);
        setWalletBalanceError("");

        try {
            const data = await requestAPI("/wallet/balance", {}, "GET");

            if (data && data !== false && data !== null && typeof data.balance === "number") {
                const walletData = data as WalletBalanceResponse;
                const normalizedBalance = Math.max(0, walletData.balance);
                setWalletBalance(normalizedBalance);
                setWalletBalanceUpdatedAt(walletData.updatedAt || "");
                setWithdrawAmount((prev) => {
                    if (prev <= 0) {
                        return normalizedBalance;
                    }
                    return Math.min(prev, normalizedBalance);
                });
            } else {
                setWalletBalanceError("Failed to load wallet balance.");
            }
        } catch (err) {
            console.error("Error fetching wallet balance:", err);
            setWalletBalanceError("Failed to load wallet balance.");
        } finally {
            setWalletBalanceLoading(false);
        }
    }, [connectedWalletAddress]);

    const handleOpenWithdrawModal = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        setWithdrawAmount(walletBalance);
        setCustomAmountModalOpen(false);
        setCustomAmountInput("");
        setCustomAmountError("");
        setWithdrawModalOpen(true);
    }, [walletBalance]);

    const handleSetMaxWithdraw = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        setWithdrawAmount(walletBalance);
    }, [walletBalance]);

    const handleOpenCustomAmountModal = useCallback(() => {
        if (!hasWithdrawableBalance || withdrawing) {
            return;
        }

        invokeHapticFeedbackImpact("light");
        const initialAmount = withdrawAmount > 0 ? withdrawAmount : walletBalance;
        setCustomAmountInput(initialAmount.toFixed(2));
        setCustomAmountError("");
        setCustomAmountModalOpen(true);
    }, [hasWithdrawableBalance, withdrawAmount, walletBalance, withdrawing]);

    const handleCloseCustomAmountModal = useCallback(() => {
        invokeHapticFeedbackImpact("light");
        setCustomAmountModalOpen(false);
        setCustomAmountError("");
    }, []);

    const handleSetCustomAmount = useCallback(() => {
        const normalizedInput = customAmountInput.replace(",", ".").trim();
        const parsedAmount = Number(normalizedInput);

        if (!normalizedInput || !Number.isFinite(parsedAmount)) {
            setCustomAmountError("Enter a valid TON amount.");
            return;
        }

        if (parsedAmount < MIN_WITHDRAW_AMOUNT) {
            setCustomAmountError(
                `Amount must be at least ${formatTonValue(MIN_WITHDRAW_AMOUNT)} TON.`,
            );
            return;
        }

        if (parsedAmount > walletBalance) {
            setCustomAmountError("Amount cannot be greater than your available balance.");
            return;
        }

        invokeHapticFeedbackImpact("light");
        setWithdrawAmount(Number(parsedAmount.toFixed(2)));
        setCustomAmountModalOpen(false);
        setCustomAmountError("");
    }, [customAmountInput, formatTonValue, walletBalance]);

    const handleWithdrawModalOpenChange = useCallback(
        (open: boolean) => {
            if (!open && customAmountModalOpen) {
                return;
            }

            setWithdrawModalOpen(open);
            if (!open) {
                setCustomAmountModalOpen(false);
                setCustomAmountInput("");
                setCustomAmountError("");
            }
        },
        [customAmountModalOpen],
    );

    const handleWithdrawSubmit = useCallback(async () => {
        if (!connectedWalletAddress) {
            setResponseStatus("error");
            setResponseTitle("Withdraw Failed");
            setResponseMessage("No connected wallet address found.");
            return;
        }

        if (withdrawAmount < MIN_WITHDRAW_AMOUNT || withdrawAmount > walletBalance) {
            setResponseStatus("error");
            setResponseTitle("Invalid Amount");
            setResponseMessage(
                `Select an amount between ${formatTonValue(MIN_WITHDRAW_AMOUNT)} TON and ${formatTonValue(walletBalance)} TON.`,
            );
            return;
        }

        setWithdrawing(true);
        invokeHapticFeedbackImpact("medium");

        try {
            const response = await requestAPI(
                "/wallet/withdraw",
                {
                    destinationAddress: connectedWalletAddress,
                    amount: Number(withdrawAmount.toFixed(2)),
                },
                "POST",
                true,
            );

            const responseErrorValue =
                typeof response === "object" && response !== null && "error" in response
                    ? (response as { error?: unknown }).error
                    : undefined;
            const hasResponseError =
                responseErrorValue !== undefined &&
                responseErrorValue !== null &&
                responseErrorValue !== false &&
                responseErrorValue !== "";

            const hasError =
                !response ||
                response === false ||
                (typeof response === "object" &&
                    response !== null &&
                    ("detail" in response ||
                        hasResponseError ||
                        ("status" in response &&
                            typeof response.status === "number" &&
                            response.status >= 400)));

            if (hasError) {
                const errorPayload =
                    typeof response === "object" && response !== null
                        ? (response as {
                              detail?: unknown;
                              message?: unknown;
                              error?: unknown;
                          })
                        : undefined;
                const detailMessage =
                    typeof errorPayload?.detail === "string" ? errorPayload.detail : "";
                const messageText =
                    typeof errorPayload?.message === "string" ? errorPayload.message : "";
                const errorText =
                    typeof errorPayload?.error === "string" ? errorPayload.error : "";

                setResponseStatus("error");
                setResponseTitle("Withdraw Failed");
                setResponseMessage(
                    detailMessage ||
                        messageText ||
                        errorText ||
                        "Could not submit your withdrawal request.",
                );
                return;
            }

            setWithdrawModalOpen(false);
            setResponseStatus("success");
            setResponseTitle("Withdraw Requested");
            setResponseMessage(
                `Your request to withdraw ${formatTonValue(withdrawAmount)} TON was submitted.`,
            );
            await fetchWalletBalance();
        } catch (err) {
            console.error("Error submitting withdraw request:", err);
            setResponseStatus("error");
            setResponseTitle("Withdraw Failed");
            setResponseMessage("An error occurred while submitting your request.");
        } finally {
            setWithdrawing(false);
        }
    }, [
        connectedWalletAddress,
        withdrawAmount,
        walletBalance,
        formatTonValue,
        fetchWalletBalance,
    ]);

    useEffect(() => {
        const fetchChannels = async () => {
            setLoading(true);
            setError(false);

            try {
                const data = await requestAPI("/channels?onlyMine=true", {}, "GET");

                if (data && data !== false && data !== null) {
                    if (data.items && Array.isArray(data.items)) {
                        const validChannels = data.items;
                        setChannels(sortByRecencyDesc(validChannels));
                    } else {
                        setError(true);
                    }
                } else {
                    setError(true);
                }
            } catch (err) {
                console.error("Error fetching channels:", err);
                setError(true);
            } finally {
                setLoading(false);
            }
        };

        fetchChannels();
        invokeHapticFeedbackImpact("medium");
    }, []);

    useEffect(() => {
        const fetchCampaigns = async () => {
            if (activeTab !== "campaigns") return;

            setLoading(true);
            setError(false);

            try {
                const data = await requestAPI("/campaigns/all?onlyMine=true", {}, "GET");

                if (data && data !== false && data !== null) {
                    if (data.items && Array.isArray(data.items)) {
                        const userCampaigns = data.items;
                        setCampaigns(sortByRecencyDesc(userCampaigns));
                    } else {
                        setError(true);
                    }
                } else {
                    setError(true);
                }
            } catch (err) {
                console.error("Error fetching campaigns:", err);
                setError(true);
            } finally {
                setLoading(false);
            }
        };

        fetchCampaigns();
    }, [activeTab]);

    useEffect(() => {
        if (!isConnectionRestored || !wallet || !connectedWalletAddress) {
            setWalletBalance(0);
            setWalletBalanceUpdatedAt("");
            setWalletBalanceError("");
            setWalletBalanceLoading(false);
            setWithdrawModalOpen(false);
            setWithdrawAmount(0);
            setCustomAmountModalOpen(false);
            setCustomAmountInput("");
            setCustomAmountError("");
            return;
        }

        fetchWalletBalance();
    }, [isConnectionRestored, wallet, connectedWalletAddress, fetchWalletBalance]);

    const renderContent = useMemo(() => {
        if (activeTab === "channels") {
            if (loading) {
                return <LoadingState count={4} />;
            }
            if (error) {
                return (
                    <ErrorState message="Failed to load channels. Please try again later." />
                );
            }
            if (channels.length === 0) {
                return (
                    <EmptyState message="No channels yet. Add your first channel to get started!" />
                );
            }

            return (
                <div className="profile-list-items">
                    {channels.map((channel) => (
                        <ChannelListItem
                            key={channel.id}
                            channel={channel}
                            onClick={() => handleChannelClick(channel.id)}
                        />
                    ))}
                </div>
            );
        }

        // Campaigns tab
        if (loading) {
            return <LoadingState count={4} />;
        }
        if (error) {
            return <ErrorState message="Failed to load campaigns. Please try again later." />;
        }
        if (campaigns.length === 0) {
            return <EmptyState message="No campaigns yet. Create your first campaign!" />;
        }

        return (
            <div className="profile-list-items">
                {campaigns.map((campaign) => (
                    <CampaignListItem
                        key={campaign.id}
                        campaign={campaign}
                        onClick={() => handleCampaignClick(campaign.id)}
                    />
                ))}
            </div>
        );
    }, [
        activeTab,
        loading,
        channels,
        campaigns,
        error,
        handleChannelClick,
        handleCampaignClick,
    ]);

    return (
        <>
            <div id="container-page-profile">
                <div className="profile-header animate__animated animate__fadeIn">
                    <div className="profile-picture">
                        {lp.tgWebAppData?.user?.photo_url ? (
                            <img
                                src={lp.tgWebAppData.user.photo_url}
                                alt={lp.tgWebAppData.user.first_name || "User"}
                            />
                        ) : (
                            <FaCircleUser />
                        )}
                    </div>

                    <h1>{lp.tgWebAppData?.user?.first_name || "User"}</h1>

                    <div className="wallet-connect-card">
                        {isConnectionRestored && !wallet && (
                            <>
                                <div className="wallet-disconnected-lottie">
                                    <LottiePlayer
                                        src={lottieAnimations.duck_safe.url}
                                        fallback={
                                            <span>{lottieAnimations.duck_safe.emoji}</span>
                                        }
                                        autoplay
                                        loop
                                    />
                                </div>
                                <p className="wallet-status disconnected">
                                    Connect your wallet to start making deals!
                                </p>
                            </>
                        )}

                        {isConnectionRestored && wallet && (
                            <p className="wallet-status connected">{walletStatusText}</p>
                        )}

                        <TonConnectButton className="wallet-connect-button" />

                        {isConnectionRestored && wallet && (
                            <div className="wallet-actions">
                                <button
                                    className="wallet-action secondary"
                                    onClick={handleDisconnectWallet}
                                >
                                    Disconnect
                                </button>
                                <button
                                    className="wallet-action"
                                    onClick={handleReconnectWallet}
                                >
                                    Reconnect
                                </button>
                            </div>
                        )}

                        {isConnectionRestored && wallet && (
                            <div className="wallet-balance-card">
                                <div className="wallet-balance-head">
                                    <h3>Wallet Balance</h3>
                                    <span className="wallet-balance-amount">
                                        {walletBalanceLoading
                                            ? "Loading..."
                                            : `${formatTonValue(walletBalance)} TON`}
                                    </span>
                                </div>

                                <div className="wallet-balance-meta">
                                    <span className="label">Wallet Address</span>
                                    <code title={connectedWalletAddress}>
                                        {shortConnectedWalletAddress}
                                    </code>
                                </div>

                                {walletBalanceUpdatedText && (
                                    <span className="wallet-balance-updated">
                                        Updated: {walletBalanceUpdatedText}
                                    </span>
                                )}

                                {walletBalanceError && (
                                    <span className="wallet-balance-error">
                                        {walletBalanceError}
                                    </span>
                                )}

                                <button
                                    className="wallet-withdraw-button"
                                    onClick={handleOpenWithdrawModal}
                                    disabled={
                                        walletBalanceLoading ||
                                        withdrawing ||
                                        !hasWithdrawableBalance ||
                                        !connectedWalletAddress
                                    }
                                >
                                    Withdraw
                                </button>
                            </div>
                        )}
                    </div>
                </div>

                <section>
                    <div id="container-tabs-profile">
                        <button
                            className={activeTab === "channels" ? "active" : ""}
                            onClick={() => handleTabChange("channels")}
                        >
                            <span>My Channels</span>
                        </button>
                        <button
                            className={activeTab === "campaigns" ? "active" : ""}
                            onClick={() => handleTabChange("campaigns")}
                        >
                            <span>My Campaigns</span>
                        </button>
                    </div>

                    {renderContent}
                </section>
            </div>
            <BottomBar />

            <Drawer.Root
                open={withdrawModalOpen}
                onOpenChange={handleWithdrawModalOpenChange}
                dismissible={!customAmountModalOpen}
            >
                <Drawer.Portal>
                    <Drawer.Overlay className="vaul-overlay" style={{ zIndex: "10005" }} />
                    <Drawer.Content
                        className="vaul-content"
                        style={{ zIndex: "10005" }}
                        aria-describedby={undefined}
                    >
                        <Drawer.Title style={{ display: "none" }}>
                            Withdraw Balance
                        </Drawer.Title>
                        <div>
                            <div className="container-modal-withdraw">
                                <span
                                    className="btn-close-modal"
                                    onClick={() => {
                                        invokeHapticFeedbackImpact("light");
                                        setWithdrawModalOpen(false);
                                    }}
                                >
                                    <IoClose />
                                </span>

                                <div className="withdraw-content">
                                    <header>
                                        <h2>Withdraw Balance</h2>
                                        <p>Request a payout to your connected wallet.</p>
                                    </header>

                                    <div className="withdraw-balance-row">
                                        <span>Available</span>
                                        <strong>{formatTonValue(walletBalance)} TON</strong>
                                    </div>

                                    <div className="withdraw-balance-row">
                                        <span>Destination</span>
                                        <code title={connectedWalletAddress}>
                                            {shortConnectedWalletAddress}
                                        </code>
                                    </div>

                                    <div className="withdraw-slider-block">
                                        <div className="withdraw-slider-head">
                                            <span>Amount</span>
                                            <button
                                                type="button"
                                                onClick={handleSetMaxWithdraw}
                                                disabled={
                                                    !hasWithdrawableBalance || withdrawing
                                                }
                                            >
                                                MAX
                                            </button>
                                        </div>

                                        <input
                                            type="range"
                                            min={
                                                hasWithdrawableBalance ? MIN_WITHDRAW_AMOUNT : 0
                                            }
                                            max={walletBalance}
                                            step={0.01}
                                            value={Math.min(withdrawAmount, walletBalance)}
                                            onChange={(event) =>
                                                setWithdrawAmount(
                                                    Math.min(
                                                        Math.max(
                                                            Number(event.currentTarget.value),
                                                            MIN_WITHDRAW_AMOUNT,
                                                        ),
                                                        walletBalance,
                                                    ),
                                                )
                                            }
                                            disabled={!hasWithdrawableBalance || withdrawing}
                                        />

                                        <div className="withdraw-slider-scale">
                                            <span>
                                                {hasWithdrawableBalance
                                                    ? `${formatTonValue(MIN_WITHDRAW_AMOUNT)} TON`
                                                    : "0 TON"}
                                            </span>
                                            <span>{formatTonValue(walletBalance)} TON</span>
                                        </div>
                                    </div>

                                    <button
                                        type="button"
                                        className="withdraw-selected-amount"
                                        onClick={handleOpenCustomAmountModal}
                                        disabled={!hasWithdrawableBalance || withdrawing}
                                    >
                                        <span>Selected</span>
                                        <strong>{formatTonValue(withdrawAmount)} TON</strong>
                                    </button>

                                    <button
                                        className="withdraw-submit-button"
                                        onClick={handleWithdrawSubmit}
                                        disabled={
                                            withdrawing ||
                                            withdrawAmount < MIN_WITHDRAW_AMOUNT ||
                                            withdrawAmount > walletBalance ||
                                            !connectedWalletAddress
                                        }
                                    >
                                        {withdrawing ? "Submitting..." : "Withdraw"}
                                    </button>
                                </div>

                                {customAmountModalOpen && (
                                    <div
                                        className="withdraw-custom-amount-overlay"
                                        onClick={handleCloseCustomAmountModal}
                                    >
                                        <div
                                            className="withdraw-custom-amount-dialog"
                                            onClick={(event) => event.stopPropagation()}
                                        >
                                            <h3>Set Custom Amount</h3>
                                            <p>
                                                Enter an amount between{" "}
                                                {formatTonValue(MIN_WITHDRAW_AMOUNT)} TON and{" "}
                                                {formatTonValue(walletBalance)} TON.
                                            </p>

                                            <div className="withdraw-custom-amount-field">
                                                <label htmlFor="profile-withdraw-custom-amount">
                                                    Amount (TON)
                                                </label>
                                                <input
                                                    id="profile-withdraw-custom-amount"
                                                    type="text"
                                                    inputMode="decimal"
                                                    value={customAmountInput}
                                                    onChange={(event) => {
                                                        setCustomAmountInput(
                                                            event.currentTarget.value,
                                                        );
                                                        if (customAmountError) {
                                                            setCustomAmountError("");
                                                        }
                                                    }}
                                                    placeholder={`${formatTonValue(MIN_WITHDRAW_AMOUNT)}`}
                                                    disabled={withdrawing}
                                                />
                                                {customAmountError && (
                                                    <span className="withdraw-custom-amount-error">
                                                        {customAmountError}
                                                    </span>
                                                )}
                                            </div>

                                            <div className="withdraw-custom-amount-actions">
                                                <button
                                                    type="button"
                                                    className="cancel-button"
                                                    onClick={handleCloseCustomAmountModal}
                                                    disabled={withdrawing}
                                                >
                                                    Cancel
                                                </button>
                                                <button
                                                    type="button"
                                                    className="confirm-button"
                                                    onClick={handleSetCustomAmount}
                                                    disabled={withdrawing}
                                                >
                                                    Set
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                        </div>
                    </Drawer.Content>
                </Drawer.Portal>
            </Drawer.Root>

            {responseStatus && (
                <ChannelResponseOverlay
                    status={responseStatus}
                    title={responseTitle}
                    message={responseMessage}
                    setStatus={setResponseStatus}
                />
            )}
        </>
    );
};

export default PageProfile;
