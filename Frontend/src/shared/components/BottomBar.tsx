import "./BottomBar.scss";

import { memo, type FC } from "react";
import { FaCircleUser } from "react-icons/fa6";
import { HiPlusCircle, HiInbox, HiBanknotes, HiMegaphone } from "react-icons/hi2";

import ImageLoader from "./ImageLoader";
import { NavLink, useLocation } from "react-router";
import { useLaunchParams } from "@tma.js/sdk-react";
import { useTranslation } from "@/core/i18n/i18nProvider";

const BottomBar: FC = memo(() => {
    const { t } = useTranslation();
    const lp = useLaunchParams();

    const { pathname } = useLocation();

    // Define route categories
    const homeRoutes = [
        "/",
        /^\/channel\/[^\/]+\/detail$/,
        /^\/campaign\/[^\/]+\/detail$/,
        /^\/campaign\/[^\/]+\/apply$/,
        /^\/listing\/[^\/]+\/apply$/,
        /^\/listing\/[^\/]+\/detail$/,
    ];

    const createRoutes = ["/create", "/add-channel", "/create-campaign"];

    const profileRoutes = [
        "/profile",
        "/channel-verification",
        /^\/channel\/[^\/]+$/,
        /^\/campaign\/[^\/]+$/,
        /^\/listing\/[^\/]+$/,
        /^\/listing\/[^\/]+\/edit$/,
        /^\/channel\/[^\/]+\/create-listing$/,
    ];

    const requestsRoutes = ["/requests"];
    const dealsRoutes = ["/deals"];

    // Helper to check if current path matches any route pattern
    const matchesRoute = (routes: (string | RegExp)[]): boolean => {
        return routes.some((route) => {
            if (typeof route === "string") {
                return pathname === route;
            }
            return route.test(pathname);
        });
    };

    const isHomePage = matchesRoute(homeRoutes);
    const isCreatePage = matchesRoute(createRoutes);
    const isRequestsPage = matchesRoute(requestsRoutes);
    const isDealsPage = matchesRoute(dealsRoutes);
    const isProfilePage = matchesRoute(profileRoutes);

    return (
        <div id="container-bottom-bar">
            <div>
                <NavLink to="/" className={isHomePage ? "active" : ""}>
                    <div>
                        <HiMegaphone />
                    </div>
                    <span>Home</span>
                </NavLink>

                <NavLink to="/requests" className={isRequestsPage ? "active" : ""}>
                    <div>
                        <HiInbox />
                    </div>
                    <span>Requests</span>
                </NavLink>

                <NavLink to="/create" className={isCreatePage ? "active" : ""}>
                    <div>
                        <HiPlusCircle className="create-icon" />
                    </div>
                    <span>Add</span>
                </NavLink>

                <NavLink to="/deals" className={isDealsPage ? "active" : ""}>
                    <div>
                        <HiBanknotes />
                    </div>
                    <span>Deals</span>
                </NavLink>

                <NavLink to="/profile" className={isProfilePage ? "active" : ""}>
                    <div>
                        {lp?.tgWebAppData?.user?.photo_url ? (
                            <ImageLoader src={lp?.tgWebAppData?.user?.photo_url} />
                        ) : (
                            <FaCircleUser />
                        )}
                    </div>
                    <span>{lp?.tgWebAppData?.user?.first_name || t("bottombar.profile")}</span>
                </NavLink>
            </div>
        </div>
    );
});

export default BottomBar;
