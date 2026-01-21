"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import { useProject } from "@/context/ProjectContext";
import Popup from "@/components/Popup";
import { IconEdit, IconTrash } from "@tabler/icons-react";
import { useParams, useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import React, { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useProtectedApi } from "@/lib/api";
import {
    fetchProjects,
    UpsertProjectPayload,
    updateProject,
    deleteProject,
} from "@/lib/projects";
import { fetchProducts } from "@/lib/products";
import { fetchRecipes } from "@/lib/recipes";
import { fetchMachines } from "@/lib/machines";
import { fetchModifiers } from "@/lib/modifiers";
import ReactMarkdown from "react-markdown";

interface Project {
    puid: string;
    name: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
    createdAt: string;
    updatedAt: string;
}

interface SummaryItem {
    label: string;
    value: number | null;
    helper?: string;
}

function getCount(value: unknown): number | null {
    if (!value) return 0;
    if (Array.isArray(value)) return value.length;
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems.length;
    }
    return null;
}

export default function ProjectPage() {
    const { routeUsername, routeProjectName, currentProject, projectsQuery } =
        useProject();
    const router = useRouter();
    const queryClient = useQueryClient();

    const { userId, loggedIn } = useAuth();
    const protectedApi = useProtectedApi();

    const projectId = currentProject?.puid ?? "";

    const productsQuery = useQuery({
        queryKey: ["products", projectId],
        queryFn: () => fetchProducts(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const recipesQuery = useQuery({
        queryKey: ["recipes", projectId],
        queryFn: () => fetchRecipes(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const machinesQuery = useQuery({
        queryKey: ["machines", projectId],
        queryFn: () => fetchMachines(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const modifiersQuery = useQuery({
        queryKey: ["modifiers", projectId],
        queryFn: () => fetchModifiers(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const productsCount = getCount(productsQuery.data);
    const recipesCount = getCount(recipesQuery.data);
    const machinesCount = getCount(machinesQuery.data);
    const modifiersCount = getCount(modifiersQuery.data);
    const workflowsCount: number | null = null;

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
                queryKey: ["projects", userId],
            });

            if (
                routeUsername &&
                updated?.name &&
                updated.name !== routeProjectName
            ) {
                router.replace(
                    `/${encodeURIComponent(routeUsername)}/${encodeURIComponent(updated.name)}/`,
                );
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
                router.push(`/${encodeURIComponent(routeUsername)}/`);
            } else {
                router.push("/");
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
            {
                label: "Workflows",
                value: workflowsCount,
                helper: "Coming soon",
            },
            { label: "Products", value: productsCount },
            { label: "Recipes", value: recipesCount },
            { label: "Machines", value: machinesCount },
            { label: "Modifiers", value: modifiersCount },
        ],
        [
            workflowsCount,
            productsCount,
            recipesCount,
            machinesCount,
            modifiersCount,
        ],
    );

    const countsLoading =
        productsQuery.isLoading ||
        recipesQuery.isLoading ||
        machinesQuery.isLoading ||
        modifiersQuery.isLoading;

    const countsError =
        productsQuery.error ||
        recipesQuery.error ||
        machinesQuery.error ||
        modifiersQuery.error;

    return (
        <ProjectPageLayout>
            <div className="flex flex-col gap-4">
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
                    </div>

                    <div className="flex gap-2 mt-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-60"
                            title="Edit project"
                            aria-label="Edit project"
                            onClick={() => setEditOpen(true)}
                            disabled={!loggedIn || !currentProject}
                        >
                            <IconEdit size={20} />
                        </button>
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-red-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-red-950/60 hover:text-red-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:opacity-60"
                            title="Delete project"
                            aria-label="Delete project"
                            onClick={() => setDeleteOpen(true)}
                            disabled={!loggedIn || !currentProject}
                        >
                            <IconTrash size={20} />
                        </button>
                    </div>
                </div>

                {projectsQuery.isLoading && (
                    <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                        Loading project…
                    </div>
                )}
                {!projectsQuery.isLoading && projectsQuery.error !== null && (
                    <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                        Failed to load projects.
                    </div>
                )}
                {!projectsQuery.isLoading &&
                    !projectsQuery.error &&
                    routeProjectName &&
                    !currentProject && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-300">
                            Project not found: {routeProjectName}
                        </div>
                    )}

                <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
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

                        return (
                            <div
                                key={item.label}
                                className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-4"
                            >
                                <div className="text-xs font-medium uppercase tracking-wider text-slate-400">
                                    {item.label}
                                </div>
                                <div className="mt-2 text-3xl font-semibold text-slate-100">
                                    {countsLoading && item.label !== "Workflows"
                                        ? "…"
                                        : displayValue}
                                </div>
                                {item.helper && (
                                    <div className="mt-1 text-xs text-slate-500">
                                        {item.helper}
                                    </div>
                                )}
                            </div>
                        );
                    })}
                </div>

                {countsError && (
                    <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                        Failed to load one or more project components.
                    </div>
                )}
            </div>

            <Popup
                open={editOpen}
                onOpenChange={(next) => {
                    setEditOpen(next);
                    if (next) setEditError(null);
                }}
                title="Edit project"
                description="Update project settings."
                initialFocusRef={editNameRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setEditOpen(false)}
                            disabled={updateProjectMutation.isPending}
                        >
                            Cancel
                        </button>
                        <button
                            type="button"
                            className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={() => {
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
                            disabled={updateProjectMutation.isPending}
                        >
                            {updateProjectMutation.isPending
                                ? "Saving…"
                                : "Save"}
                        </button>
                    </div>
                }
            >
                <div className="flex flex-col gap-4">
                    {editError && (
                        <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            {editError}
                        </div>
                    )}

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Name
                        </label>
                        <input
                            ref={editNameRef}
                            value={editName}
                            onChange={(e) => setEditName(e.target.value)}
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={updateProjectMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Description
                        </label>
                        <textarea
                            value={editDescription}
                            onChange={(e) => setEditDescription(e.target.value)}
                            rows={3}
                            className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={updateProjectMutation.isPending}
                        />
                    </div>

                    <label className="flex items-center gap-3 rounded-lg border border-slate-800 cursor-pointer bg-slate-900/40 px-3 py-2 text-sm text-slate-200">
                        <input
                            type="checkbox"
                            checked={editIsPublic}
                            onChange={(e) => setEditIsPublic(e.target.checked)}
                            disabled={updateProjectMutation.isPending}
                            className="h-4 w-4 accent-purple-500 cursor-pointer"
                        />
                        <div className="min-w-0">
                            <div className="font-medium">Public project</div>
                            <div className="text-xs text-slate-400">
                                Allow others to view this project.
                            </div>
                        </div>
                    </label>
                </div>
            </Popup>

            <Popup
                open={deleteOpen}
                onOpenChange={(next) => {
                    setDeleteOpen(next);
                    if (next) setDeleteError(null);
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
                            className="rounded-lg bg-red-600/30 px-4 py-2 text-sm font-medium text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={() => {
                                setDeleteError(null);
                                if (deleteConfirmText.trim() !== "DELETE") {
                                    setDeleteError(
                                        'Type "DELETE" to confirm deletion.',
                                    );
                                    return;
                                }
                                deleteProjectMutation.mutate();
                            }}
                            disabled={
                                deleteProjectMutation.isPending ||
                                deleteConfirmText.trim() !== "DELETE"
                            }
                        >
                            {deleteProjectMutation.isPending
                                ? "Deleting…"
                                : "Delete"}
                        </button>
                    </div>
                }
            >
                <div className="flex flex-col gap-4">
                    {deleteError && (
                        <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            {deleteError}
                        </div>
                    )}

                    <div className="text-sm text-slate-300">
                        Deleting{" "}
                        <span className="font-semibold text-slate-100">
                            {currentProject?.name ??
                                routeProjectName ??
                                "this project"}
                        </span>{" "}
                        will remove all associated data.
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Type <span className="font-mono">DELETE</span> to
                            confirm
                        </label>
                        <input
                            ref={deleteConfirmRef}
                            value={deleteConfirmText}
                            onChange={(e) =>
                                setDeleteConfirmText(e.target.value)
                            }
                            placeholder="DELETE"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                            disabled={deleteProjectMutation.isPending}
                        />
                    </div>
                </div>
            </Popup>
        </ProjectPageLayout>
    );
}
