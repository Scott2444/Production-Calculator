// Abstract API function to handle protected requests with automatic token refresh
import { useAuth } from "../context/AuthContext";
import { getApiUrl } from "./apiUrl";

/**
 * Makes a protected API request using the access_token cookie
 * If a 401 is received, attempts to refresh the access_token using the refresh_token cookie
 * If refresh fails, logs the user out
 *
 * @param input - RequestInfo (URL or Request object)
 * @param init - RequestInit (fetch options)
 */

export function useProtectedApi() {
    const { setLoggedIn } = useAuth();

    return async function protectedApi(
        input: RequestInfo,
        init: RequestInit = {},
    ) {
        const resolvedInput =
            typeof input === "string" ? getApiUrl(input) : input;

        const fetchWithCookies = (
            url: RequestInfo,
            options: RequestInit = {},
        ) =>
            fetch(url, {
                credentials: "include",
                ...options,
            });

        let response = await fetchWithCookies(resolvedInput, init);

        if (response.status === 401) {
            // Try to refresh the access token
            const refreshResponse = await fetchWithCookies(
                getApiUrl("/auth/refresh"),
                { method: "POST" },
            );
            if (refreshResponse.status === 401) {
                // Refresh failed, log out
                setLoggedIn(false);
                return refreshResponse;
            }
            // Retry original request after refresh
            response = await fetchWithCookies(resolvedInput, init);
            if (response.status === 401) {
                // Still unauthorized, log out
                setLoggedIn(false);
            }
        }
        return response;
    };
}
