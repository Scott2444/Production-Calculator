"use client";

import {
    PersistQueryClientProvider,
    Persister,
} from "@tanstack/react-query-persist-client";
import { QueryClient, useQueryClient } from "@tanstack/react-query";
import { createAsyncStoragePersister } from "@tanstack/query-async-storage-persister";
import { ReactNode, useEffect, useState, useRef } from "react";

export function QueryProvider({ children }: { children: ReactNode }) {
    const [isClient, setIsClient] = useState(false);
    const [queryClient] = useState(
        () =>
            new QueryClient({
                defaultOptions: {
                    queries: {
                        staleTime: 1000 * 60 * 5,
                        gcTime: 1000 * 60 * 60 * 24,
                        refetchOnWindowFocus: false,
                        retry: 1,
                    },
                },
            }),
    );

    const persisterRef = useRef<Persister | null>(null);

    useEffect(() => {
        if (typeof window !== "undefined") {
            persisterRef.current = createAsyncStoragePersister({
                storage: window.localStorage,
                key: "production-calculator-react-query",
            });
            setIsClient(true);
        }
    }, []);

    if (!isClient) return null;

    return (
        <PersistQueryClientProvider
            client={queryClient}
            persistOptions={{
                persister: persisterRef.current!,
                maxAge: 1000 * 60 * 60 * 24,
                buster: process.env.NEXT_PUBLIC_QUERY_CACHE_BUSTER ?? "v1",
                dehydrateOptions: {
                    shouldDehydrateQuery: (query) =>
                        query.state.status === "success",
                },
            }}
        >
            {children}
        </PersistQueryClientProvider>
    );
}

export function useClearQueryCache() {
    const queryClient = useQueryClient();
    return () => queryClient.clear();
}
