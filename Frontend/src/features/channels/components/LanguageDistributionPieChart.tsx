import {
    ArcElement,
    Chart as ChartJS,
    Legend,
    Tooltip,
    type Plugin,
    type TooltipItem,
} from "chart.js";
import { useMemo, type CSSProperties, type FC } from "react";
import { Pie } from "react-chartjs-2";

import type { ChannelLanguageDistribution } from "../types/channel";

ChartJS.register(ArcElement, Tooltip, Legend);

interface LanguageDistributionPieChartProps {
    distribution: ChannelLanguageDistribution[];
}

const PIE_COLORS = ["#FF3B30", "#FF9500", "#FFCC00", "#34C759", "#5AC8FB", "#007AFF"].reverse();

const hexToRgbChannels = (hex: string): string => {
    const normalized = hex.replace("#", "");
    const fullHex =
        normalized.length === 3
            ? normalized
                  .split("")
                  .map((value) => `${value}${value}`)
                  .join("")
            : normalized;

    const parsed = Number.parseInt(fullHex, 16);
    if (Number.isNaN(parsed)) return "255, 255, 255";

    const red = (parsed >> 16) & 255;
    const green = (parsed >> 8) & 255;
    const blue = parsed & 255;
    return `${red}, ${green}, ${blue}`;
};

const LanguageDistributionPieChart: FC<LanguageDistributionPieChartProps> = ({
    distribution,
}) => {
    const sortedDistribution = useMemo(
        () => [...distribution].sort((a, b) => b.value - a.value),
        [distribution],
    );

    const pieDistribution = useMemo(() => sortedDistribution.slice(0, 2), [sortedDistribution]);

    const chartData = useMemo(
        () => ({
            labels: pieDistribution.map((item) => item.language),
            datasets: [
                {
                    data: pieDistribution.map((item) => item.value),
                    backgroundColor: pieDistribution.map(
                        (_, index) => PIE_COLORS[index % PIE_COLORS.length],
                    ),
                    borderColor: "transparent",
                    borderWidth: 0,
                    hoverBorderColor: "transparent",
                    hoverBorderWidth: 0,
                    hoverOffset: 1,
                },
            ],
        }),
        [pieDistribution],
    );

    const options = useMemo(
        () => ({
            responsive: true,
            maintainAspectRatio: false,
            cutout: 0,
            plugins: {
                legend: {
                    display: false,
                },
                tooltip: {
                    displayColors: false,
                    padding: 8,
                    callbacks: {
                        label: (context: TooltipItem<"pie">) => {
                            const item = pieDistribution[context.dataIndex];
                            if (!item) return context.label;
                            return `${item.language}: ${item.percentage.toLocaleString(undefined, { maximumFractionDigits: 1 })}%`;
                        },
                    },
                },
            },
        }),
        [pieDistribution],
    );

    const percentageLabelsPlugin = useMemo<Plugin<"pie">>(
        () => ({
            id: "pie-percentage-labels",
            afterDatasetsDraw: (chart) => {
                const arcs = chart.getDatasetMeta(0)?.data;
                if (!arcs || arcs.length === 0) return;

                const ctx = chart.ctx;
                ctx.save();
                ctx.font = "600 10px sans-serif";
                ctx.textAlign = "center";
                ctx.textBaseline = "middle";
                ctx.fillStyle = "rgba(255, 255, 255, 0.94)";
                ctx.strokeStyle = "rgba(0, 0, 0, 0.2)";
                ctx.lineWidth = 2;
                ctx.lineJoin = "round";

                arcs.forEach((arc, index) => {
                    const item = pieDistribution[index];
                    if (!item) return;

                    const { x, y, startAngle, endAngle, innerRadius, outerRadius } =
                        arc.getProps(
                            ["x", "y", "startAngle", "endAngle", "innerRadius", "outerRadius"],
                            true,
                        );

                    const angle = (startAngle + endAngle) / 2;
                    const radius = innerRadius + (outerRadius - innerRadius) * 0.62;
                    const labelX = x + Math.cos(angle) * radius;
                    const labelY = y + Math.sin(angle) * radius;
                    const label = `${item.percentage.toLocaleString(undefined, {
                        maximumFractionDigits: 1,
                    })}%`;

                    ctx.strokeText(label, labelX, labelY);
                    ctx.fillText(label, labelX, labelY);
                });

                ctx.restore();
            },
        }),
        [pieDistribution],
    );

    return (
        <div className="channel-language-pie">
            <div className="channel-language-pie-canvas">
                <Pie data={chartData} options={options} plugins={[percentageLabelsPlugin]} />
            </div>

            <div className="channel-language-pie-legend">
                {sortedDistribution.map((item, index) => {
                    const pieColor = PIE_COLORS[index % PIE_COLORS.length];
                    const itemStyle: CSSProperties & { "--pie-color-rgb"?: string } = {
                        "--pie-color-rgb": hexToRgbChannels(pieColor),
                    };

                    return (
                        <div
                            key={item.language}
                            className="language-pie-legend-item"
                            style={itemStyle}
                        >
                            <span
                                className="language-pie-legend-dot"
                                style={{
                                    backgroundColor: pieColor,
                                }}
                            />
                            <span className="language-pie-legend-name">{item.language}</span>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};

export default LanguageDistributionPieChart;
