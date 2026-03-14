"use client";

import { useRouterState } from "@tanstack/react-router";
import { useMemo } from "react";

function safeDecodeURIComponent(value: string): string {
    try {
        return decodeURIComponent(value);
    } catch {
        return value;
    }
}

export function useRouteParams() {
    const pathname = useRouterState({
        select: (state) => state.location.pathname,
    });
    return useMemo(() => {
        const segments = pathname.split("/").filter(Boolean);
        return {
            username: segments[0] ? safeDecodeURIComponent(segments[0]) : "",
            projectName: segments[1] ? safeDecodeURIComponent(segments[1]) : "",
            subpage: segments[2] ?? "",
        };
    }, [pathname]);
}
