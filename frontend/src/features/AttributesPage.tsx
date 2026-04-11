"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import SearchBar from "@/components/SearchBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { useEffect, useMemo, useRef, useState } from "react";
import {
    type NewAttributePayload,
    postNewAttribute,
    updateAttribute,
    deleteAttribute,
} from "@/lib/attributes";
import { useProtectedApi } from "@/lib/api";
import AttributeEditorDialog from "@/components/AttributeEditorDialog";
import { useAuth } from "@/context/AuthContext";
import { useProject } from "@/context/ProjectContext";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSearch } from "@/hooks/Search";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { IconCheck, IconEdit, IconPlus, IconTrash } from "@tabler/icons-react";
import ReactMarkdown from "react-markdown";
import { useAttributesQuery } from "@/hooks/useQueries";
import { type Attribute } from "@/types/attributes";

function coerceAttributes(value: unknown): Attribute[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Attribute[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Attribute[];
    }
    return [];
}

export default function Attributes() {
    const { loggedIn } = useAuth();
    const { routeUsername, routeProjectName, projectId, canEdit } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const attributesQuery = useAttributesQuery(projectId);

    const attributes = useMemo(
        () => coerceAttributes(attributesQuery.data),
        [attributesQuery.data],
    );

    const sortedAttributes = useMemo(() => {
        return [...attributes].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
        );
    }, [attributes]);

    const {
        searchText,
        setSearchText,
        filteredItems: filteredAttributes,
    } = useSearch(sortedAttributes, {
        toText: (a) => `${a.name} ${a.description ?? ""} ${a.unit ?? ""}`,
    });

    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createUnit, setCreateUnit] = useState("");
    const [createError, setCreateError] = useState<string | null>(null);
    const createNameRef = useRef<HTMLInputElement>(null);

    const createAttributeMutation = useMutation({
        mutationFn: async (payload: NewAttributePayload) => {
            if (!projectId) throw new Error("No project selected.");
            return postNewAttribute(
                projectId,
                protectedApi,
                payload,
            ) as Promise<Attribute>;
        },
        onSuccess: async () => {
            setCreateError(null);
            setCreateName("");
            setCreateDescription("");
            setCreateUnit("");
            setCreateOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["attributes", projectId],
            });
        },
        onError: (err) => {
            setCreateError(
                err instanceof Error
                    ? err.message
                    : "Failed to create attribute.",
            );
        },
    });

    const [editOpen, setEditOpen] = useState(false);
    const [editTarget, setEditTarget] = useState<Attribute | null>(null);
    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");
    const [editUnit, setEditUnit] = useState("");
    const [editError, setEditError] = useState<string | null>(null);
    const editNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!editOpen) return;
        setEditError(null);
        setEditName(editTarget?.name ?? "");
        setEditDescription(editTarget?.description ?? "");
        setEditUnit(editTarget?.unit ?? "");
    }, [editOpen, editTarget]);

    const updateAttributeMutation = useMutation({
        mutationFn: async (payload: NewAttributePayload) => {
            if (!projectId) throw new Error("No project selected.");
            if (!editTarget) throw new Error("No attribute selected.");
            return updateAttribute(
                projectId,
                editTarget.puid,
                protectedApi,
                payload,
            ) as Promise<Attribute>;
        },
        onSuccess: async () => {
            setEditError(null);
            setEditOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["attributes", projectId],
            });
        },
        onError: (err) => {
            setEditError(
                err instanceof Error
                    ? err.message
                    : "Failed to update attribute.",
            );
        },
    });

    const deleteConfirm = useDeleteConfirmation<string>({
        resetDeps: [projectId],
    });
    const [deleteError, setDeleteError] = useState<string | null>(null);

    useEffect(() => {
        setDeleteError(null);
    }, [projectId]);

    const deleteAttributeMutation = useMutation({
        mutationFn: async (puid: string) => {
            if (!projectId) throw new Error("No project selected.");
            await deleteAttribute(projectId, puid, protectedApi);
        },
        onSuccess: async () => {
            deleteConfirm.reset();
            setDeleteError(null);
            await queryClient.invalidateQueries({
                queryKey: ["attributes", projectId],
            });
        },
        onError: (err) => {
            deleteConfirm.reset();
            setDeleteError(
                err instanceof Error
                    ? err.message
                    : "Failed to delete attribute.",
            );
        },
    });

    return (
        <ProjectPageLayout>
            <div className="flex flex-col gap-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            Attributes
                        </h1>
                        <div className="mt-1 text-sm text-slate-400">
                            {routeProjectName ? (
                                <span>Project: {routeProjectName}</span>
                            ) : (
                                <span>Select a project</span>
                            )}
                            {routeUsername ? (
                                <span> • Owner: {routeUsername}</span>
                            ) : null}
                        </div>
                    </div>

                    {canEdit && (
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 self-start rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={() => {
                                setCreateError(null);
                                setCreateOpen(true);
                            }}
                            disabled={!canEdit}
                            title={
                                canEdit
                                    ? "Add attribute"
                                    : loggedIn
                                      ? "Only the project owner can manage attributes"
                                      : "Sign in to manage attributes"
                            }
                        >
                            <IconPlus size={18} />
                            Add attribute
                        </button>
                    )}
                </div>

                <ProjectStatusGate>
                    <SearchBar
                        value={searchText}
                        onChange={setSearchText}
                        disabled={!projectId}
                    />

                    <ErrorDisplay
                        errors={
                            deleteError
                                ? [
                                      {
                                          id: "delete-error",
                                          message: deleteError,
                                          onDismiss: () => setDeleteError(null),
                                      },
                                  ]
                                : []
                        }
                    />

                    {attributesQuery.isLoading && projectId && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                            Loading attributes...
                        </div>
                    )}

                    {!attributesQuery.isLoading && attributesQuery.error && (
                        <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            Failed to load attributes.
                        </div>
                    )}

                    {!attributesQuery.isLoading &&
                        !attributesQuery.error &&
                        projectId &&
                        filteredAttributes.length === 0 && (
                            <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-sm text-slate-300">
                                {searchText.trim()
                                    ? "No attributes match your search."
                                    : "No attributes yet."}
                            </div>
                        )}

                    {filteredAttributes.length > 0 && (
                        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 2xl:grid-cols-3">
                            {filteredAttributes.map((attribute) => (
                                <div
                                    key={attribute.puid}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 p-4"
                                >
                                    <div className="flex items-start justify-between gap-3">
                                        <div className="min-w-0">
                                            <div className="truncate text-base font-semibold text-slate-100">
                                                {attribute.name}
                                            </div>
                                            <div className="mt-1 text-sm text-slate-400">
                                                Unit:{" "}
                                                {attribute.unit?.trim() ||
                                                    "(none)"}
                                            </div>

                                            {attribute.description ? (
                                                <div className="mt-2 text-sm text-slate-300">
                                                    <ReactMarkdown>
                                                        {attribute.description}
                                                    </ReactMarkdown>
                                                </div>
                                            ) : (
                                                <div className="mt-2 text-sm text-slate-500">
                                                    No description
                                                </div>
                                            )}
                                        </div>

                                        {canEdit && (
                                            <div className="flex gap-2">
                                                <button
                                                    type="button"
                                                    className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                    title="Edit attribute"
                                                    aria-label="Edit attribute"
                                                    onClick={() => {
                                                        setEditTarget(
                                                            attribute,
                                                        );
                                                        setEditError(null);
                                                        setEditOpen(true);
                                                    }}
                                                    disabled={!canEdit}
                                                >
                                                    <IconEdit size={20} />
                                                </button>
                                                <button
                                                    type="button"
                                                    data-delete-confirm="true"
                                                    className={
                                                        deleteConfirm.isConfirming(
                                                            attribute.puid,
                                                        )
                                                            ? "rounded-lg border border-red-500/60 bg-red-600/30 p-2 text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                            : "rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                    }
                                                    title={
                                                        deleteConfirm.isConfirming(
                                                            attribute.puid,
                                                        )
                                                            ? "Click again to confirm"
                                                            : "Delete attribute"
                                                    }
                                                    aria-label={
                                                        deleteConfirm.isConfirming(
                                                            attribute.puid,
                                                        )
                                                            ? "Confirm delete attribute"
                                                            : "Delete attribute"
                                                    }
                                                    onClick={() => {
                                                        if (!canEdit) return;

                                                        setDeleteError(null);

                                                        deleteConfirm.confirmOrRequest(
                                                            attribute.puid,
                                                            () => {
                                                                deleteAttributeMutation.mutate(
                                                                    attribute.puid,
                                                                );
                                                            },
                                                        );
                                                    }}
                                                    disabled={
                                                        !canEdit ||
                                                        deleteAttributeMutation.isPending
                                                    }
                                                >
                                                    {deleteConfirm.isConfirming(
                                                        attribute.puid,
                                                    ) ? (
                                                        <IconCheck size={20} />
                                                    ) : (
                                                        <IconTrash size={20} />
                                                    )}
                                                </button>
                                            </div>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </ProjectStatusGate>

                <div className="mt-auto border-t border-slate-800 pt-4">
                    <div className="flex flex-wrap items-center gap-3 text-sm text-slate-400">
                        <span>Expecting something else?</span>
                        {!attributesQuery.isLoading && (
                            <button
                                type="button"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-60"
                                onClick={() => {
                                    void attributesQuery.refetch();
                                }}
                                disabled={attributesQuery.isFetching}
                            >
                                {attributesQuery.isFetching
                                    ? "Refreshing..."
                                    : "Refresh"}
                            </button>
                        )}
                    </div>
                </div>
            </div>

            <AttributeEditorDialog
                mode="create"
                open={createOpen}
                onOpenChange={(next) => {
                    setCreateOpen(next);
                    if (next) setCreateError(null);
                }}
                name={createName}
                description={createDescription}
                unit={createUnit}
                onNameChange={setCreateName}
                onDescriptionChange={setCreateDescription}
                onUnitChange={setCreateUnit}
                error={createError}
                onDismissError={() => setCreateError(null)}
                initialFocusRef={createNameRef}
                submitting={createAttributeMutation.isPending}
                submitDisabled={!canEdit || !projectId}
                onCancel={() => setCreateOpen(false)}
                onSubmit={() => {
                    setCreateError(null);
                    const trimmed = createName.trim();
                    if (!trimmed) {
                        setCreateError("Attribute name is required.");
                        return;
                    }

                    createAttributeMutation.mutate({
                        name: trimmed,
                        description: createDescription.trim()
                            ? createDescription.trim()
                            : null,
                        unit: createUnit.trim() ? createUnit.trim() : null,
                    });
                }}
            />

            <AttributeEditorDialog
                mode="edit"
                open={editOpen}
                onOpenChange={(next) => {
                    setEditOpen(next);
                    if (next) setEditError(null);
                }}
                name={editName}
                description={editDescription}
                unit={editUnit}
                onNameChange={setEditName}
                onDescriptionChange={setEditDescription}
                onUnitChange={setEditUnit}
                error={editError}
                onDismissError={() => setEditError(null)}
                initialFocusRef={editNameRef}
                submitting={updateAttributeMutation.isPending}
                submitDisabled={
                    updateAttributeMutation.isPending || !canEdit || !editTarget
                }
                onCancel={() => setEditOpen(false)}
                onSubmit={() => {
                    setEditError(null);
                    const trimmed = editName.trim();
                    if (!trimmed) {
                        setEditError("Attribute name is required.");
                        return;
                    }
                    if (!editTarget) {
                        setEditError("No attribute selected.");
                        return;
                    }

                    updateAttributeMutation.mutate({
                        name: trimmed,
                        description: editDescription.trim()
                            ? editDescription.trim()
                            : null,
                        unit: editUnit.trim() ? editUnit.trim() : null,
                    });
                }}
            />
        </ProjectPageLayout>
    );
}
