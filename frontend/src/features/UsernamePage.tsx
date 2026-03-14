"use client";

import CreateProject from "@/components/CreateProject";
import ErrorDisplay from "@/components/ErrorDisplay";
import ProjectPageLayout from "@/components/ProjectPageLayout";
import { useAuth } from "@/context/AuthContext";
import { useRouteParams } from "@/hooks/useRouteParams";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { useProtectedApi } from "@/lib/api";
import { fetchProject, resolveProject } from "@/lib/projects";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "@tanstack/react-router";
import { useMemo, useState } from "react";

interface ResolvedProject {
    projectName: string;
    projectPuid: string;
}

interface Project {
    puid: string;
    name: string;
    ownerUsername: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
    createdAt: string;
    updatedAt: string;
}

interface UserProjectsData {
    resolvedProjects: ResolvedProject[];
    projects: Project[];
    aliasProjects: Record<string, Project>;
    detailErrors: string[];
}

function formatTimestamp(value: string): string {
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return "Unknown";
    return parsed.toLocaleString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
}

function errorMessage(error: unknown, fallback: string): string {
    if (error instanceof Error && error.message.trim()) {
        return error.message;
    }
    return fallback;
}

export default function ProjectHomePage() {
    const { username } = useRouteParams();
    const [createOpen, setCreateOpen] = useState(false);
    const [deleteError, setDeleteError] = useState<string | null>(null);

    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const protectedApi = useProtectedApi();
    const { loggedIn, username: authedUsername } = useAuth();

    const isOwnPage =
        loggedIn &&
        Boolean(username) &&
        Boolean(authedUsername) &&
        username.toLowerCase() === authedUsername?.toLowerCase();

    const userProjectsQuery = useQuery<UserProjectsData>({
        queryKey: ["username-projects", username],
        queryFn: async () => {
            const resolvedProjects = await resolveProject(
                username,
                undefined,
                protectedApi,
            );

            const uniqueByPuid = new Map<string, ResolvedProject>();
            for (const projectRef of resolvedProjects) {
                if (!projectRef.projectPuid) continue;
                if (!uniqueByPuid.has(projectRef.projectPuid)) {
                    uniqueByPuid.set(projectRef.projectPuid, projectRef);
                }
            }

            const dedupedProjects = Array.from(uniqueByPuid.values());

            const settled = await Promise.allSettled(
                dedupedProjects.map(async (projectRef) => {
                    const project = (await fetchProject(
                        projectRef.projectPuid,
                        protectedApi,
                    )) as Project;
                    return project;
                }),
            );

            const projects: Project[] = [];
            const aliasProjects: Record<string, Project> = {};
            const detailErrors: string[] = [];

            for (let index = 0; index < settled.length; index++) {
                const result = settled[index];
                if (result.status === "fulfilled") {
                    const project = result.value;
                    projects.push(project);

                    // Fetch alias project details if applicable
                    if (project.aliasProjectPuid) {
                        try {
                            const alias = (await fetchProject(
                                project.aliasProjectPuid,
                                protectedApi,
                            )) as Project;
                            aliasProjects[project.aliasProjectPuid] = alias;
                        } catch (err) {
                            console.error(
                                `Failed to fetch alias ${project.aliasProjectPuid}:`,
                                err,
                            );
                        }
                    }
                    continue;
                }

                const fallbackName =
                    dedupedProjects[index]?.projectName ?? "project";
                detailErrors.push(
                    `Failed to load details for ${fallbackName}: ${errorMessage(result.reason, "Unknown error")}`,
                );
            }

            projects.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));

            return {
                resolvedProjects: dedupedProjects,
                projects,
                aliasProjects,
                detailErrors,
            };
        },
        enabled: Boolean(username),
        staleTime: 60 * 1000,
    });

    const headerText = useMemo(() => {
        if (!username) return "User projects";
        return `${username}'s projects`;
    }, [username]);

    const hasProjects = (userProjectsQuery.data?.projects.length ?? 0) > 0;
    const resolvedCount = userProjectsQuery.data?.resolvedProjects.length ?? 0;
    const loadedCount = userProjectsQuery.data?.projects.length ?? 0;
    const unresolvedCount = Math.max(0, resolvedCount - loadedCount);

    return (
        <ProjectPageLayout>
            <div className="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-6">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            {headerText}
                        </h1>
                        <p className="mt-1 text-sm text-slate-400">
                            {isOwnPage
                                ? "Manage your projects and jump into any project workspace."
                                : "Browse this user's public projects."}
                        </p>
                    </div>

                    {isOwnPage && (
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 self-start rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setCreateOpen(true)}
                        >
                            Create a Project
                        </button>
                    )}
                </div>

                <ErrorDisplay
                    errors={[
                        deleteError
                            ? {
                                  id: "delete-project-error",
                                  message: deleteError,
                                  onDismiss: () => setDeleteError(null),
                              }
                            : null,
                        userProjectsQuery.error
                            ? {
                                  id: "resolve-projects-error",
                                  message: errorMessage(
                                      userProjectsQuery.error,
                                      "Failed to load projects.",
                                  ),
                              }
                            : null,
                        ...(userProjectsQuery.data?.detailErrors ?? []).map(
                            (message, index) => ({
                                id: `project-detail-error-${index}`,
                                message,
                            }),
                        ),
                    ]}
                />

                {userProjectsQuery.isLoading && (
                    <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                        Loading projects...
                    </div>
                )}

                {!userProjectsQuery.isLoading && !userProjectsQuery.error && (
                    <>
                        {resolvedCount > 0 && unresolvedCount > 0 && (
                            <div className="rounded-xl border border-amber-900/50 bg-amber-950/30 px-4 py-3 text-sm text-amber-200">
                                Loaded {loadedCount} of {resolvedCount}{" "}
                                projects. {unresolvedCount} project
                                {unresolvedCount === 1 ? "" : "s"} could not be
                                fully loaded.
                            </div>
                        )}

                        {!hasProjects && (
                            <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                                {isOwnPage
                                    ? "You have not created any projects yet."
                                    : "This user does not have any public projects yet."}
                            </div>
                        )}

                        {hasProjects && (
                            <div className="overflow-hidden rounded-xl border border-slate-800 bg-slate-900/40">
                                {userProjectsQuery.data?.projects.map(
                                    (project, index) => {
                                        return (
                                            <div
                                                key={project.puid}
                                                className={`flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-start sm:justify-between ${
                                                    index <
                                                    (userProjectsQuery.data
                                                        ?.projects.length ??
                                                        1) -
                                                        1
                                                        ? "border-b border-slate-800"
                                                        : ""
                                                }`}
                                            >
                                                <div className="min-w-0">
                                                    <Link
                                                        to="/$username/$projectName"
                                                        params={{
                                                            username,
                                                            projectName:
                                                                project.name,
                                                        }}
                                                        className="text-base font-semibold text-purple-300 transition-colors hover:text-purple-200"
                                                    >
                                                        {project.name}
                                                    </Link>
                                                    <div className="mt-1 text-sm text-slate-300">
                                                        {project.description?.trim() ||
                                                            "No description provided."}
                                                    </div>
                                                    <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-slate-400">
                                                        <span className="rounded-md border border-slate-700 bg-slate-900/60 px-2 py-0.5 text-slate-300">
                                                            {project.isPublic
                                                                ? "Public"
                                                                : "Private"}
                                                        </span>
                                                        {project.aliasProjectPuid && (
                                                            <>
                                                                <span className="rounded-md border border-blue-900/50 bg-blue-950/40 px-2 py-0.5 text-blue-200">
                                                                    Alias
                                                                </span>
                                                                {userProjectsQuery
                                                                    .data
                                                                    ?.aliasProjects?.[
                                                                    project
                                                                        .aliasProjectPuid
                                                                ] && (
                                                                    <Link
                                                                        to="/$username/$projectName"
                                                                        params={{
                                                                            username:
                                                                                userProjectsQuery
                                                                                    .data
                                                                                    .aliasProjects?.[
                                                                                    project
                                                                                        .aliasProjectPuid
                                                                                ]
                                                                                    ?.ownerUsername ??
                                                                                "",
                                                                            projectName:
                                                                                userProjectsQuery
                                                                                    .data
                                                                                    .aliasProjects?.[
                                                                                    project
                                                                                        .aliasProjectPuid
                                                                                ]
                                                                                    ?.name ??
                                                                                "",
                                                                        }}
                                                                        className="text-blue-400 underline decoration-blue-900/50 transition-colors hover:text-blue-300"
                                                                    >
                                                                        Original
                                                                        Project:{" "}
                                                                        {
                                                                            userProjectsQuery
                                                                                .data
                                                                                .aliasProjects?.[
                                                                                project
                                                                                    .aliasProjectPuid
                                                                            ]
                                                                                ?.ownerUsername
                                                                        }
                                                                        /
                                                                        {
                                                                            userProjectsQuery
                                                                                .data
                                                                                .aliasProjects?.[
                                                                                project
                                                                                    .aliasProjectPuid
                                                                            ]
                                                                                ?.name
                                                                        }
                                                                    </Link>
                                                                )}
                                                            </>
                                                        )}
                                                    </div>
                                                    <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-slate-400">
                                                        <span>
                                                            Updated{" "}
                                                            {formatTimestamp(
                                                                project.updatedAt,
                                                            )}
                                                        </span>
                                                        <span>
                                                            Created{" "}
                                                            {formatTimestamp(
                                                                project.createdAt,
                                                            )}
                                                        </span>
                                                    </div>
                                                </div>

                                                <div className="flex items-center gap-2 sm:pt-1">
                                                    <Link
                                                        to="/$username/$projectName"
                                                        params={{
                                                            username,
                                                            projectName:
                                                                project.name,
                                                        }}
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60"
                                                    >
                                                        Open
                                                    </Link>
                                                </div>
                                            </div>
                                        );
                                    },
                                )}
                            </div>
                        )}
                    </>
                )}

                <div className="mt-auto border-t border-slate-800 pt-4">
                    <div className="flex flex-wrap items-center gap-3 text-sm text-slate-400">
                        <span>Looking for more projects?</span>
                        <Link
                            to="/explore"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm font-medium text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        >
                            Go to Explore
                        </Link>
                        {!userProjectsQuery.isLoading && (
                            <button
                                type="button"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-60"
                                onClick={() => {
                                    void userProjectsQuery.refetch();
                                }}
                                disabled={userProjectsQuery.isFetching}
                            >
                                {userProjectsQuery.isFetching
                                    ? "Refreshing..."
                                    : "Refresh"}
                            </button>
                        )}
                    </div>
                </div>

                <CreateProject
                    open={createOpen}
                    onOpenChange={setCreateOpen}
                    username={username}
                    onCreated={(project) => {
                        void queryClient.invalidateQueries({
                            queryKey: ["username-projects", username],
                            exact: true,
                        });

                        if (username) {
                            void navigate({
                                to: `/${encodeURIComponent(username)}/${encodeURIComponent(project.name)}/`,
                            });
                        }
                    }}
                />
            </div>
        </ProjectPageLayout>
    );
}
