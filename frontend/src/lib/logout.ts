import { useAuth } from "../context/AuthContext";
import { useClearQueryCache } from "../components/QueryProvider";
import Cookies from "js-cookie";

/**
 * Log out the user by clearing authentication state
 * @returns A function that logs out the user
 */

export function useLogout() {
    const { setLoggedIn, setUserId, setUsername } = useAuth();
    const clearQueryCache = useClearQueryCache();
    return async function Logout() {
        setLoggedIn(false);
        setUserId(undefined);
        setUsername(undefined);
        Cookies.remove("user_id");
        clearQueryCache();
    };
}
