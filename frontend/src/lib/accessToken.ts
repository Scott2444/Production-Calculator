// Function to get access token and store the user ID in context
import { useAuth } from "../context/AuthContext";
import { getApiUrl } from "./apiUrl";

/**
 * Requests an access_token cookie and stores the user ID in context
 */

export function useAccessTokenFetch() {
    const { setUserId } = useAuth();

    return async function accessTokenFetch() {
        const response = await fetch(getApiUrl("/auth/refresh"), {
            method: "POST",
            credentials: "include",
        });
        if (response.ok) {
            const data = await response.json();
            setUserId(data.puid);
        }
        return response;
    };
}
