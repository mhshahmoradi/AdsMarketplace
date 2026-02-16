/**
 * API-related type definitions
 */

// ============================================================================
// Request Types
// ============================================================================

/**
 * HTTP methods
 */
export type HTTPMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

/**
 * API request configuration
 */
export interface APIRequestConfig {
    method?: HTTPMethod;
    headers?: Record<string, string>;
    body?: unknown;
    params?: Record<string, string | number | boolean>;
    timeout?: number;
    retry?: number;
    signal?: AbortSignal;
}

/**
 * Query parameters
 */
export type QueryParams = Record<string, string | number | boolean | undefined | null>;

// ============================================================================
// Response Types
// ============================================================================

/**
 * API error response structure
 */
export interface APIErrorResponse {
    detail?: string;
    title?: string;
    status?: number;
    message?: string;
    errors?: Array<{
        field: string;
        message: string;
    }>;
}

/**
 * Successful API response wrapper
 */
export interface APISuccessResponse<T = unknown> {
    data: T;
    message?: string;
    status?: number;
}

/**
 * Generic API response (can be success or error)
 */
export type APIResponse<T = unknown> = T | APIErrorResponse | null | false;

/**
 * Paginated API response
 */
export interface PaginatedAPIResponse<T> {
    items: T[];
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
}

// ============================================================================
// Status Types
// ============================================================================

/**
 * API request status
 */
export type APIRequestStatus = "idle" | "pending" | "success" | "error";

/**
 * API call state
 */
export interface APICallState<T = unknown> {
    data: T | null;
    error: APIErrorResponse | null;
    status: APIRequestStatus;
    isLoading: boolean;
    isSuccess: boolean;
    isError: boolean;
}

// ============================================================================
// Logging Types
// ============================================================================

/**
 * Log levels for API requests
 */
export type LogLevel = "info" | "success" | "warning" | "error" | "debug";

/**
 * API request log data
 */
export interface APILogData {
    timestamp: string;
    level: LogLevel;
    method: HTTPMethod;
    path: string;
    duration?: number;
    status?: number;
    request?: unknown;
    response?: unknown;
    error?: unknown;
}

// ============================================================================
// Validation Types
// ============================================================================

/**
 * API response validator function
 */
export type ResponseValidator<T> = (data: unknown) => data is T;

/**
 * Validation result
 */
export interface ValidationResult<T = unknown> {
    success: boolean;
    data?: T;
    error?: string;
}

// ============================================================================
// Retry Configuration
// ============================================================================

/**
 * Retry strategy configuration
 */
export interface RetryConfig {
    maxAttempts: number;
    delayMs: number;
    backoffMultiplier?: number;
    retryableStatuses?: number[];
}

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Check if response is an error
 */
export function isAPIError(response: unknown): response is APIErrorResponse {
    if (!response || typeof response !== "object") {
        return false;
    }

    const hasDetail = "detail" in response;
    const hasNumericStatus =
        "status" in response && typeof (response as APIErrorResponse).status === "number";
    const isErrorStatus =
        hasNumericStatus && (response as APIErrorResponse).status! >= 400;

    return hasDetail || isErrorStatus;
}

/**
 * Check if response is successful
 */
export function isAPISuccess<T>(response: unknown): response is T {
    if (response === null || response === false || response === undefined) {
        return false;
    }

    return !isAPIError(response);
}

/**
 * Check if response is paginated
 */
export function isPaginatedResponse<T>(
    response: unknown
): response is PaginatedAPIResponse<T> {
    if (!response || typeof response !== "object") {
        return false;
    }

    return (
        "items" in response &&
        "total" in response &&
        "page" in response &&
        "pageSize" in response &&
        Array.isArray((response as PaginatedAPIResponse<T>).items)
    );
}
