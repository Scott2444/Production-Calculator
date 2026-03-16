"use client";

import React, { useMemo, useState } from "react";
import { Link, useRouterState } from "@tanstack/react-router";
import { useRouteParams } from "@/hooks/useRouteParams";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/context/AuthContext";
import { useProject } from "@/context/ProjectContext";
import { useProtectedApi } from "@/lib/api";
import { fetchWorkflows, type Workflow } from "@/lib/workflow";
import {
    IconAdjustments,
    IconBox,
    IconCpu,
    IconGitBranch,
    IconSoup,
    IconBolt,
    IconLayoutSidebarLeftCollapse,
    IconLayoutSidebarLeftExpand,
    IconChevronRight,
    IconDualScreen,
} from "@tabler/icons-react";

const navItems = [
    { name: "Products", slug: "products", icon: IconBox },
    { name: "Recipes", slug: "recipes", icon: IconSoup },
    { name: "Machines", slug: "machines", icon: IconCpu },
    { name: "Modifiers", slug: "modifiers", icon: IconAdjustments },
    { name: "Attributes", slug: "attributes", icon: IconBolt },
] as const;

function coerceWorkflows(value: unknown): Workflow[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Workflow[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Workflow[];
    }
    return [];
}

function getWorkflowRouteSegment(workflow: Workflow): string {
    const trimmedName = workflow.name?.trim();
    return trimmedName && trimmedName.length > 0 ? trimmedName : workflow.puid;
}

function normalizePath(path: string): string {
    if (!path) return "/";
    if (path.length > 1 && path.endsWith("/")) {
        return path.slice(0, -1);
    }
    return path;
}

function safeDecodeURIComponent(value: string): string {
    try {
        return decodeURIComponent(value);
    } catch {
        return value;
    }
}

function getPathSegments(pathname: string): string[] {
    return normalizePath(pathname)
        .split("/")
        .filter(Boolean)
        .map(safeDecodeURIComponent);
}

