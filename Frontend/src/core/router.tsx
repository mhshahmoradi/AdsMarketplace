import { isRouteErrorResponse, useRouteError } from "react-router";

import LayoutRoot from "@/core/layouts/Root";
import PageError from "@/core/pages/Error";
import { PageHome } from "@/features/store";
import { PageProfile } from "@/features/profile";
import { PageCreate } from "@/features/create";
import {
    PageAddChannel,
    PageChannelVerification,
    PageChannelDashboard,
    PageChannelDetail,
} from "@/features/channels";
import {
    PageCreateCampaign,
    PageCampaignDashboard,
    PageCampaignDetail,
    PageApplyCampaign,
} from "@/features/campaigns";
import {
    PageApplyListing,
    PageListingDetail,
    PageListingOwnerDetail,
    PageCreateListing,
} from "@/features/listings";
import { PageRequests } from "@/features/requests";
import { PageDeals, PageDealDetail } from "@/features/deals";
import { createBrowserRouter } from "react-router";

const ErrorFallback = () => {
    const error = useRouteError();

    if (isRouteErrorResponse(error)) {
        return <PageError title={error.status.toString()} description={error.statusText} />;
    }

    return <PageError />;
};

export const router = createBrowserRouter([
    {
        path: "/",
        Component: LayoutRoot,
        errorElement: <ErrorFallback />,
        children: [
            {
                index: true,
                Component: PageHome,
            },
            {
                index: true,
                path: "/profile",
                Component: PageProfile,
            },
            {
                index: true,
                path: "/create",
                Component: PageCreate,
            },
            {
                index: true,
                path: "/add-channel",
                Component: PageAddChannel,
            },
            {
                index: true,
                path: "/create-campaign",
                Component: PageCreateCampaign,
            },
            {
                index: true,
                path: "/campaign/:campaignId/edit",
                Component: PageCreateCampaign,
            },
            {
                index: true,
                path: "/channel-verification",
                Component: PageChannelVerification,
            },
            {
                index: true,
                path: "/channel/:channelId",
                Component: PageChannelDashboard,
            },
            {
                index: true,
                path: "/channel/:channelId/detail",
                Component: PageChannelDetail,
            },
            {
                index: true,
                path: "/campaign/:campaignId",
                Component: PageCampaignDashboard,
            },
            {
                index: true,
                path: "/campaign/:campaignId/detail",
                Component: PageCampaignDetail,
            },
            {
                index: true,
                path: "/campaign/:campaignId/apply",
                Component: PageApplyCampaign,
            },
            {
                index: true,
                path: "/channel/:channelId/create-listing",
                Component: PageCreateListing,
            },
            {
                index: true,
                path: "/listing/:listingId/edit",
                Component: PageCreateListing,
            },
            {
                index: true,
                path: "/listing/:listingId",
                Component: PageListingOwnerDetail,
            },
            {
                index: true,
                path: "/listing/:listingId/detail",
                Component: PageListingDetail,
            },
            {
                index: true,
                path: "/listing/:listingId/apply",
                Component: PageApplyListing,
            },
            {
                index: true,
                path: "/requests",
                Component: PageRequests,
            },
            {
                index: true,
                path: "/deals",
                Component: PageDeals,
            },
            {
                index: true,
                path: "/deals/:dealId",
                Component: PageDealDetail,
            },
        ],
    },
]);
