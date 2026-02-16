type LogLevel = "info" | "success" | "warning" | "error";

interface LogData {
    timestamp: string;
    level: LogLevel;
    method: string;
    path: string;
    duration?: number;
    status?: number;
    request?: any;
    response?: any;
    error?: any;
}

class APILogger {
    private static instance: APILogger;
    private logs: LogData[] = [];
    private maxLogs = 100; // Keep last 100 logs
    private enableConsole = true;
    private enableStorage = false;

    private constructor() {
        // Load settings from localStorage if available
        const settings = localStorage.getItem("api_logger_settings");
        if (settings) {
            try {
                const parsed = JSON.parse(settings);
                this.enableConsole = parsed.enableConsole ?? true;
                this.enableStorage = parsed.enableStorage ?? false;
            } catch (e) {
                // Ignore parse errors
            }
        }
    }

    static getInstance(): APILogger {
        if (!APILogger.instance) {
            APILogger.instance = new APILogger();
        }
        return APILogger.instance;
    }

    private getColor(level: LogLevel): string {
        const root = document.documentElement;
        switch (level) {
            case "info":
                return getComputedStyle(root).getPropertyValue("--primary-color").trim();
            case "success":
                return getComputedStyle(root).getPropertyValue("--success-color").trim();
            case "warning":
                return getComputedStyle(root).getPropertyValue("--warning-color").trim();
            case "error":
                return getComputedStyle(root).getPropertyValue("--danger-color").trim();
            default:
                return getComputedStyle(root).getPropertyValue("--muted-color").trim();
        }
    }

    private consoleLog(logData: LogData): void {
        if (!this.enableConsole) return;

        const color = this.getColor(logData.level);
        const emoji = {
            info: "ℹ️",
            success: "✅",
            warning: "⚠️",
            error: "❌",
        }[logData.level];

        const durationStr = logData.duration ? ` (${logData.duration}ms)` : "";
        const statusStr = logData.status ? ` [${logData.status}]` : "";

        console.groupCollapsed(
            `%c${emoji} API ${logData.level.toUpperCase()} %c${logData.method} ${logData.path}${statusStr}${durationStr}`,
            `color: ${color}; font-weight: bold;`,
            "color: inherit; font-weight: normal;",
        );

        console.log(`🕐 Timestamp: ${logData.timestamp}`);

        if (logData.request && Object.keys(logData.request).length > 0) {
            console.log("📤 Request:", logData.request);
        }

        if (logData.response) {
            console.log("📥 Response:", logData.response);
        }

        if (logData.error) {
            console.error("💥 Error:", logData.error);
        }

        if (logData.duration) {
            const root = document.documentElement;
            const perfColor =
                logData.duration < 500
                    ? getComputedStyle(root).getPropertyValue("--success-color").trim()
                    : logData.duration < 1000
                      ? getComputedStyle(root).getPropertyValue("--warning-color").trim()
                      : getComputedStyle(root).getPropertyValue("--danger-color").trim();
            console.log(
                `%c⏱️  Duration: ${logData.duration}ms`,
                `color: ${perfColor}; font-weight: bold;`,
            );
        }

        console.groupEnd();
    }

    log(logData: LogData): void {
        // Add to logs array
        this.logs.push(logData);

        // Keep only last maxLogs entries
        if (this.logs.length > this.maxLogs) {
            this.logs.shift();
        }

        // Console logging
        this.consoleLog(logData);

        // Optional: Store in localStorage for debugging
        if (this.enableStorage) {
            try {
                localStorage.setItem("api_logs", JSON.stringify(this.logs.slice(-20)));
            } catch (e) {
                // Storage quota exceeded, clear old logs
                localStorage.removeItem("api_logs");
            }
        }
    }

    getLogs(): LogData[] {
        return [...this.logs];
    }

    clearLogs(): void {
        this.logs = [];
        localStorage.removeItem("api_logs");
    }

    configure(settings: { enableConsole?: boolean; enableStorage?: boolean }): void {
        if (settings.enableConsole !== undefined) {
            this.enableConsole = settings.enableConsole;
        }
        if (settings.enableStorage !== undefined) {
            this.enableStorage = settings.enableStorage;
        }
        localStorage.setItem(
            "api_logger_settings",
            JSON.stringify({
                enableConsole: this.enableConsole,
                enableStorage: this.enableStorage,
            }),
        );
    }
}

export const requestAPI = async (
    path = "/",
    params: { [key: string]: any } = {},
    method: "GET" | "POST" | "PATCH" | "PUT" = "POST",
    useJson = true,
): Promise<any | null | false> => {
    const logger = APILogger.getInstance();
    const startTime = performance.now();

    try {
        const headers: { [key: string]: string } = {};

        // Get JWT token from localStorage
        const jwt = localStorage.getItem("jwt");
        if (jwt) {
            headers["Authorization"] = `Bearer ${jwt}`;
        }

        let body: FormData | string | undefined;

        if (method === "POST" || method === "PATCH" || method === "PUT") {
            if (useJson) {
                headers["Content-Type"] = "application/json";
                headers["Accept"] = "*/*";
                body = JSON.stringify(params);
            } else {
                const FD = new FormData();
                for (const [key, item] of Object.entries(params)) {
                    FD.append(key, item);
                }
                body = FD;
            }
        }

        // Log request
        logger.log({
            timestamp: new Date().toISOString(),
            level: "info",
            method,
            path,
            request: (method === "POST" || method === "PATCH") && useJson ? params : undefined,
        });

        const request = await fetch(import.meta.env.VITE_BACKEND_BASE_URL + path, {
            method: method,
            body: body,
            headers: headers,
        });

        const duration = Math.round(performance.now() - startTime);
        const result = await request.json();

        // Determine log level based on response
        let logLevel: LogLevel = "success";
        if (!request.ok) {
            logLevel = request.status >= 500 ? "error" : "warning";
        }

        // Log response
        logger.log({
            timestamp: new Date().toISOString(),
            level: logLevel,
            method,
            path,
            duration,
            status: request.status,
            response: result,
        });

        if ("data" in result && result.ok) {
            return result.data;
        }

        // If response is successful but doesn't have the expected structure, return the result
        if (request.ok) {
            return result;
        }

        // Return error response with detail for error handling
        return result;
    } catch (e) {
        const duration = Math.round(performance.now() - startTime);

        // Log error
        logger.log({
            timestamp: new Date().toISOString(),
            level: "error",
            method,
            path,
            duration,
            error: e instanceof Error ? { message: e.message, stack: e.stack } : e,
        });

        console.error("API request failed:", e);
        return null;
    }
};

// Export logger instance for external access (debugging, monitoring)
export const apiLogger = APILogger.getInstance();

// Expose to window for debugging in browser console
if (typeof window !== "undefined") {
    (window as any).apiLogger = apiLogger;
}
