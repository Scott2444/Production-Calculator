"use client";

import { ReactNode } from "react";
import { useProject } from "@/context/ProjectContext";

function StatusCard({
    children,
    variant = "neutral",
}: {
    children: ReactNode;
    variant?: "neutral" | "error";
}) {
    const className =
        variant === "error"
            ? "rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200"
            : "rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400";

    return <div className={className}>{children}</div>;
}

export default function ProjectStatusGate({
    children,
}: {
    children: ReactNode;
}) {
    const { routeProjectName, currentProject, projectQuery } = useProject();

    if (projectQuery.isLoading) {
        return <StatusCard>Loading project…</StatusCard>;
    }

    if (projectQuery.error) {
        return (
            <StatusCard variant="error">Failed to load projects.</StatusCard>
        );
    }

    if (routeProjectName && !currentProject) {
        return (
            <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-300">
                Project not found: {routeProjectName}
            </div>
        );
    }

    return <>{children}</>;
}
