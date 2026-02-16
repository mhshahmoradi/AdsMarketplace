import { ShimmerThumbnail } from "react-shimmer-effects";

interface RequestsLoadingStateProps {
    count?: number;
}

const RequestCardShimmer = () => (
    <div className="request-card-shimmer">
        <ShimmerThumbnail />
    </div>
);

const RequestsLoadingState = ({ count = 5 }: RequestsLoadingStateProps) => (
    <div className="requests-loading-skeleton">
        {Array.from({ length: count }).map((_, index) => (
            <RequestCardShimmer key={index} />
        ))}
    </div>
);

export default RequestsLoadingState;
