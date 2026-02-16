import "./ListingFilters.scss";

import { useState, useEffect, useRef, type FC } from "react";
import { HiMagnifyingGlass, HiAdjustmentsHorizontal, HiXMark } from "react-icons/hi2";
import { invokeHapticFeedbackImpact } from "@/shared/utils/telegram";

export interface ListingFilterParams {
    searchTerm?: string;
    minPrice?: number;
    maxPrice?: number;
    minSubscribers?: number;
    minAvgViews?: number;
    language?: string;
    page?: number;
    pageSize?: number;
}

interface ListingFiltersProps {
    onFilterChange: (filters: ListingFilterParams) => void;
    loading?: boolean;
}

const languages = [
    "All Languages",
    "English",
    "Spanish",
    "Russian",
    "Arabic",
    "Portuguese",
    "French",
    "German",
    "Chinese",
];

const ListingFilters: FC<ListingFiltersProps> = ({ onFilterChange, loading = false }) => {
    const [searchTerm, setSearchTerm] = useState("");
    const [showFilters, setShowFilters] = useState(false);
    const [minPrice, setMinPrice] = useState("");
    const [maxPrice, setMaxPrice] = useState("");
    const [minSubscribers, setMinSubscribers] = useState("");
    const [minAvgViews, setMinAvgViews] = useState("");
    const [language, setLanguage] = useState("All Languages");
    const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
    const isMountedRef = useRef(false);

    // Debounced search effect
    useEffect(() => {
        // Skip on initial mount
        if (!isMountedRef.current) {
            isMountedRef.current = true;
            return;
        }

        // Clear existing timer
        if (debounceTimerRef.current) {
            clearTimeout(debounceTimerRef.current);
        }

        // Set new timer for 500ms debounce
        debounceTimerRef.current = setTimeout(() => {
            applyFilters({ searchTerm });
        }, 500);

        // Cleanup on unmount or when searchTerm changes
        return () => {
            if (debounceTimerRef.current) {
                clearTimeout(debounceTimerRef.current);
            }
        };
    }, [searchTerm]);

    const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value;
        setSearchTerm(value);
        // Don't call applyFilters here - let the useEffect handle it with debounce
    };

    const handleClearSearch = () => {
        setSearchTerm("");
        // Clear the debounce timer and apply immediately
        if (debounceTimerRef.current) {
            clearTimeout(debounceTimerRef.current);
        }
        applyFilters({ searchTerm: "" });
    };

    const toggleFilters = () => {
        invokeHapticFeedbackImpact("light");
        setShowFilters(!showFilters);
    };

    const applyFilters = (overrides: Partial<ListingFilterParams> = {}) => {
        const filters: ListingFilterParams = {
            searchTerm: "searchTerm" in overrides ? overrides.searchTerm : searchTerm,
            minPrice:
                "minPrice" in overrides
                    ? overrides.minPrice
                    : minPrice
                      ? parseFloat(minPrice)
                      : undefined,
            maxPrice:
                "maxPrice" in overrides
                    ? overrides.maxPrice
                    : maxPrice
                      ? parseFloat(maxPrice)
                      : undefined,
            minSubscribers:
                "minSubscribers" in overrides
                    ? overrides.minSubscribers
                    : minSubscribers
                      ? parseInt(minSubscribers)
                      : undefined,
            minAvgViews:
                "minAvgViews" in overrides
                    ? overrides.minAvgViews
                    : minAvgViews
                      ? parseInt(minAvgViews)
                      : undefined,
            language:
                "language" in overrides
                    ? overrides.language
                    : language !== "All Languages"
                      ? language
                      : undefined,
            page: 1,
            pageSize: 20,
        };

        // Remove undefined values
        Object.keys(filters).forEach(
            (key) =>
                filters[key as keyof ListingFilterParams] === undefined &&
                delete filters[key as keyof ListingFilterParams],
        );

        onFilterChange(filters);
    };

    const handleApplyFilters = () => {
        invokeHapticFeedbackImpact("medium");
        applyFilters();
    };

    const handleResetFilters = () => {
        invokeHapticFeedbackImpact("light");
        setMinPrice("");
        setMaxPrice("");
        setMinSubscribers("");
        setMinAvgViews("");
        setLanguage("All Languages");
        applyFilters({
            minPrice: undefined,
            maxPrice: undefined,
            minSubscribers: undefined,
            minAvgViews: undefined,
            language: undefined,
        });
    };

    const hasActiveFilters =
        minPrice || maxPrice || minSubscribers || minAvgViews || language !== "All Languages";

    return (
        <div className="listing-filters">
            {/* Search Bar */}
            <div className="search-container">
                <div className="search-input-wrapper">
                    <HiMagnifyingGlass className="search-icon" />
                    <input
                        type="text"
                        placeholder="Search listings..."
                        value={searchTerm}
                        onChange={handleSearchChange}
                        className="search-input"
                    />
                    {searchTerm && (
                        <button className="clear-search-btn" onClick={handleClearSearch}>
                            <HiXMark />
                        </button>
                    )}
                </div>

                <button
                    className={`filter-toggle-btn ${showFilters ? "active" : ""} ${hasActiveFilters ? "has-filters" : ""}`}
                    onClick={toggleFilters}
                >
                    <HiAdjustmentsHorizontal />
                </button>
            </div>

            {/* Filter Panel */}
            {showFilters && (
                <div className="filter-panel animate__animated animate__fadeIn animate__faster">
                    <div className="filter-section">
                        <label htmlFor="language-filter">Language</label>
                        <select
                            id="language-filter"
                            value={language}
                            onChange={(e) => setLanguage(e.target.value)}
                            disabled={loading}
                        >
                            {languages.map((lang) => (
                                <option key={lang} value={lang}>
                                    {lang}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="filter-section">
                        <label>Price Range (TON)</label>
                        <div className="budget-inputs">
                            <input
                                type="number"
                                placeholder="Min"
                                value={minPrice}
                                onChange={(e) => setMinPrice(e.target.value)}
                                disabled={loading}
                                min="0"
                                step="0.01"
                            />
                            <span className="budget-separator">—</span>
                            <input
                                type="number"
                                placeholder="Max"
                                value={maxPrice}
                                onChange={(e) => setMaxPrice(e.target.value)}
                                disabled={loading}
                                min="0"
                                step="0.01"
                            />
                        </div>
                    </div>

                    <div className="filter-section">
                        <label>Channel Metrics</label>
                        <div className="metric-inputs">
                            <input
                                type="number"
                                placeholder="Min Subscribers"
                                value={minSubscribers}
                                onChange={(e) => setMinSubscribers(e.target.value)}
                                disabled={loading}
                                min="0"
                            />
                            <input
                                type="number"
                                placeholder="Min Avg Views"
                                value={minAvgViews}
                                onChange={(e) => setMinAvgViews(e.target.value)}
                                disabled={loading}
                                min="0"
                            />
                        </div>
                    </div>

                    <div className="filter-actions">
                        <button
                            className="btn-reset"
                            onClick={handleResetFilters}
                            disabled={loading || !hasActiveFilters}
                        >
                            Reset
                        </button>
                        <button
                            className="btn-apply"
                            onClick={handleApplyFilters}
                            disabled={loading}
                        >
                            Apply Filters
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ListingFilters;
