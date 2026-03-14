"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import SearchBar from "@/components/SearchBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { useEffect, useMemo, useRef, useState } from "react";
import {
    fetchModifiers,
    type NewModifierPayload,
    postNewModifier,
    updateModifier,
    deleteModifier,
} from "@/lib/modifiers";
import { useProtectedApi } from "@/lib/api";
import Popup from "@/components/Popup";
import ItemCard from "@/components/ItemCard";
import { useAuth } from "@/context/AuthContext";
import { useProject } from "@/context/ProjectContext";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearch } from "@/hooks/Search";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { IconCheck, IconEdit, IconPlus, IconTrash } from "@tabler/icons-react";
import ReactMarkdown from "react-markdown";

interface Modifier {
    puid: string;
    name: string;
    description: string | null;
    flatSpeedBonus: number;
    additivePercentBonus: number;
    multiplicativeModifier: number;
    createdAt: string;
    updatedAt: string;
}

function coerceModifiers(value: unknown): Modifier[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Modifier[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Modifier[];
    }
    return [];
}

function parseRequiredNumber(value: string): number | null {
    const trimmed = value.trim();
    if (!trimmed) return null;
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed)) return null;
    return parsed;
}

export default function Modifiers() {
    const { loggedIn } = useAuth();
    const { routeUsername, routeProjectName, projectId, canEdit } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const modifiersQuery = useQuery({
        queryKey: ["modifiers", projectId],
        queryFn: () => fetchModifiers(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const modifiers = useMemo(
        () => coerceModifiers(modifiersQuery.data),
        [modifiersQuery.data],
    );

    const sortedModifiers = useMemo(() => {
        return [...modifiers].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
        );
    }, [modifiers]);

    const {
        searchText,
        setSearchText,
        filteredItems: filteredModifiers,
    } = useSearch(sortedModifiers, {
        toText: (m) =>
            `${m.name} ${m.description ?? ""} ${m.flatSpeedBonus} ${m.additivePercentBonus} ${m.multiplicativeModifier}`,
    });

    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createFlatSpeedBonus, setCreateFlatSpeedBonus] = useState("0");
    const [createAdditivePercentBonus, setCreateAdditivePercentBonus] =
        useState("0");
    const [createMultiplicativeModifier, setCreateMultiplicativeModifier] =
        useState("1");
    const [createError, setCreateError] = useState<string | null>(null);
    const createNameRef = useRef<HTMLInputElement>(null);

    const createModifierMutation = useMutation({
        mutationFn: async (payload: NewModifierPayload) => {
            if (!projectId) throw new Error("No project selected.");
            return postNewModifier(
                projectId,
                protectedApi,
                payload,
            ) as Promise<Modifier>;
        },
        onSuccess: async () => {
            setCreateError(null);
            setCreateName("");
            setCreateDescription("");
            setCreateFlatSpeedBonus("0");
            setCreateAdditivePercentBonus("0");
            setCreateMultiplicativeModifier("1");
            setCreateOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["modifiers", projectId],
            });
        },
        onError: (err) => {
            setCreateError(
                err instanceof Error
                    ? err.message
                    : "Failed to create modifier.",
            );
        },
    });

    const [editOpen, setEditOpen] = useState(false);
    const [editTarget, setEditTarget] = useState<Modifier | null>(null);
    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");
    const [editFlatSpeedBonus, setEditFlatSpeedBonus] = useState("0");
    const [editAdditivePercentBonus, setEditAdditivePercentBonus] =
        useState("0");
    const [editMultiplicativeModifier, setEditMultiplicativeModifier] =
        useState("1");
    const [editError, setEditError] = useState<string | null>(null);
    const editNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!editOpen) return;
        setEditError(null);
        setEditName(editTarget?.name ?? "");
        setEditDescription(editTarget?.description ?? "");
        setEditFlatSpeedBonus(
            editTarget ? String(editTarget.flatSpeedBonus) : "0",
        );
        setEditAdditivePercentBonus(
            editTarget ? String(editTarget.additivePercentBonus) : "0",
        );
        setEditMultiplicativeModifier(
            editTarget ? String(editTarget.multiplicativeModifier) : "1",
        );
    }, [editOpen, editTarget]);

    const updateModifierMutation = useMutation({
        mutationFn: async (payload: NewModifierPayload) => {
            if (!projectId) throw new Error("No project selected.");
            if (!editTarget) throw new Error("No modifier selected.");
            return updateModifier(
                projectId,
                editTarget.puid,
                protectedApi,
                payload,
            ) as Promise<Modifier>;
        },
        onSuccess: async () => {
            setEditError(null);
            setEditOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["modifiers", projectId],
            });
        },
        onError: (err) => {
            setEditError(
                err instanceof Error
                    ? err.message
                    : "Failed to update modifier.",
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

    const deleteModifierMutation = useMutation({
        mutationFn: async (puid: string) => {
            if (!projectId) throw new Error("No project selected.");
            await deleteModifier(projectId, puid, protectedApi);
        },
        onSuccess: async () => {
            deleteConfirm.reset();
            setDeleteError(null);
            await queryClient.invalidateQueries({
                queryKey: ["modifiers", projectId],
            });
        },
        onError: (err) => {
            deleteConfirm.reset();
            setDeleteError(
                err instanceof Error
                    ? err.message
                    : "Failed to delete modifier.",
            );
        },
    });

    return (
        <ProjectPageLayout>
            <div className="flex flex-col gap-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            Modifiers
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
                                ? "Add modifier"
                                : loggedIn
                                  ? "Only the project owner can manage modifiers"
                                  : "Sign in to manage modifiers"
                        }
                    >
                        <IconPlus size={18} />
                        Add modifier
                    </button>
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

                    {modifiersQuery.isLoading && projectId && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                            Loading modifiers…
                        </div>
                    )}

                    {!modifiersQuery.isLoading && modifiersQuery.error && (
                        <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            Failed to load modifiers.
                        </div>
                    )}

                    {!modifiersQuery.isLoading &&
                        !modifiersQuery.error &&
                        projectId &&
                        filteredModifiers.length === 0 && (
                            <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-sm text-slate-300">
                                {searchText.trim()
                                    ? "No modifiers match your search."
                                    : "No modifiers yet."}
                            </div>
                        )}

                    {filteredModifiers.length > 0 && (
                        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                            {filteredModifiers.map((modifier) => (
                                <div
                                    key={modifier.puid}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 p-4"
                                >
                                    <div className="flex items-start justify-between gap-3">
                                        <div className="min-w-0">
                                            <div className="truncate text-base font-semibold text-slate-100">
                                                {modifier.name}
                                            </div>

                                            {modifier.description ? (
                                                <div className="mt-1 text-sm text-slate-300">
                                                    <ReactMarkdown>
                                                        {modifier.description}
                                                    </ReactMarkdown>
                                                </div>
                                            ) : (
                                                <div className="mt-1 text-sm text-slate-500">
                                                    No description
                                                </div>
                                            )}

                                            <div className="mt-3 grid grid-cols-1 gap-2 text-sm text-slate-300 sm:grid-cols-3">
                                                <div>
                                                    <span className="text-slate-400">
                                                        Flat speed:
                                                    </span>{" "}
                                                    {modifier.flatSpeedBonus}
                                                </div>
                                                <div>
                                                    <span className="text-slate-400">
                                                        Additive %:
                                                    </span>{" "}
                                                    {
                                                        modifier.additivePercentBonus
                                                    }
                                                </div>
                                                <div>
                                                    <span className="text-slate-400">
                                                        Multiplier:
                                                    </span>{" "}
                                                    {
                                                        modifier.multiplicativeModifier
                                                    }
                                                </div>
                                            </div>
                                        </div>

                                        <div className="flex gap-2">
                                            <button
                                                type="button"
                                                className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                title="Edit modifier"
                                                aria-label="Edit modifier"
                                                onClick={() => {
                                                    setEditTarget(modifier);
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
                                                        modifier.puid,
                                                    )
                                                        ? "rounded-lg border border-red-500/60 bg-red-600/30 p-2 text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                        : "rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                }
                                                title={
                                                    deleteConfirm.isConfirming(
                                                        modifier.puid,
                                                    )
                                                        ? "Click again to confirm"
                                                        : "Delete modifier"
                                                }
                                                aria-label={
                                                    deleteConfirm.isConfirming(
                                                        modifier.puid,
                                                    )
                                                        ? "Confirm delete modifier"
                                                        : "Delete modifier"
                                                }
                                                onClick={() => {
                                                    if (!canEdit) return;
                                                    setDeleteError(null);
                                                    deleteConfirm.confirmOrRequest(
                                                        modifier.puid,
                                                        () => {
                                                            deleteModifierMutation.mutate(
                                                                modifier.puid,
                                                            );
                                                        },
                                                    );
                                                }}
                                                disabled={
                                                    !canEdit ||
                                                    deleteModifierMutation.isPending
                                                }
                                            >
                                                {deleteConfirm.isConfirming(
                                                    modifier.puid,
                                                ) ? (
                                                    <IconCheck size={20} />
                                                ) : (
                                                    <IconTrash size={20} />
                                                )}
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </ProjectStatusGate>
            </div>

            <ItemCard
                open={createOpen}
                onOpenChange={(next) => {
                    setCreateOpen(next);
                    if (next) setCreateError(null);
                }}
                title="Add modifier"
                description="Create a new modifier in this project."
                initialFocusRef={createNameRef}
                submitLabel="Create"
                submittingLabel="Creating…"
                submitting={createModifierMutation.isPending}
                submitDisabled={!canEdit || !projectId}
                cancelDisabled={createModifierMutation.isPending}
                onCancel={() => setCreateOpen(false)}
                onSubmit={() => {
                    setCreateError(null);
                    const trimmed = createName.trim();
                    if (!trimmed) {
                        setCreateError("Modifier name is required.");
                        return;
                    }

                    const flat = parseRequiredNumber(createFlatSpeedBonus);
                    if (flat === null) {
                        setCreateError("Flat speed bonus is required.");
                        return;
                    }

                    const additive = parseRequiredNumber(
                        createAdditivePercentBonus,
                    );
                    if (additive === null) {
                        setCreateError("Additive percent bonus is required.");
                        return;
                    }

                    const mult = parseRequiredNumber(
                        createMultiplicativeModifier,
                    );
                    if (mult === null) {
                        setCreateError("Multiplicative modifier is required.");
                        return;
                    }

                    createModifierMutation.mutate({
                        name: trimmed,
                        description: createDescription.trim()
                            ? createDescription.trim()
                            : null,
                        flatSpeedBonus: flat,
                        additivePercentBonus: additive,
                        multiplicativeModifier: mult,
                    });
                }}
            >
                <div className="flex flex-col gap-4">
                    <ErrorDisplay
                        errors={
                            createError
                                ? [
                                      {
                                          id: "create-error",
                                          message: createError,
                                          onDismiss: () => setCreateError(null),
                                      },
                                  ]
                                : []
                        }
                    />

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Name
                        </label>
                        <input
                            ref={createNameRef}
                            value={createName}
                            onChange={(e) => setCreateName(e.target.value)}
                            placeholder="Overclock"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createModifierMutation.isPending}
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
                            disabled={createModifierMutation.isPending}
                        />
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Flat speed bonus
                            </label>
                            <input
                                type="number"
                                step="any"
                                value={createFlatSpeedBonus}
                                onChange={(e) =>
                                    setCreateFlatSpeedBonus(e.target.value)
                                }
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={createModifierMutation.isPending}
                            />
                        </div>

                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Additive percent bonus
                            </label>
                            <input
                                type="number"
                                step="any"
                                value={createAdditivePercentBonus}
                                onChange={(e) =>
                                    setCreateAdditivePercentBonus(
                                        e.target.value,
                                    )
                                }
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={createModifierMutation.isPending}
                            />
                        </div>

                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Multiplicative modifier
                            </label>
                            <input
                                type="number"
                                step="any"
                                value={createMultiplicativeModifier}
                                onChange={(e) =>
                                    setCreateMultiplicativeModifier(
                                        e.target.value,
                                    )
                                }
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={createModifierMutation.isPending}
                            />
                        </div>
                    </div>
                </div>
            </ItemCard>

            <Popup
                open={editOpen}
                onOpenChange={(next) => {
                    setEditOpen(next);
                    if (next) setEditError(null);
                }}
                title="Edit modifier"
                description="Update modifier details."
                initialFocusRef={editNameRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setEditOpen(false)}
                            disabled={updateModifierMutation.isPending}
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
                                    setEditError("Modifier name is required.");
                                    return;
                                }
                                if (!editTarget) {
                                    setEditError("No modifier selected.");
                                    return;
                                }

                                const flat =
                                    parseRequiredNumber(editFlatSpeedBonus);
                                if (flat === null) {
                                    setEditError(
                                        "Flat speed bonus is required.",
                                    );
                                    return;
                                }

                                const additive = parseRequiredNumber(
                                    editAdditivePercentBonus,
                                );
                                if (additive === null) {
                                    setEditError(
                                        "Additive percent bonus is required.",
                                    );
                                    return;
                                }

                                const mult = parseRequiredNumber(
                                    editMultiplicativeModifier,
                                );
                                if (mult === null) {
                                    setEditError(
                                        "Multiplicative modifier is required.",
                                    );
                                    return;
                                }

                                updateModifierMutation.mutate({
                                    name: trimmed,
                                    description: editDescription.trim()
                                        ? editDescription.trim()
                                        : null,
                                    flatSpeedBonus: flat,
                                    additivePercentBonus: additive,
                                    multiplicativeModifier: mult,
                                });
                            }}
                            disabled={
                                updateModifierMutation.isPending ||
                                !canEdit ||
                                !editTarget
                            }
                        >
                            {updateModifierMutation.isPending
                                ? "Saving…"
                                : "Save"}
                        </button>
                    </div>
                }
            >
                <div className="flex flex-col gap-4">
                    <ErrorDisplay
                        errors={
                            editError
                                ? [
                                      {
                                          id: "edit-error",
                                          message: editError,
                                          onDismiss: () => setEditError(null),
                                      },
                                  ]
                                : []
                        }
                    />

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Name
                        </label>
                        <input
                            ref={editNameRef}
                            value={editName}
                            onChange={(e) => setEditName(e.target.value)}
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={updateModifierMutation.isPending}
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
                            disabled={updateModifierMutation.isPending}
                        />
                    </div>

                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Flat speed bonus
                            </label>
                            <input
                                type="number"
                                step="any"
                                value={editFlatSpeedBonus}
                                onChange={(e) =>
                                    setEditFlatSpeedBonus(e.target.value)
                                }
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={updateModifierMutation.isPending}
                            />
                        </div>

                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Additive percent bonus
                            </label>
                            <input
                                type="number"
                                step="any"
                                value={editAdditivePercentBonus}
                                onChange={(e) =>
                                    setEditAdditivePercentBonus(e.target.value)
                                }
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={updateModifierMutation.isPending}
                            />
                        </div>

                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-slate-200">
                                Multiplicative modifier
                            </label>
                            <input
                                type="number"
                                step="any"
                                value={editMultiplicativeModifier}
                                onChange={(e) =>
                                    setEditMultiplicativeModifier(
                                        e.target.value,
                                    )
                                }
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                disabled={updateModifierMutation.isPending}
                            />
                        </div>
                    </div>
                </div>
            </Popup>
        </ProjectPageLayout>
    );
}
