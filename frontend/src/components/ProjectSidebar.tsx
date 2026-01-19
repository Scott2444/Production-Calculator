"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter, useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/context/AuthContext";
import { useProtectedApiFetch } from "@/lib/api";
import { fetchProjects } from "@/lib/projects";
import DropDown from "@/components/DropDown";
import Popup from "@/components/Popup";
import {
    IconAdjustments,
    IconBox,
    IconCheck,
    IconCpu,
    IconGitBranch,
    IconPlus,
    IconSoup,
} from "@tabler/icons-react";

interface Project {
    puid: string;
    name: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
    createdAt: string;
    updatedAt: string;
}

const navItems = [
    { name: "Workflows", slug: "workflows", icon: IconGitBranch },
    { name: "Products", slug: "products", icon: IconBox },
    { name: "Recipes", slug: "recipes", icon: IconSoup },
    { name: "Machines", slug: "machines", icon: IconCpu },
    { name: "Modifiers", slug: "modifiers", icon: IconAdjustments },
] as const;

export default function ProjectSidebar() {
    const pathname = usePathname();
    const router = useRouter();
    const params = useParams<{ username?: string; project_name?: string }>();
    const queryClient = useQueryClient();
    const username = params?.username ?? "";
    const routeProjectName = params?.project_name
        ? decodeURIComponent(params.project_name)
        : "";

    const { userId, loggedIn } = useAuth();
    const protectedApiFetch = useProtectedApiFetch();
    const {
        data: projects,
        isLoading,
        error,
    } = useQuery({
        queryKey: ["projects", userId],
        queryFn: () => fetchProjects(userId!, protectedApiFetch),
        staleTime: 5 * 60 * 1000,
        enabled: Boolean(userId),
    });

    const [currentProject, setCurrentProject] = useState<Project | null>(null);

    // Create Project State
    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createIsPublic, setCreateIsPublic] = useState(false);
    const [createError, setCreateError] = useState<string | null>(null);
    const createNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!projects || !routeProjectName) return;
        const match = projects.find(
            (p: Project) => p.name === routeProjectName,
        );
        if (match) setCurrentProject(match);
    }, [projects, routeProjectName]);

    const userHomeHref = useMemo(() => {
        if (!username) return "/";
        return `/${encodeURIComponent(username)}/`;
    }, [username]);

    const projectHomeHref = useMemo(() => {
        if (!username || !currentProject) return userHomeHref;
        return `/${encodeURIComponent(username)}/${encodeURIComponent(currentProject.name)}/`;
    }, [username, currentProject, userHomeHref]);

    const handleSelectProject = (
        project: Project | null,
        close?: () => void,
    ) => {
        setCurrentProject(project);
        close?.();
        if (!username) return;
        if (!project) {
            router.push(userHomeHref);
            return;
        }
        router.push(
            `/${encodeURIComponent(username)}/${encodeURIComponent(project.name)}/`,
        );
    };

    const createProjectMutation = useMutation({
        mutationFn: async (payload: {
            name: string;
            description: string | null;
            isPublic: boolean;
        }) => {
            const response = await protectedApiFetch("/api/projects", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    name: payload.name,
                    description: payload.description,
                    isPublic: payload.isPublic,
                }),
            });

            if (!response.ok) {
                let message = "Failed to create project.";
                try {
                    const data = (await response.json()) as { error?: string };
                    if (data?.error) message = data.error;
                } catch {
                    // ignore json parse errors
                }
                throw new Error(message);
            }

            return (await response.json()) as Project;
        },
        onSuccess: async (project) => {
            setCreateOpen(false);
            setCreateName("");
            setCreateDescription("");
            setCreateIsPublic(false);
            setCreateError(null);

            setCurrentProject(project);
            await queryClient.invalidateQueries({
                queryKey: ["projects", userId],
            });

            if (username) {
                router.push(
                    `/${encodeURIComponent(username)}/${encodeURIComponent(project.name)}/`,
                );
            }
        },
        onError: (err) => {
            setCreateError(
                err instanceof Error
                    ? err.message
                    : "Failed to create project.",
            );
        },
    });

    const linksDisabled = !currentProject;

    return (
        <aside className="w-72 shrink-0 self-stretch border-r-2 border-black bg-slate-900/80 text-slate-200">
            <div className="h-full p-4 flex flex-col gap-6">
                {/* Project Selector */}
                <DropDown
                    label={
                        <div className="min-w-0">
                            <div className="text-xs text-slate-400">
                                Project
                            </div>
                            <div className="truncate font-medium">
                                {currentProject ? currentProject.name : "None"}
                            </div>
                        </div>
                    }
                    align="left"
                    disabled={!loggedIn}
                    className="w-full"
                    matchTriggerWidth
                >
                    {({ close }) => (
                        <div className="p-2">
                            <div className="max-h-72 overflow-auto">
                                <div className="flex flex-col gap-1">
                                    <button
                                        type="button"
                                        className={`group flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors cursor-pointer hover:bg-slate-800/70 ${
                                            !currentProject
                                                ? "bg-purple-600/15 text-slate-100"
                                                : "text-slate-200"
                                        }`}
                                        onClick={() =>
                                            handleSelectProject(null, close)
                                        }
                                    >
                                        <span className="truncate">None</span>
                                        <span
                                            className={`shrink-0 ${
                                                !currentProject
                                                    ? "text-purple-300"
                                                    : "text-slate-500 opacity-0 group-hover:opacity-100"
                                            }`}
                                            aria-hidden="true"
                                        >
                                            <IconCheck size={16} />
                                        </span>
                                    </button>

                                    {isLoading && (
                                        <div className="px-3 py-2 text-sm text-slate-400">
                                            Loading projects…
                                        </div>
                                    )}
                                    {!isLoading && error && (
                                        <div className="px-3 py-2 text-sm text-red-300">
                                            Failed to load projects.
                                        </div>
                                    )}
                                    {!isLoading &&
                                        !error &&
                                        (projects?.length ?? 0) === 0 && (
                                            <div className="px-3 py-2 text-sm text-slate-400">
                                                No projects yet.
                                            </div>
                                        )}

                                    {!isLoading &&
                                        !error &&
                                        projects?.map((project: Project) => {
                                            const selected =
                                                project.puid ===
                                                currentProject?.puid;
                                            return (
                                                <button
                                                    type="button"
                                                    key={project.puid}
                                                    className={`group flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors cursor-pointer hover:bg-slate-800/70 ${
                                                        selected
                                                            ? "bg-purple-600/15 text-slate-100"
                                                            : "text-slate-200"
                                                    }`}
                                                    onClick={() =>
                                                        handleSelectProject(
                                                            project,
                                                            close,
                                                        )
                                                    }
                                                >
                                                    <span className="truncate">
                                                        {project.name}
                                                    </span>
                                                    <span
                                                        className={`shrink-0 ${
                                                            selected
                                                                ? "text-purple-300"
                                                                : "text-slate-500 opacity-0 group-hover:opacity-100"
                                                        }`}
                                                        aria-hidden="true"
                                                    >
                                                        <IconCheck size={16} />
                                                    </span>
                                                </button>
                                            );
                                        })}
                                </div>
                            </div>

                            <div className="mt-2">
                                <button
                                    type="button"
                                    className="flex w-full items-center justify-center gap-2 rounded-lg bg-purple-600/20 px-4 py-2 text-sm font-medium text-purple-200 transition-colors cursor-pointer hover:bg-purple-600/30"
                                    onClick={() => {
                                        close();
                                        setCreateError(null);
                                        setCreateOpen(true);
                                    }}
                                >
                                    <IconPlus size={16} />
                                    Create project
                                </button>
                            </div>
                        </div>
                    )}
                </DropDown>

                {/* Quick link */}
                <Link
                    href={projectHomeHref}
                    className={`rounded-xl border px-4 py-3 text-sm transition-all ${
                        currentProject
                            ? "border-slate-700 bg-slate-900/60 hover:border-purple-500/60 hover:bg-slate-800/60"
                            : "border-slate-800 bg-slate-900/40 text-slate-500 cursor-not-allowed pointer-events-none"
                    }`}
                    aria-disabled={!currentProject}
                >
                    <div className="text-xs text-slate-400">Overview</div>
                    <div className="font-medium">
                        {currentProject
                            ? "Project Dashboard"
                            : "Select a project"}
                    </div>
                </Link>

                {/* Navigation Links */}
                <nav className="flex flex-col gap-1">
                    <div className="px-2 pb-2 text-xs font-medium uppercase tracking-wider text-slate-400">
                        Components
                    </div>
                    {navItems.map((item) => {
                        const Icon = item.icon;
                        const href = currentProject
                            ? `/${encodeURIComponent(username)}/${encodeURIComponent(currentProject.name)}/${item.slug}/`
                            : userHomeHref;
                        const active =
                            pathname === href ||
                            (pathname?.startsWith(href) && href !== "/");

                        if (linksDisabled) {
                            return (
                                <div
                                    key={item.slug}
                                    className="flex items-center gap-3 rounded-xl px-3 py-2 text-slate-500"
                                    aria-disabled="true"
                                >
                                    <Icon
                                        size={18}
                                        className="text-slate-600"
                                    />
                                    <span className="text-sm font-medium">
                                        {item.name}
                                    </span>
                                </div>
                            );
                        }

                        return (
                            <Link
                                key={item.slug}
                                href={href}
                                className={`flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium transition-all ${
                                    active
                                        ? "bg-purple-600/20 text-slate-100"
                                        : "text-slate-200 hover:bg-slate-800/60 hover:text-purple-300"
                                }`}
                            >
                                <Icon
                                    size={18}
                                    className={
                                        active
                                            ? "text-purple-300"
                                            : "text-slate-400"
                                    }
                                />
                                <span>{item.name}</span>
                            </Link>
                        );
                    })}
                </nav>

                <div className="mt-auto pt-4 border-t border-slate-800">
                    {!loggedIn && (
                        <div className="text-xs text-slate-500">
                            Log in to view projects.
                        </div>
                    )}
                </div>

                <Popup
                    open={createOpen}
                    onOpenChange={(next) => {
                        setCreateOpen(next);
                        if (next) {
                            setCreateError(null);
                            // Keep existing values if user reopens quickly; only clear on success.
                        }
                    }}
                    title="Create project"
                    description="Create a new project for your account."
                    initialFocusRef={createNameRef}
                    footer={
                        <div className="flex items-center justify-end gap-2">
                            <button
                                type="button"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                onClick={() => setCreateOpen(false)}
                                disabled={createProjectMutation.isPending}
                            >
                                Cancel
                            </button>
                            <button
                                type="button"
                                className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                onClick={() => {
                                    setCreateError(null);
                                    const trimmed = createName.trim();
                                    if (!trimmed) {
                                        setCreateError(
                                            "Project name is required.",
                                        );
                                        return;
                                    }
                                    createProjectMutation.mutate({
                                        name: trimmed,
                                        description: createDescription.trim()
                                            ? createDescription.trim()
                                            : null,
                                        isPublic: createIsPublic,
                                    });
                                }}
                                disabled={createProjectMutation.isPending}
                            >
                                {createProjectMutation.isPending
                                    ? "Creating…"
                                    : "Create"}
                            </button>
                        </div>
                    }
                >
                    <div className="flex flex-col gap-4">
                        {createError && (
                            <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                                {createError}
                            </div>
                        )}

                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Name
                            </label>
                            <input
                                ref={createNameRef}
                                value={createName}
                                onChange={(e) => setCreateName(e.target.value)}
                                placeholder="My project"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={createProjectMutation.isPending}
                            />
                        </div>

                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Description
                            </label>
                            <textarea
                                value={createDescription}
                                onChange={(e) =>
                                    setCreateDescription(e.target.value)
                                }
                                placeholder="Optional"
                                rows={3}
                                className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={createProjectMutation.isPending}
                            />
                        </div>

                        <label className="flex items-center gap-3 rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-2 text-sm text-slate-200">
                            <input
                                type="checkbox"
                                checked={createIsPublic}
                                onChange={(e) =>
                                    setCreateIsPublic(e.target.checked)
                                }
                                disabled={createProjectMutation.isPending}
                                className="h-4 w-4 accent-purple-500"
                            />
                            <div className="min-w-0">
                                <div className="font-medium">
                                    Public project
                                </div>
                                <div className="text-xs text-slate-400">
                                    Allow others to view this project.
                                </div>
                            </div>
                        </label>
                    </div>
                </Popup>
            </div>
        </aside>
    );
}
