import { Virtuoso } from "react-virtuoso";
import { type ReactNode } from "react";
import "./VirtualizedList.scss";

interface VirtualizedListProps<T> {
    data: T[];
    renderItem: (item: T, index: number) => ReactNode;
    className?: string;
    overscan?: number;
    emptyState?: ReactNode;
    errorState?: ReactNode;
    loadingState?: ReactNode;
    isLoading?: boolean;
    hasError?: boolean;
}

export function VirtualizedList<T>({
    data,
    renderItem,
    className = "container-list-items",
    overscan = 200,
    emptyState,
    errorState,
    loadingState,
    isLoading = false,
    hasError = false,
}: VirtualizedListProps<T>) {
    // Show loading state
    if (isLoading && loadingState) {
        return <>{loadingState}</>;
    }

    // Show error state
    if (hasError && errorState) {
        return <>{errorState}</>;
    }

    // Show empty state
    if (!isLoading && !hasError && data.length === 0 && emptyState) {
        return <>{emptyState}</>;
    }

    // Render virtualized list
    return (
        <Virtuoso
            data={data}
            overscan={overscan}
            className={`virtualized-list ${className}`.trim()}
            style={{ touchAction: "pan-y", WebkitOverflowScrolling: "touch" }}
            itemContent={(index, item) => {
                if (!item) return null;
                return (
                    <div
                        style={{
                            paddingBottom: index === data.length - 1 ? "1.125rem" : "",
                            touchAction: "pan-y",
                        }}
                    >
                        {renderItem(item, index)}
                    </div>
                );
            }}
        />
    );
}
