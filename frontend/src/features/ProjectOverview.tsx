"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import { useProject } from "@/context/ProjectContext";
import Popup from "@/components/Popup";
import ProjectEditorDialog from "@/components/ProjectEditorDialog";
import { IconEdit, IconTrash } from "@tabler/icons-react";
import { Link, useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/context/AuthContext";
import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useProtectedApi } from "@/lib/api";
import {
    fetchProject,
    UpsertProjectPayload,
    updateProject,
    deleteProject,
} from "@/lib/projects";
import { fetchWorkflows, type Workflow } from "@/lib/workflow";
import ReactMarkdown from "react-markdown";
import { formatTimestamp } from "@/lib/timestamp";
import { Project } from "@/types/projects";

interface SummaryItem {
    label: string;
    value: number | null;
    helper?: string;
}

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

export default function ProjectPage() {
    const {
        routeUsername,
        routeProjectName,
        currentProject,
        isOwner,
        projectQuery,
    } = useProject();
    const navigate = useNavigate();
    const queryClient = useQueryClient();

    const { userId } = useAuth();
    const protectedApi = useProtectedApi();

    const projectId = currentProject?.puid ?? "";

    const workflowsQuery = useQuery({
        queryKey: ["workflows", projectId],
        queryFn: () => fetchWorkflows(projectId, protectedApi),
        enabled: Boolean(projectId) && isOwner,
        staleTime: 60 * 1000,
    });

    const aliasProjectQuery = useQuery({
        queryKey: ["project", currentProject?.aliasProjectPuid],
        queryFn: () =>
            fetchProject(currentProject!.aliasProjectPuid!, protectedApi),
        enabled: Boolean(currentProject?.aliasProjectPuid),
        staleTime: 5 * 60 * 1000,
    });

    const productsCount = currentProject?.productCount ?? null;
    const recipesCount = currentProject?.recipeCount ?? null;
    const machinesCount = currentProject?.machineCount ?? null;
    const modifiersCount = currentProject?.modifierCount ?? null;
    const attributesCount = currentProject?.attributeCount ?? null;

    const workflows = useMemo(
        () => coerceWorkflows(workflowsQuery.data),
        [workflowsQuery.data],
    );

    const sortedWorkflows = useMemo(() => {
        return [...workflows].sort((a, b) => {
            const left = a.name?.trim() || a.puid;
            const right = b.name?.trim() || b.puid;
            return left.localeCompare(right, undefined, {
                sensitivity: "base",
            });
        });
    }, [workflows]);

    const [editOpen, setEditOpen] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);

    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");
    const [editIsPublic, setEditIsPublic] = useState(false);
    const [editError, setEditError] = useState<string | null>(null);
    const editNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!editOpen) return;
        setEditError(null);
        setEditName(currentProject?.name ?? "");
        setEditDescription(currentProject?.description ?? "");
        setEditIsPublic(Boolean(currentProject?.isPublic));
    }, [editOpen, currentProject]);

    const updateProjectMutation = useMutation({
        mutationFn: async (payload: UpsertProjectPayload) => {
            if (!currentProject) throw new Error("No project selected.");
            const updated = await updateProject(
                currentProject.puid,
                protectedApi,
                payload,
            );
            return updated as Project;
        },
        onSuccess: async (updated) => {
            setEditError(null);
            setEditOpen(false);

            await queryClient.invalidateQueries({
                queryKey: ["project", projectId],
            });
            await queryClient.invalidateQueries({
                queryKey: ["projects", userId],
            });

            if (
                routeUsername &&
                updated?.name &&
                updated.name !== routeProjectName
            ) {
                void navigate({
                    to: `/${encodeURIComponent(routeUsername)}/${encodeURIComponent(updated.name)}/`,
                    replace: true,
                });
            }
        },
        onError: (err) => {
            setEditError(
                err instanceof Error
                    ? err.message
                    : "Failed to update project.",
            );
        },
    });

    const [deleteConfirmText, setDeleteConfirmText] = useState("");
    const [deleteError, setDeleteError] = useState<string | null>(null);
    const deleteConfirmRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!deleteOpen) return;
        setDeleteError(null);
        setDeleteConfirmText("");
    }, [deleteOpen]);

    const deleteProjectMutation = useMutation({
        mutationFn: async () => {
            if (!currentProject) throw new Error("No project selected.");
            const response = await deleteProject(
                currentProject.puid,
                protectedApi,
            );
            return response as unknown;
        },
        onSuccess: async () => {
            setDeleteError(null);
            setDeleteOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["projects", userId],
            });
            if (routeUsername) {
                void navigate({ to: `/${encodeURIComponent(routeUsername)}/` });
            } else {
                void navigate({ to: "/" });
            }
        },
        onError: (err) => {
            setDeleteError(
                err instanceof Error
                    ? err.message
                    : "Failed to delete project.",
            );
        },
    });

    const summaryItems: SummaryItem[] = useMemo(
        () => [
            { label: "Products", value: productsCount },
            { label: "Recipes", value: recipesCount },
            { label: "Machines", value: machinesCount },
            { label: "Modifiers", value: modifiersCount },
            { label: "Attributes", value: attributesCount },
        ],
        [
            productsCount,
            recipesCount,
            machinesCount,
            modifiersCount,
            attributesCount,
        ],
    );

    const workflowsListHref: string = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/workflows`;
    const isProjectOverviewLoading =
        projectQuery.isLoading ||
        workflowsQuery.isLoading ||
        aliasProjectQuery.isLoading;
    const isProjectOverviewFetching =
        projectQuery.isFetching ||
        workflowsQuery.isFetching ||
        aliasProjectQuery.isFetching;

    return (
        <ProjectPageLayout>
            <div className="flex min-h-full flex-col gap-4">
                <div className="flex items-start justify-between gap-4">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            {currentProject?.name ||
                                routeProjectName ||
                                "Project"}
                        </h1>
                        <div className="mt-1 text-sm text-slate-400">
                            Owner: {routeUsername || "(unknown)"}
                            {currentProject && (
                                <span className="ml-2 rounded-md border border-slate-700 bg-slate-900/50 px-2 py-0.5 text-xs text-slate-300">
                                    {currentProject.isPublic
                                        ? "Public"
                                        : "Private"}
                                </span>
                            )}
                        </div>
                        {currentProject?.description && (
                            <div className="mt-2 text-sm text-slate-300 prose prose-invert max-w-none">
                                <ReactMarkdown>
                                    {currentProject.description}
                                </ReactMarkdown>
                            </div>
                        )}
                        {currentProject?.aliasProjectPuid && (
                            <div>
                                <div className="mt-6">
                                    <h2 className="text-lg font-semibold text-slate-100">
                                        Alias
                                    </h2>
                                    <div className="text-sm text-slate-400">
                                        This project is an alias for another
                                        project. All of components are copied
                                        from the source project, but workflows
                                        can be created independently.
                                    </div>
                                </div>
                                <div className="mt-4 flex flex-col gap-2 rounded-xl border border-blue-900/50 bg-blue-950/30 p-4 w-fit">
                                    <div className="flex items-center gap-2">
                                        <span className="rounded-md border border-blue-900/50 bg-blue-950/40 px-2 py-0.5 text-xs font-medium text-blue-200 uppercase tracking-wider">
                                            Alias Project
                                        </span>
                                    </div>
                                    {aliasProjectQuery.isLoading ? (
                                        <div className="text-sm text-blue-300/60 animate-pulse">
                                            Loading source project details...
                                        </div>
                                    ) : aliasProjectQuery.data ? (
                                        <div className="flex items-center justify-between gap-20">
                                            <div className="min-w-0">
                                                <div className="text-base font-semibold text-blue-100">
                                                    {
                                                        aliasProjectQuery.data
                                                            .name
                                                    }
                                                </div>
                                                <div className="text-sm text-blue-300">
                                                    By{" "}
                                                    {
                                                        aliasProjectQuery.data
                                                            .ownerUsername
                                                    }
                                                </div>
                                            </div>
                                            <Link
                                                to="/$username/$projectName"
                                                params={{
                                                    username:
                                                        aliasProjectQuery.data
                                                            .ownerUsername,
                                                    projectName:
                                                        aliasProjectQuery.data
                                                            .name,
                                                }}
                                                className="rounded-lg bg-blue-700/30 px-4 py-2 text-sm font-medium text-blue-100 transition-colors hover:bg-blue-700/50 focus:outline-none focus:ring-2 focus:ring-blue-500/40"
                                            >
                                                View Source
                                            </Link>
                                        </div>
                                    ) : (
                                        <div className="text-sm text-red-300/80">
                                            Failed to load source project
                                            details.
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>

                    {isOwner && (
                        <div className="flex gap-2 mt-2">
                            <button
                                type="button"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-60"
                                title="Edit project"
                                aria-label="Edit project"
                                onClick={() => setEditOpen(true)}
                                disabled={!isOwner}
                            >
                                <IconEdit size={20} />
                            </button>
                            <button
                                type="button"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-red-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-red-950/60 hover:text-red-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:opacity-60"
                                title="Delete project"
                                aria-label="Delete project"
                                onClick={() => setDeleteOpen(true)}
                                disabled={!isOwner}
                            >
                                <IconTrash size={20} />
                            </button>
                        </div>
                    )}
                </div>

                <div className="mt-6">
                    <h2 className="text-lg font-semibold text-slate-100">
                        Components
                    </h2>
                    <div className="text-sm text-slate-400">
                        Overview of the components in this project. These
                        represent the fundamental game data that can be used in
                        workflows.
                    </div>
                </div>

                <ProjectStatusGate>
                    <div className="grid gap-4 grid-cols-[repeat(auto-fit,minmax(180px,1fr))]">
                        {summaryItems.map((item) => {
                            const showPlaceholder = item.value === null;
                            const isUnknown =
                                !showPlaceholder &&
                                typeof item.value === "number" &&
                                item.value < 0;

                            const displayValue = showPlaceholder
                                ? "—"
                                : isUnknown
                                  ? "?"
                                  : String(item.value ?? 0);

                            let href: string | null = null;
                            if (item.label === "Products") {
                                href = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/products`;
                            } else if (item.label === "Recipes") {
                                href = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/recipes`;
                            } else if (item.label === "Machines") {
                                href = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/machines`;
                            } else if (item.label === "Modifiers") {
                                href = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/modifiers`;
                            } else if (item.label === "Attributes") {
                                href = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/attributes`;
                            }

                            const cardContent = (
                                <>
                                    <div className="text-xs font-medium uppercase tracking-wider text-slate-400">
                                        {item.label}
                                    </div>
                                    <div className="mt-2 text-3xl font-semibold text-slate-100">
                                        {displayValue}
                                    </div>
                                    {item.helper && (
                                        <div className="mt-1 text-xs text-slate-500">
                                            {item.helper}
                                        </div>
                                    )}
                                </>
                            );

                            return href ? (
                                <Link
                                    key={item.label}
                                    to={href}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-4 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 block"
                                >
                                    {cardContent}
                                </Link>
                            ) : (
                                <div
                                    key={item.label}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-4"
                                >
                                    {cardContent}
                                </div>
                            );
                        })}
                    </div>

                    <div className="mt-6 flex items-center justify-between gap-3">
                        <div>
                            <h2 className="text-lg font-semibold text-slate-100">
                                Workflows
                            </h2>
                            <div className="text-sm text-slate-400">
                                Key production workflow definitions for this
                                project.
                            </div>
                        </div>
                        <Link
                            to={workflowsListHref}
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm text-slate-300 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        >
                            View all
                        </Link>
                    </div>

                    {!isOwner ? (
                        <div className="mt-4 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                            Workflows can only be viewed by the project owner.
                        </div>
                    ) : (
                        <>
                            {workflowsQuery.isLoading && projectId && (
                                <div className="mt-4 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                                    Loading workflows…
                                </div>
                            )}

                            {!workflowsQuery.isLoading &&
                                workflowsQuery.error && (
                                    <div className="mt-4 rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                                        Failed to load workflows.
                                    </div>
                                )}

                            {!workflowsQuery.isLoading &&
                                !workflowsQuery.error &&
                                projectId &&
                                sortedWorkflows.length === 0 && (
                                    <div className="mt-4 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-sm text-slate-300">
                                        No workflows yet.
                                    </div>
                                )}

                            {sortedWorkflows.length > 0 && (
                                <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2 2xl:grid-cols-3">
                                    {sortedWorkflows.map((workflow) => {
                                        const workflowLabel =
                                            workflow.name?.trim() ||
                                            workflow.puid;
                                        const workflowHref: string = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/workflows/${encodeURIComponent(getWorkflowRouteSegment(workflow))}`;

                                        return (
                                            <Link
                                                key={workflow.puid}
                                                to={workflowHref}
                                                className="group rounded-xl border border-slate-800 bg-slate-900/40 p-4 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                            >
                                                <div className="flex items-start justify-between gap-3">
                                                    <div className="min-w-0">
                                                        <div className="truncate text-base font-semibold text-slate-100 group-hover:text-white">
                                                            {workflowLabel}
                                                        </div>
                                                        {workflow.description ? (
                                                            <div className="mt-1 line-clamp-3 text-sm text-slate-300">
                                                                {
                                                                    workflow.description
                                                                }
                                                            </div>
                                                        ) : (
                                                            <div className="mt-1 text-sm text-slate-500">
                                                                No description
                                                            </div>
                                                        )}
                                                        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-slate-400">
                                                            <span>
                                                                Updated{" "}
                                                                {formatTimestamp(
                                                                    workflow.updatedAt,
                                                                )}
                                                            </span>
                                                            <span>
                                                                Created{" "}
                                                                {formatTimestamp(
                                                                    workflow.createdAt,
                                                                )}
                                                            </span>
                                                        </div>
                                                    </div>
                                                    <div className="rounded-md border border-slate-700 bg-slate-900/70 px-2 py-1 text-xs text-slate-400">
                                                        Open
                                                    </div>
                                                </div>
                                            </Link>
                                        );
                                    })}
                                </div>
                            )}
                        </>
                    )}
                </ProjectStatusGate>

                <div className="mt-auto border-t border-slate-800 pt-4">
                    <div className="flex flex-wrap items-center gap-3 text-sm text-slate-400">
                        <span>Expecting something else?</span>
                        {!isProjectOverviewLoading && (
                            <button
                                type="button"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-60"
                                onClick={() => {
                                    const refetches: Promise<unknown>[] = [
                                        projectQuery.refetch(),
                                    ];

                                    if (isOwner) {
                                        refetches.push(
                                            workflowsQuery.refetch(),
                                        );
                                    }

                                    if (currentProject?.aliasProjectPuid) {
                                        refetches.push(
                                            aliasProjectQuery.refetch(),
                                        );
                                    }

                                    void Promise.all(refetches);
                                }}
                                disabled={isProjectOverviewFetching}
                            >
                                {isProjectOverviewFetching
                                    ? "Refreshing..."
                                    : "Refresh"}
                            </button>
                        )}
                    </div>
                </div>
            </div>

            <ProjectEditorDialog
                mode="edit"
                open={editOpen}
                onOpenChange={(next) => {
                    setEditOpen(next);
                    if (next) setEditError(null);
                }}
                name={editName}
                description={editDescription}
                isPublic={editIsPublic}
                onNameChange={setEditName}
                onDescriptionChange={setEditDescription}
                onIsPublicChange={setEditIsPublic}
                alias={currentProject?.aliasProjectPuid ?? null}
                error={editError}
                onDismissError={() => setEditError(null)}
                initialFocusRef={editNameRef}
                submitting={updateProjectMutation.isPending}
                submitDisabled={updateProjectMutation.isPending}
                onCancel={() => setEditOpen(false)}
                onSubmit={() => {
                    setEditError(null);
                    const trimmed = editName.trim();
                    if (!trimmed) {
                        setEditError("Project name is required.");
                        return;
                    }
                    if (!currentProject) {
                        setEditError("No project selected.");
                        return;
                    }

                    updateProjectMutation.mutate({
                        name: trimmed,
                        description: editDescription.trim()
                            ? editDescription.trim()
                            : null,
                        isPublic: editIsPublic,
                        aliasProjectPuid:
                            currentProject.aliasProjectPuid ?? null,
                    });
                }}
            />

            <Popup
                open={deleteOpen}
                onOpenChange={(next) => {
                    setDeleteOpen(next);
                    if (next) {
                        setDeleteError(null);
                        setDeleteConfirmText("");
                    }
                }}
                title="Delete project"
                description="This action cannot be undone."
                initialFocusRef={deleteConfirmRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setDeleteOpen(false)}
                            disabled={deleteProjectMutation.isPending}
                        >
                            Cancel
                        </button>
                        <button
                            type="button"
                            className="rounded-lg bg-red-600/30 px-4 py-2 text-sm font-medium text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:opacity-50"
                            onClick={() => {
                                if (
                                    deleteConfirmText.trim().toUpperCase() !==
                                    "DELETE"
                                )
                                    return;
                                deleteProjectMutation.mutate();
                            }}
                            disabled={
                                deleteProjectMutation.isPending ||
                                deleteConfirmText.trim().toUpperCase() !==
                                    "DELETE"
                            }
                        >
                            {deleteProjectMutation.isPending
                                ? "Deleting..."
                                : "Delete Project"}
                        </button>
                    </div>
                }
            >
                <div className="flex flex-col gap-4 py-2">
                    {deleteError && (
                        <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-3 py-2 text-sm text-red-200">
                            {deleteError}
                        </div>
                    )}
                    <div className="text-sm text-slate-300">
                        Are you sure you want to delete{" "}
                        <span className="font-semibold text-slate-100 italic">
                            {currentProject?.name ??
                                routeProjectName ??
                                "this project"}
                        </span>
                        ? All components and workflows associated with this
                        project will be permanently removed.
                    </div>
                    <div className="flex flex-col gap-1.5">
                        <label
                            htmlFor="delete-project-confirm"
                            className="text-xs font-medium text-slate-400"
                        >
                            Type{" "}
                            <span className="text-red-400 font-bold">
                                DELETE
                            </span>{" "}
                            to confirm
                        </label>
                        <input
                            ref={deleteConfirmRef}
                            id="delete-project-confirm"
                            type="text"
                            className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 placeholder-slate-500 focus:border-red-500/60 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                            placeholder="DELETE"
                            value={deleteConfirmText}
                            onChange={(e) =>
                                setDeleteConfirmText(e.target.value)
                            }
                            disabled={deleteProjectMutation.isPending}
                        />
                    </div>
                </div>
            </Popup>
        </ProjectPageLayout>
    );
}
