"use client";

import React, {
    createContext,
    useContext,
    useState,
    useEffect,
    ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";

function getCookieValue(name: string): string | undefined {
    const prefix = `${name}=`;
    const entry = document.cookie
        .split(";")
        .map((cookie) => cookie.trim())
        .find((cookie) => cookie.startsWith(prefix));

    if (!entry) return undefined;
    const value = entry.slice(prefix.length);
    return value ? decodeURIComponent(value) : undefined;
}

interface CachedUser {
    username?: string;
}

interface AuthContextType {
    loggedIn: boolean;
    isHydrated: boolean;
    userId?: string;
    username?: string;
    setLoggedIn: (value: boolean) => void;
    setUserId: (id: string | undefined) => void;
    setUsername: (name: string | undefined) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
    const queryClient = useQueryClient();
    const [loggedIn, setLoggedIn] = useState(false);
    const [isHydrated, setIsHydrated] = useState(false);
    const [userId, setUserId] = useState<string | undefined>(undefined);
    const [username, setUsername] = useState<string | undefined>(undefined);

    useEffect(() => {
        const cookieUserId = getCookieValue("user_id");

        setLoggedIn(Boolean(cookieUserId));
        setUserId(cookieUserId);

        if (!cookieUserId) {
            setUsername(undefined);
            setIsHydrated(true);
            return;
        }

        const cachedUser = queryClient.getQueryData<CachedUser>([
            "user",
            cookieUserId,
        ]);
        setUsername(cachedUser?.username);
        setIsHydrated(true);
    }, [queryClient]);

    useEffect(() => {
        if (!userId) {
            setUsername(undefined);
            return;
        }

        const existingUser = queryClient.getQueryData<CachedUser>([
            "user",
            userId,
        ]);
        if (existingUser?.username) {
            setUsername(existingUser.username);
        }

        return queryClient.getQueryCache().subscribe((event) => {
            const queryKey = event.query.queryKey;
            if (
                Array.isArray(queryKey) &&
                queryKey[0] === "user" &&
                queryKey[1] === userId
            ) {
                const updatedUser = event.query.state.data as
                    | CachedUser
                    | undefined;
                if (updatedUser?.username) {
                    setUsername(updatedUser.username);
                }
            }
        });
    }, [queryClient, userId]);

    return (
        <AuthContext.Provider
            value={{
                loggedIn,
                isHydrated,
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
