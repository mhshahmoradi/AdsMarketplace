import "../styles/Home.scss";
import { useState, useMemo, useCallback, useEffect, useRef } from "react";
import { useNavigate } from "react-router";
import { invokeHapticFeedbackImpact } from "@/shared/utils/telegram";
import BottomBar from "@/shared/components/BottomBar";
import { requestAPI } from "@/shared/utils/api";
import { sortByRecencyDesc } from "@/shared/utils/sort";
import CampaignFilters, {
    type CampaignFilterParams,
} from "@/shared/components/CampaignFilters";
import ListingFilters, { type ListingFilterParams } from "@/shared/components/ListingFilters";
import { type Campaign } from "@/shared/types/campaign";
import { type Listing } from "@/shared/types/listing";
import {
    ListingListItem,
    CampaignListItem,
    VirtualizedList,
    LoadingState,
    EmptyState,
    ErrorState,
} from "@/shared/components/list";
import { useListStateStore } from "@/shared/stores/useListStateStore";

type TabType = "listings" | "campaigns";

function PageHome() {
    const activeTab = useListStateStore((state) => state.homeActiveTab);

    const [fetching, setFetching] = useState(false);
    const [listings, setListings] = useState<Listing[]>([]);
    const [campaigns, setCampaigns] = useState<Campaign[]>([]);
    const [error, setError] = useState(false);
    const [campaignFilters, setCampaignFilters] = useState<CampaignFilterParams>({
        page: 1,
        pageSize: 20,
    });
    const [listingFilters, setListingFilters] = useState<ListingFilterParams>({
        page: 1,
        pageSize: 20,
    });
    const prevListingFiltersRef = useRef<string>("");
    const prevCampaignFiltersRef = useRef<string>("");
    const navigate = useNavigate();

    const handleTabChange = (tab: TabType) => {
        if (tab !== activeTab) {
            invokeHapticFeedbackImpact("light");
            useListStateStore.getState().setHomeActiveTab(tab);
        }
    };

    const handleListingClick = useCallback(
        (listing: Listing) => {
            invokeHapticFeedbackImpact("light");
            const targetPath = `/listing/${listing.id}/detail`;
            navigate(targetPath);
        },
        [navigate],
    );

    const handleCampaignClick = useCallback(
        (campaign: Campaign) => {
            invokeHapticFeedbackImpact("light");
            const targetPath = `/campaign/${campaign.id}/detail`;
            navigate(targetPath);
        },
        [navigate],
    );

    const handleListingApplyClick = useCallback(
        (listing: Listing) => {
            invokeHapticFeedbackImpact("light");
            const targetPath = `/listing/${listing.id}/apply`;
            navigate(targetPath);
        },
        [navigate],
    );

    const handleCampaignApplyClick = useCallback(
        (campaign: Campaign) => {
            invokeHapticFeedbackImpact("light");
            const targetPath = `/campaign/${campaign.id}/apply`;
            navigate(targetPath);
        },
        [navigate],
    );

    const handleCampaignFilterChange = useCallback((filters: CampaignFilterParams) => {
        setCampaignFilters(filters);
    }, []);

    const handleListingFilterChange = useCallback((filters: ListingFilterParams) => {
        setListingFilters(filters);
    }, []);

    useEffect(() => {
        if (activeTab !== "listings") return;

        const currentFiltersStr = JSON.stringify(listingFilters);

        prevListingFiltersRef.current = currentFiltersStr;

        const fetchListings = async () => {
            setFetching(true);
            setError(false);

            try {
                const hasFilters =
                    listingFilters.searchTerm ||
                    listingFilters.minPrice !== undefined ||
                    listingFilters.maxPrice !== undefined ||
                    listingFilters.minSubscribers !== undefined ||
                    listingFilters.minAvgViews !== undefined ||
                    listingFilters.language;

                let endpoint: string;

                if (hasFilters) {
                    const params = new URLSearchParams();
                    if (listingFilters.searchTerm) {
                        params.append("SearchTerm", listingFilters.searchTerm);
                    }
                    if (listingFilters.minPrice !== undefined) {
                        params.append("MinPrice", listingFilters.minPrice.toString());
                    }
                    if (listingFilters.maxPrice !== undefined) {
                        params.append("MaxPrice", listingFilters.maxPrice.toString());
                    }
                    if (listingFilters.minSubscribers !== undefined) {
                        params.append(
                            "MinSubscribers",
                            listingFilters.minSubscribers.toString(),
                        );
                    }
                    if (listingFilters.minAvgViews !== undefined) {
                        params.append("MinAvgViews", listingFilters.minAvgViews.toString());
                    }
                    if (listingFilters.language) {
                        params.append("Language", listingFilters.language);
                    }
                    if (listingFilters.page) {
                        params.append("Page", listingFilters.page.toString());
                    }
                    if (listingFilters.pageSize) {
                        params.append("PageSize", listingFilters.pageSize.toString());
                    }
                    endpoint = `/listings?${params.toString()}`;
                } else {
                    endpoint = "/listings";
                }

                const data = await requestAPI(endpoint, {}, "GET");
                console.log("Listings API response:", data);

                if (data && data !== false && data !== null) {
                    if (data.items && Array.isArray(data.items)) {
                        const validListings = data.items.filter((listing: Listing) => {
                            const isValid =
                                listing &&
                                typeof listing === "object" &&
                                listing.id &&
                                listing.channelId &&
                                listing.channelTitle;

                            if (!isValid) {
                                console.warn("Invalid listing data:", listing);
                            }
                            return isValid;
                        });
                        console.log(
                            "Valid listings loaded:",
                            validListings.length,
                            validListings,
                        );
                        setListings(sortByRecencyDesc(validListings));
                    } else {
                        console.error("Invalid listings response format", data);
                        setError(true);
                    }
                } else {
                    console.error("Failed to fetch listings");
                    setError(true);
                }
            } catch (err) {
                console.error("Error fetching listings:", err);
                setError(true);
            } finally {
                setFetching(false);
            }
        };

        fetchListings();
    }, [activeTab, listingFilters]);

    useEffect(() => {
        if (activeTab !== "campaigns") return;

        const currentFiltersStr = JSON.stringify(campaignFilters);

        prevCampaignFiltersRef.current = currentFiltersStr;

        const fetchCampaigns = async () => {
            setFetching(true);
            setError(false);

            try {
                const hasFilters =
                    campaignFilters.searchTerm ||
                    campaignFilters.minBudget !== undefined ||
                    campaignFilters.maxBudget !== undefined ||
                    campaignFilters.language;

                let endpoint: string;

                if (hasFilters) {
                    const params = new URLSearchParams();
                    if (campaignFilters.searchTerm) {
                        params.append("SearchTerm", campaignFilters.searchTerm);
                    }
                    if (campaignFilters.minBudget !== undefined) {
                        params.append("MinBudget", campaignFilters.minBudget.toString());
                    }
                    if (campaignFilters.maxBudget !== undefined) {
                        params.append("MaxBudget", campaignFilters.maxBudget.toString());
                    }
                    if (campaignFilters.language) {
                        params.append("Language", campaignFilters.language);
                    }
                    if (campaignFilters.page) {
                        params.append("Page", campaignFilters.page.toString());
                    }
                    if (campaignFilters.pageSize) {
                        params.append("PageSize", campaignFilters.pageSize.toString());
                    }
                    endpoint = `/campaigns?${params.toString()}`;
                } else {
                    endpoint = "/campaigns/all";
                }

                const data = await requestAPI(endpoint, {}, "GET");
                console.log("Campaigns API response:", data);

                if (data && data !== false && data !== null) {
                    if (data.items && Array.isArray(data.items)) {
                        const validCampaigns = data.items.filter((campaign: Campaign) => {
                            const isValid =
                                campaign &&
                                typeof campaign === "object" &&
                                campaign.id &&
                                campaign.title;

                            if (!isValid) {
                                console.warn("Invalid campaign data:", campaign);
                            }
                            return isValid;
                        });
                        console.log(
                            "Valid campaigns loaded:",
                            validCampaigns.length,
                            validCampaigns,
                        );
                        setCampaigns(sortByRecencyDesc(validCampaigns));
                    } else {
                        console.error("Invalid campaigns response format", data);
                        setError(true);
                    }
                } else {
                    console.error("Failed to fetch campaigns");
                    setError(true);
                }
            } catch (err) {
                console.error("Error fetching campaigns:", err);
                setError(true);
            } finally {
                setFetching(false);
            }
        };

        fetchCampaigns();
    }, [activeTab, campaignFilters]);

    const renderContent = useMemo(() => {
        if (activeTab === "listings") {
            return (
                <VirtualizedList
                    data={listings}
                    isLoading={fetching}
                    hasError={error}
                    loadingState={<LoadingState count={6} />}
                    errorState={
                        <ErrorState message="Failed to load listings. Please try again later." />
                    }
                    emptyState={
                        <EmptyState message="No listings found. Try adjusting your filters!" />
                    }
                    renderItem={(listing) => (
                        <ListingListItem
                            listing={listing}
                            onClick={() => handleListingClick(listing)}
                            onApplyClick={() => handleListingApplyClick(listing)}
                        />
                    )}
                />
            );
        }

        // Campaigns tab
        return (
            <VirtualizedList
                data={campaigns}
                isLoading={fetching}
                hasError={error}
                loadingState={<LoadingState count={6} />}
                errorState={
                    <ErrorState message="Failed to load campaigns. Please try again later." />
                }
                emptyState={
                    <EmptyState message="No campaigns found. Try adjusting your filters!" />
                }
                renderItem={(campaign) => (
                    <CampaignListItem
                        campaign={campaign}
                        showScheduleTime
                        onClick={() => handleCampaignClick(campaign)}
                        onApplyClick={() => handleCampaignApplyClick(campaign)}
                    />
                )}
            />
        );
    }, [
        activeTab,
        fetching,
        listings,
        campaigns,
        error,
        handleListingClick,
        handleCampaignClick,
        handleListingApplyClick,
        handleCampaignApplyClick,
    ]);

    return (
        <>
            <div id="container-page-home">
                <section>
                    <div id="container-tabs-home">
                        <button
                            className={activeTab === "listings" ? "active" : ""}
                            onClick={() => handleTabChange("listings")}
                        >
                            <span>Listings</span>
                        </button>
                        <button
                            className={activeTab === "campaigns" ? "active" : ""}
                            onClick={() => handleTabChange("campaigns")}
                        >
                            <span>Campaigns</span>
                        </button>
                    </div>

                    {activeTab === "listings" && (
                        <ListingFilters
                            onFilterChange={handleListingFilterChange}
                            loading={fetching}
                        />
                    )}

                    {activeTab === "campaigns" && (
                        <CampaignFilters
                            onFilterChange={handleCampaignFilterChange}
                            loading={fetching}
                        />
                    )}

                    {renderContent}
                </section>
            </div>
            <BottomBar />
        </>
    );
}

export default PageHome;
