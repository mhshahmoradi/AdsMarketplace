import { ShimmerThumbnail } from "react-shimmer-effects";
import "./ListStates.scss";

// Shimmer Loading Item
export const ShimmerListItem = () => (
    <div className="list-item-shimmer">
        <div className="shimmer-image">
            <ShimmerThumbnail />
        </div>
    </div>
);

// Loading State with multiple shimmer items
interface LoadingStateProps {
    count?: number;
}

export const LoadingState = ({ count = 6 }: LoadingStateProps) => (
    <div className="container-shimmer-list">
        {Array.from(new Array(count)).map((_, index) => (
            <ShimmerListItem key={index} />
        ))}
    </div>
);

// Empty State
interface EmptyStateProps {
    message?: string;
    icon?: React.ReactNode;
}

export const EmptyState = ({ message = "No items found", icon }: EmptyStateProps) => (
    <div className="container-empty-state">
        {icon && <div className="empty-state-icon">{icon}</div>}
        <p>{message}</p>
    </div>
);

// Error State
interface ErrorStateProps {
    message?: string;
    onRetry?: () => void;
}

export const ErrorState = ({
    message = "Failed to load data. Please try again later.",
    onRetry,
}: ErrorStateProps) => (
    <div className="container-error-state">
        <p>{message}</p>
        {onRetry && (
            <button className="retry-button" onClick={onRetry}>
                Retry
            </button>
        )}
    </div>
);