export default function ProjectSidebar() {
    const pathname = useRouterState({
        select: (state) => state.location.pathname,
    });
    const { username: routeUsername, projectName: routeProjectName } =
        useRouteParams();

    const { loggedIn } = useAuth();
    const { projectId, isOwner } = useProject();
    const protectedApi = useProtectedApi();
    const [collapsed, setCollapsed] = useState(false);

    const workflowsQuery = useQuery({
        queryKey: ["sidebar-workflows", projectId],
        queryFn: () => fetchWorkflows(projectId, protectedApi),
        enabled: Boolean(projectId) && isOwner,
        staleTime: 60 * 1000,
    });

    const workflows = useMemo(() => {
        const entries = coerceWorkflows(workflowsQuery.data);
        return [...entries].sort((a, b) => {
            const left = a.name?.trim() || a.puid;
            const right = b.name?.trim() || b.puid;
            return left.localeCompare(right, undefined, {
                sensitivity: "base",
            });
        });
    }, [workflowsQuery.data]);

    const userHomeHref = useMemo(() => {
        if (!routeUsername) return "/";
        return `/${encodeURIComponent(routeUsername)}/`;
    }, [routeUsername]);

    const projectHomeHref = useMemo(() => {
        if (!routeUsername || !routeProjectName) return userHomeHref;
        return `/${encodeURIComponent(routeUsername)}/${encodeURIComponent(routeProjectName)}/`;
    }, [routeUsername, routeProjectName, userHomeHref]);

    const workflowsHref = useMemo(() => {
        if (!routeUsername || !routeProjectName) return userHomeHref;
        return `/${encodeURIComponent(routeUsername)}/${encodeURIComponent(routeProjectName)}/workflows/`;
    }, [routeUsername, routeProjectName, userHomeHref]);

    const pathnameSegments = useMemo(
        () => getPathSegments(pathname),
        [pathname],
    );
    const projectRootActive = pathnameSegments.length === 2;
    const activeSubpage = pathnameSegments[2] ?? "";
    const activeWorkflowName = pathnameSegments[3] ?? "";

    const linkBaseClass =
        "group flex h-10 items-center gap-3 rounded-xl border px-3 text-sm font-medium transition-colors";
    const activeLinkClass =
        "border-purple-500/40 bg-purple-500/15 text-slate-100";
    const inactiveLinkClass =
        "border-transparent text-slate-300 hover:border-slate-700 hover:bg-slate-800/65 hover:text-slate-100";

    return (
        <aside
            className={`${
                collapsed ? "w-17" : "w-72"
            } shrink-0 self-stretch border-r border-slate-800 bg-slate-900/60 text-slate-200 transition-[width] duration-200 overflow-hidden text-nowrap`}
        >
            <div className="flex h-full flex-col gap-3 p-3">
                <div className="space-y-2">
                    <Link
                        to={userHomeHref}
                        className="inline-flex h-7 items-center rounded-md px-2 text-xs text-slate-400 transition-colors hover:bg-slate-800/65 hover:text-slate-200"
                        title="Return to user's projects"
                    >
                        <IconChevronRight
                            size={14}
                            className="mr-1 shrink-0 rotate-180"
                        />
                        <span
                            className={`overflow-hidden transition-[max-width,opacity] duration-200 ${
                                collapsed
                                    ? "max-w-0 opacity-0"
                                    : "max-w-40 opacity-100"
                            }`}
                        >
                            Return to projects
                        </span>
                    </Link>

                    <div className="min-w-0 pl-1">
                        <div className="h-5 truncate text-xs uppercase tracking-wider text-slate-500">
                            Project Navigation
                        </div>
                        <div className="text-lg h-8 truncate font-semibold text-slate-100">
                            {routeProjectName || "Project"}
                        </div>
                    </div>
                </div>

                <nav className="flex flex-col gap-1">
                    <Link
                        to={projectHomeHref}
                        className={`${linkBaseClass} ${
                            projectRootActive
                                ? activeLinkClass
                                : inactiveLinkClass
                        }`}
                        title="Project overview"
                    >
                        <IconDualScreen
                            size={18}
                            className="shrink-0 text-slate-400 group-hover:text-slate-200"
                        />
                        <span
                            className={`truncate overflow-hidden transition-[max-width,opacity] duration-200 ${
                                collapsed
                                    ? "max-w-0 opacity-0"
                                    : "max-w-52 opacity-100"
                            }`}
                        >
                            Overview
                        </span>
                    </Link>
                </nav>

                <div className="border-t border-slate-800 pt-4">
                    <div className="h-6 px-2 pb-2 text-xs font-medium uppercase tracking-wider text-slate-500">
                        <span
                            className={`block overflow-hidden transition-[max-width,opacity] duration-200 ${
                                collapsed
                                    ? "max-w-0 opacity-0"
                                    : "max-w-24 opacity-100"
                            }`}
                        >
                            Components
                        </span>
                    </div>
                    <nav className="flex flex-col gap-1">
                        {navItems.map((item) => {
                            const Icon = item.icon;
                            const href = `/${encodeURIComponent(routeUsername)}/${encodeURIComponent(routeProjectName)}/${item.slug}/`;
                            const active = activeSubpage === item.slug;

                            return (
                                <Link
                                    key={item.slug}
                                    to={href}
                                    className={`${linkBaseClass} ${
                                        active
                                            ? activeLinkClass
                                            : inactiveLinkClass
                                    }`}
                                    title={item.name}
                                >
                                    <Icon
                                        size={18}
                                        className="shrink-0 text-slate-400 group-hover:text-slate-200"
                                    />
                                    <span
                                        className={`truncate overflow-hidden transition-[max-width,opacity] duration-200 ${
                                            collapsed
                                                ? "max-w-0 opacity-0"
                                                : "max-w-40 opacity-100"
                                        }`}
                                    >
                                        {item.name}
                                    </span>
                                </Link>
                            );
                        })}
                    </nav>
                </div>

                <div className="border-t border-slate-800 pt-4">
                    <div className="h-6 px-2 pb-2 text-xs font-medium uppercase tracking-wider text-slate-500">
                        <span
                            className={`block overflow-hidden transition-[max-width,opacity] duration-200 ${
                                collapsed
                                    ? "max-w-0 opacity-0"
                                    : "max-w-20 opacity-100"
                            }`}
                        >
                            Workflows
                        </span>
                    </div>

                    {!isOwner ? (
                        <div className="h-10 rounded-xl border border-slate-800 bg-slate-900/50 px-3 text-xs text-slate-500 flex items-center">
                            {collapsed ? "WF" : "Owner only"}
                        </div>
                    ) : (
                        <div className="flex flex-col gap-1">
                            <Link
                                to={workflowsHref}
                                className={`${linkBaseClass} ${
                                    activeSubpage === "workflows"
                                        ? activeLinkClass
                                        : inactiveLinkClass
                                }`}
                                title="Workflow list"
                            >
                                <IconGitBranch
                                    size={18}
                                    className="shrink-0 text-slate-400 group-hover:text-slate-200"
                                />
                                <span
                                    className={`truncate overflow-hidden transition-[max-width,opacity] duration-200 ${
                                        collapsed
                                            ? "max-w-0 opacity-0"
                                            : "max-w-40 opacity-100"
                                    }`}
                                >
                                    All Workflows
                                </span>
                            </Link>

                            {!collapsed &&
                                workflows.map((workflow) => {
                                    const workflowLabel =
                                        workflow.name?.trim() || workflow.puid;
                                    const workflowRouteSegment =
                                        getWorkflowRouteSegment(workflow);
                                    const workflowHref = `/${encodeURIComponent(routeUsername)}/${encodeURIComponent(routeProjectName)}/workflows/${encodeURIComponent(getWorkflowRouteSegment(workflow))}`;
                                    const workflowActive =
                                        activeSubpage === "workflows" &&
                                        activeWorkflowName ===
                                            workflowRouteSegment;

                                    return (
                                        <Link
                                            key={workflow.puid}
                                            to={workflowHref}
                                            className={`ml-4 flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm transition-colors ${
                                                workflowActive
                                                    ? "bg-purple-500/15 text-purple-200"
                                                    : "text-slate-400 hover:bg-slate-800/60 hover:text-slate-200"
                                            }`}
                                            title={workflowLabel}
                                        >
                                            <IconChevronRight
                                                size={14}
                                                className="shrink-0"
                                            />
                                            <span className="truncate">
                                                {workflowLabel}
                                            </span>
                                        </Link>
                                    );
                                })}

                            {!collapsed && workflowsQuery.isLoading && (
                                <div className="ml-4 px-3 py-1.5 text-xs text-slate-500">
                                    Loading workflows...
                                </div>
                            )}

                            {!collapsed &&
                                !workflowsQuery.isLoading &&
                                workflowsQuery.error && (
                                    <div className="ml-4 px-3 py-1.5 text-xs text-red-300">
                                        Failed to load workflows.
                                    </div>
                                )}
                        </div>
                    )}
                </div>

                <div className="mt-auto border-t border-slate-800 pt-2 flex justify-end">
                    <button
                        type="button"
                        aria-label={
                            collapsed ? "Expand sidebar" : "Collapse sidebar"
                        }
                        onClick={() => setCollapsed((value) => !value)}
                        className="mt-3 inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-slate-700 bg-slate-900/70 px-3 text-sm text-slate-300 transition-colors hover:bg-slate-800/80 hover:text-slate-100"
                    >
                        {collapsed ? (
                            <IconLayoutSidebarLeftExpand size={18} />
                        ) : (
                            <IconLayoutSidebarLeftCollapse size={18} />
                        )}
                    </button>
                </div>
            </div>
        </aside>
    );
}
