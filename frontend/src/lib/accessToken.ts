// Function to get access token and store the user ID in context
import { useAuth } from "../context/AuthContext";

/**
 * Requests an access_token cookie and stores the user ID in context
 */

export function useAccessTokenFetch() {
	const { setUserId } = useAuth();

	return async function accessTokenFetch() {
        let response = await fetch("/api/auth/refresh", { method: "POST" });
        if (response.ok) {
            const data = await response.json();
            setUserId(data.puid);
        }
        return response;
    };
}
