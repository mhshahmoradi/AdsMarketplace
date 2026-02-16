import "./CampaignFilters.scss";

import { useState, useEffect, useRef, type FC } from "react";
import { HiMagnifyingGlass, HiAdjustmentsHorizontal, HiXMark } from "react-icons/hi2";
import { invokeHapticFeedbackImpact } from "@/shared/utils/telegram";

export interface CampaignFilterParams {
    searchTerm?: string;
    minBudget?: number;
    maxBudget?: number;
    language?: string;
    page?: number;
    pageSize?: number;
}

interface CampaignFiltersProps {
    onFilterChange: (filters: CampaignFilterParams) => void;
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

const CampaignFilters: FC<CampaignFiltersProps> = ({ onFilterChange, loading = false }) => {
    const [searchTerm, setSearchTerm] = useState("");
    const [showFilters, setShowFilters] = useState(false);
    const [minBudget, setMinBudget] = useState("");
    const [maxBudget, setMaxBudget] = useState("");
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

    const applyFilters = (overrides: Partial<CampaignFilterParams> = {}) => {
        const filters: CampaignFilterParams = {
            searchTerm: "searchTerm" in overrides ? overrides.searchTerm : searchTerm,
            minBudget:
                "minBudget" in overrides
                    ? overrides.minBudget
                    : minBudget
                      ? parseInt(minBudget)
                      : undefined,
            maxBudget:
                "maxBudget" in overrides
                    ? overrides.maxBudget
                    : maxBudget
                      ? parseInt(maxBudget)
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
                filters[key as keyof CampaignFilterParams] === undefined &&
                delete filters[key as keyof CampaignFilterParams],
        );

        onFilterChange(filters);
    };

    const handleApplyFilters = () => {
        invokeHapticFeedbackImpact("medium");
        applyFilters();
    };

    const handleResetFilters = () => {
        invokeHapticFeedbackImpact("light");
        setMinBudget("");
        setMaxBudget("");
        setLanguage("All Languages");
        applyFilters({ minBudget: undefined, maxBudget: undefined, language: undefined });
    };

    const hasActiveFilters = minBudget || maxBudget || language !== "All Languages";

    return (
        <div className="campaign-filters">
            {/* Search Bar */}
            <div className="search-container">
                <div className="search-input-wrapper">
                    <HiMagnifyingGlass className="search-icon" />
                    <input
                        type="text"
                        placeholder="Search campaigns..."
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
                        <label>Budget Range (TON)</label>
                        <div className="budget-inputs">
                            <input
                                type="number"
                                placeholder="Min"
                                value={minBudget}
                                onChange={(e) => setMinBudget(e.target.value)}
                                disabled={loading}
                                min="0"
                            />
                            <span className="budget-separator">—</span>
                            <input
                                type="number"
                                placeholder="Max"
                                value={maxBudget}
                                onChange={(e) => setMaxBudget(e.target.value)}
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

export default CampaignFilters;
