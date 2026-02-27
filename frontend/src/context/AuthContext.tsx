"use client";

import React, {
    createContext,
    useContext,
    useState,
    useEffect,
    ReactNode,
} from "react";
import { getApiUrl } from "@/lib/apiUrl";

interface AuthContextType {
    loggedIn: boolean;
    userId?: string;
    username?: string;
    setLoggedIn: (value: boolean) => void;
    setUserId: (id: string | undefined) => void;
    setUsername: (name: string | undefined) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
    const [loggedIn, setLoggedIn] = useState(false);
    const [userId, setUserId] = useState<string | undefined>(undefined);
    const [username, setUsername] = useState<string | undefined>(undefined);
    useEffect(() => {
        const checkAuth = async () => {
            // Check if the user is logged in
            const hasToken = document.cookie
                .split(";")
                .some((cookie) => cookie.trim().startsWith("user_id="));
            if (hasToken) {
                setLoggedIn(hasToken);
                // Grab new access token
                const response = await fetch(getApiUrl("/auth/refresh"), {
                    method: "POST",
                    credentials: "include",
                });
                if (response.ok) {
                    const data = await response.json();
                    setUserId(data.puid);
                    setUsername(data.username);
                }
            }
        };
        checkAuth();
    }, []);

    return (
        <AuthContext.Provider
            value={{
                loggedIn,
                setLoggedIn,
                userId,
                setUserId,
                username,
                setUsername,
            }}
        >
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
}
