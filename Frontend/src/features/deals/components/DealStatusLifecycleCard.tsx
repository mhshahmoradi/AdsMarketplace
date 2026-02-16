import { useEffect, useRef } from "react";
import { HiXCircle } from "react-icons/hi2";
import { STATUS_LIFECYCLE, formatStatusLabel, getStatusColor } from "../utils/dealDetail.utils";

interface DealStatusLifecycleCardProps {
    dealStatus?: string;
    isTerminalStatus: boolean;
    lifecycleIndex: number;
}

const DealStatusLifecycleCard = ({
    dealStatus,
    isTerminalStatus,
    lifecycleIndex,
}: DealStatusLifecycleCardProps) => {
    const timelineScrollRef = useRef<HTMLDivElement | null>(null);
    const statusNodeRefs = useRef<Array<HTMLButtonElement | null>>([]);

    const centerStatusNode = (index: number, behavior: ScrollBehavior = "smooth") => {
        const container = timelineScrollRef.current;
        const node = statusNodeRefs.current[index];
        if (!container || !node) {
            return;
        }

        const targetScrollLeft =
            node.offsetLeft + node.offsetWidth / 2 - container.clientWidth / 2;
        container.scrollTo({ left: targetScrollLeft, behavior });
    };

    useEffect(() => {
        if (isTerminalStatus || lifecycleIndex < 0) {
            return;
        }

        centerStatusNode(lifecycleIndex, "auto");
    }, [isTerminalStatus, lifecycleIndex]);

    return (
        <div className="status-lifecycle animate__animated animate__fadeIn">
            <div className="lifecycle-header">
                <h2>Status Lifecycle</h2>
                <span className={`status-pill ${getStatusColor(dealStatus)}`}>
                    {formatStatusLabel(dealStatus)}
                </span>
            </div>

            <div className="lifecycle-scroll-shell">
                <div className="lifecycle-scroll" ref={timelineScrollRef}>
                    <div className="lifecycle-timeline">
                        {STATUS_LIFECYCLE.map((status, index) => {
                            const isCurrent =
                                !isTerminalStatus &&
                                lifecycleIndex >= 0 &&
                                index === lifecycleIndex;
                            const isDone =
                                !isTerminalStatus &&
                                lifecycleIndex >= 0 &&
                                index < lifecycleIndex;

                            return (
                                <button
                                    key={status}
                                    type="button"
                                    ref={(element) => {
                                        statusNodeRefs.current[index] = element;
                                    }}
                                    className={`timeline-node ${
                                        isCurrent ? "current" : isDone ? "done" : "inactive"
                                    }`}
                                    onClick={() => centerStatusNode(index)}
                                >
                                    <span className="node-circle" />
                                    <span className="node-label">
                                        {formatStatusLabel(status)}
                                    </span>
                                </button>
                            );
                        })}
                    </div>
                </div>
            </div>

            {isTerminalStatus && (
                <div className="terminal-banner">
                    <HiXCircle />
                    <span>Terminal state: {formatStatusLabel(dealStatus)}</span>
                </div>
            )}
        </div>
    );
};

export default DealStatusLifecycleCard;
