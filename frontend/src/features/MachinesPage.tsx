"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import DropDown from "@/components/DropDown";
import ItemCard from "@/components/ItemCard";
import Popup from "@/components/Popup";
import SearchBar from "@/components/SearchBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { useProject } from "@/context/ProjectContext";
import { useProtectedApi } from "@/lib/api";
import {
    deleteMachine,
    fetchMachines,
    postNewMachine,
    type NewMachinePayload,
    updateMachine,
} from "@/lib/machines";
import { fetchRecipes } from "@/lib/recipes";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { useSearch } from "@/hooks/Search";
import {
    IconCheck,
    IconEdit,
    IconPlus,
    IconSearch,
    IconTrash,
} from "@tabler/icons-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";

interface Recipe {
    puid: string;
    name: string;
    description: string | null;
    baseCraftingTime: number;
    createdAt: string;
    updatedAt: string;
}

interface Machine {
    puid: string;
    name: string;
    description: string | null;
    baseSpeed: number;
    recipePuids: string[];
    createdAt: string;
    updatedAt: string;
}

function coerceMachines(value: unknown): Machine[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Machine[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Machine[];
    }
    return [];
}

function coerceRecipes(value: unknown): Recipe[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Recipe[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Recipe[];
    }
    return [];
}

function normalizeStringArray(value: unknown): string[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as string[];
    return [];
}

function formatSelectedRecipesLabel(
    selectedPuids: string[],
    recipeNameByPuid: Map<string, string>,
    recipesCount: number,
): string {
    if (recipesCount === 0) return "No recipes";
    if (selectedPuids.length === 0) return "Select recipes";
    if (selectedPuids.length === 1) {
        return recipeNameByPuid.get(selectedPuids[0]) ?? "1 selected";
    }
    return `${selectedPuids.length} selected`;
}

function uniqueTrimmedPuids(values: string[]): string[] {
    const out: string[] = [];
    const seen = new Set<string>();
    for (const raw of values) {
        const v = raw?.trim?.() ?? raw;
        if (!v) continue;
        if (seen.has(v)) continue;
        seen.add(v);
        out.push(v);
    }
    return out;
}

export default function Machines() {
    const { routeUsername, routeProjectName, projectId, canEdit } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const recipesQuery = useQuery({
        queryKey: ["recipes", projectId],
        queryFn: () => fetchRecipes(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const recipes = useMemo(
        () => coerceRecipes(recipesQuery.data),
        [recipesQuery.data],
    );

    const sortedRecipes = useMemo(() => {
        return [...recipes].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
        );
    }, [recipes]);

    const recipeNameByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const r of sortedRecipes) map.set(r.puid, r.name);
        return map;
    }, [sortedRecipes]);

    const machinesQuery = useQuery({
        queryKey: ["machines", projectId],
        queryFn: () => fetchMachines(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const machines = useMemo(
        () => coerceMachines(machinesQuery.data),
        [machinesQuery.data],
    );

    const sortedMachines = useMemo(() => {
        return [...machines].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
        );
    }, [machines]);

    const machineToText = useMemo(() => {
        return (m: Machine) => {
            const recipeNames = normalizeStringArray(m.recipePuids)
                .map((p) => recipeNameByPuid.get(p) ?? p)
                .join(" ");
            return `${m.name} ${m.description ?? ""} ${m.baseSpeed} ${recipeNames}`;
        };
    }, [recipeNameByPuid]);

    const {
        searchText,
        setSearchText,
        filteredItems: filteredMachines,
    } = useSearch(sortedMachines, {
        toText: machineToText,
    });

    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createBaseSpeed, setCreateBaseSpeed] = useState<string>("1");
    const [createRecipePuids, setCreateRecipePuids] = useState<string[]>([]);
    const [createError, setCreateError] = useState<string | null>(null);
    const createNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!createOpen) return;
        setCreateError(null);
    }, [createOpen]);

    const createMachineMutation = useMutation({
        mutationFn: async (payload: NewMachinePayload) => {
            if (!projectId) throw new Error("No project selected.");
            return postNewMachine(
                projectId,
                protectedApi,
                payload,
            ) as Promise<Machine>;
        },
        onSuccess: async () => {
            setCreateError(null);
            setCreateName("");
            setCreateDescription("");
            setCreateBaseSpeed("1");
            setCreateRecipePuids([]);
            setCreateOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["machines", projectId],
            });
        },
        onError: (err) => {
            setCreateError(
                err instanceof Error
                    ? err.message
                    : "Failed to create machine.",
            );
        },
    });

    const [editOpen, setEditOpen] = useState(false);
    const [editTarget, setEditTarget] = useState<Machine | null>(null);
    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");
    const [editBaseSpeed, setEditBaseSpeed] = useState<string>("1");
    const [editRecipePuids, setEditRecipePuids] = useState<string[]>([]);
    const [editError, setEditError] = useState<string | null>(null);
    const editNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!editOpen) return;
        setEditError(null);
        setEditName(editTarget?.name ?? "");
        setEditDescription(editTarget?.description ?? "");
        setEditBaseSpeed(
            typeof editTarget?.baseSpeed === "number"
                ? String(editTarget.baseSpeed)
                : "1",
        );
        setEditRecipePuids(
            uniqueTrimmedPuids(normalizeStringArray(editTarget?.recipePuids)),
        );
    }, [editOpen, editTarget]);

    const updateMachineMutation = useMutation({
        mutationFn: async (payload: NewMachinePayload) => {
            if (!projectId) throw new Error("No project selected.");
            if (!editTarget) throw new Error("No machine selected.");
            return updateMachine(
                projectId,
                editTarget.puid,
                protectedApi,
                payload,
            ) as Promise<Machine>;
        },
        onSuccess: async () => {
            setEditError(null);
            setEditOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["machines", projectId],
            });
        },
        onError: (err) => {
            setEditError(
                err instanceof Error
                    ? err.message
                    : "Failed to update machine.",
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

    const deleteMachineMutation = useMutation({
        mutationFn: async (puid: string) => {
            if (!projectId) throw new Error("No project selected.");
            await deleteMachine(projectId, puid, protectedApi);
        },
        onSuccess: async () => {
            deleteConfirm.reset();
            setDeleteError(null);
            await queryClient.invalidateQueries({
                queryKey: ["machines", projectId],
            });
        },
        onError: (err) => {
            deleteConfirm.reset();
            setDeleteError(
                err instanceof Error
                    ? err.message
                    : "Failed to delete machine.",
            );
        },
    });

    const RecipeMultiSelect = ({
        value,
        onChange,
        disabled,
    }: {
        value: string[];
        onChange: (next: string[]) => void;
        disabled?: boolean;
    }) => {
        const effectiveDisabled =
            Boolean(disabled) || sortedRecipes.length === 0;
        const selected = uniqueTrimmedPuids(value);
        const selectedSet = new Set(selected);

        const {
            searchText: menuSearchText,
            setSearchText: setMenuSearchText,
            filteredItems: filteredRecipes,
        } = useSearch(sortedRecipes, {
            toText: (r) => `${r.name} ${r.description ?? ""}`,
        });

        return (
            <DropDown
                label={
                    <div className="min-w-0">
                        <div className="truncate text-sm text-slate-200">
                            {formatSelectedRecipesLabel(
                                selected,
                                recipeNameByPuid,
                                sortedRecipes.length,
                            )}
                        </div>
                    </div>
                }
                align="right"
                disabled={effectiveDisabled}
                className="w-full"
                buttonClassName="rounded-lg px-3 py-2"
                matchTriggerWidth
            >
                {({ close }) => (
                    <div className="p-2">
                        <div className="flex flex-col gap-1">
                            <div className="sticky top-0 z-10 rounded-lg border border-slate-800 bg-slate-950 p-2">
                                <div className="flex items-center gap-2">
                                    <div className="text-slate-400">
                                        <IconSearch size={16} />
                                    </div>
                                    <input
                                        value={menuSearchText}
                                        onChange={(e) =>
                                            setMenuSearchText(e.target.value)
                                        }
                                        placeholder="Search recipes"
                                        className="w-full rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={effectiveDisabled}
                                        aria-label="Search recipes"
                                    />
                                </div>
                            </div>

                            {filteredRecipes.map((r) => {
                                const isSelected = selectedSet.has(r.puid);
                                return (
                                    <button
                                        key={r.puid}
                                        type="button"
                                        className={`group flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors cursor-pointer hover:bg-slate-800/70 ${
                                            isSelected
                                                ? "bg-purple-600/15 text-slate-100"
                                                : "text-slate-200"
                                        }`}
                                        onClick={() => {
                                            const next = new Set(selected);
                                            if (next.has(r.puid))
                                                next.delete(r.puid);
                                            else next.add(r.puid);
                                            onChange(Array.from(next));
                                        }}
                                    >
                                        <span className="min-w-0 truncate">
                                            {r.name}
                                        </span>
                                        <span
                                            className={`shrink-0 ${
                                                isSelected
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
                            {sortedRecipes.length > 0 &&
                                filteredRecipes.length === 0 && (
                                    <div className="px-3 py-2 text-sm text-slate-400">
                                        No recipes match your search.
                                    </div>
                                )}

                            {sortedRecipes.length === 0 && (
                                <div className="px-3 py-2 text-sm text-slate-400">
                                    No recipes yet.
                                </div>
                            )}

                            <div className="mt-1 flex items-center justify-end">
                                <button
                                    type="button"
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    onClick={() => {
                                        setMenuSearchText("");
                                        close();
                                    }}
                                >
                                    Done
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </DropDown>
        );
    };

    const renderRecipeList = (recipePuids: unknown) => {
        const items = uniqueTrimmedPuids(normalizeStringArray(recipePuids));
        if (items.length === 0) {
            return <div className="text-sm text-slate-500">None</div>;
        }
        const sorted = [...items].sort((a, b) => {
            const an = recipeNameByPuid.get(a) ?? a;
            const bn = recipeNameByPuid.get(b) ?? b;
            return an.localeCompare(bn, undefined, { sensitivity: "base" });
        });

        return (
            <div className="flex flex-col gap-1">
                {sorted.map((puid) => (
                    <div
                        key={puid}
                        className="min-w-0 truncate text-sm text-slate-200"
                    >
                        {recipeNameByPuid.get(puid) ?? puid}
                    </div>
                ))}
            </div>
        );
    };

    return (
        <ProjectPageLayout>
            <div className="flex flex-col gap-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            Machines
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
                                ? "Add machine"
                                : "Only the project owner can manage machines"
                        }
                    >
                        <IconPlus size={18} />
                        Add machine
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

                    {machinesQuery.isLoading && projectId && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                            Loading machines…
                        </div>
                    )}

                    {!machinesQuery.isLoading && machinesQuery.error && (
                        <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            Failed to load machines.
                        </div>
                    )}

                    {!machinesQuery.isLoading &&
                        !machinesQuery.error &&
                        projectId &&
                        filteredMachines.length === 0 && (
                            <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-sm text-slate-300">
                                {searchText.trim()
                                    ? "No machines match your search."
                                    : "No machines yet."}
                            </div>
                        )}

                    {filteredMachines.length > 0 && (
                        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                            {filteredMachines.map((machine) => (
                                <div
                                    key={machine.puid}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 p-4"
                                >
                                    <div className="flex items-start justify-between gap-3">
                                        <div className="min-w-0">
                                            <div className="truncate text-base font-semibold text-slate-100">
                                                {machine.name}
                                            </div>
                                            <div className="mt-1 text-sm text-slate-400">
                                                Base speed: {machine.baseSpeed}
                                            </div>
                                            {machine.description ? (
                                                <div className="mt-2 text-sm text-slate-300">
                                                    <ReactMarkdown>
                                                        {machine.description}
                                                    </ReactMarkdown>
                                                </div>
                                            ) : (
                                                <div className="mt-2 text-sm text-slate-500">
                                                    No description
                                                </div>
                                            )}
                                        </div>

                                        <div className="flex gap-2">
                                            <button
                                                type="button"
                                                className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                title="Edit machine"
                                                aria-label="Edit machine"
                                                onClick={() => {
                                                    setEditTarget(machine);
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
                                                        machine.puid,
                                                    )
                                                        ? "rounded-lg border border-red-500/60 bg-red-600/30 p-2 text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                        : "rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                }
                                                title={
                                                    deleteConfirm.isConfirming(
                                                        machine.puid,
                                                    )
                                                        ? "Click again to confirm"
                                                        : "Delete machine"
                                                }
                                                aria-label={
                                                    deleteConfirm.isConfirming(
                                                        machine.puid,
                                                    )
                                                        ? "Confirm delete machine"
                                                        : "Delete machine"
                                                }
                                                onClick={() => {
                                                    if (!canEdit) return;

                                                    setDeleteError(null);

                                                    deleteConfirm.confirmOrRequest(
                                                        machine.puid,
                                                        () => {
                                                            deleteMachineMutation.mutate(
                                                                machine.puid,
                                                            );
                                                        },
                                                    );
                                                }}
                                                disabled={
                                                    !canEdit ||
                                                    deleteMachineMutation.isPending
                                                }
                                            >
                                                {deleteConfirm.isConfirming(
                                                    machine.puid,
                                                ) ? (
                                                    <IconCheck size={20} />
                                                ) : (
                                                    <IconTrash size={20} />
                                                )}
                                            </button>
                                        </div>
                                    </div>

                                    <div className="mt-4 grid grid-cols-1 gap-4">
                                        <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                            <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                                                Recipes
                                            </div>
                                            <div className="mt-2">
                                                {renderRecipeList(
                                                    machine.recipePuids,
                                                )}
                                            </div>
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
                title="Add machine"
                description="Create a new machine in this project."
                initialFocusRef={createNameRef}
                submitLabel="Create"
                submittingLabel="Creating…"
                submitting={createMachineMutation.isPending}
                submitDisabled={!canEdit || !projectId}
                cancelDisabled={createMachineMutation.isPending}
                onCancel={() => setCreateOpen(false)}
                onSubmit={() => {
                    setCreateError(null);
                    const trimmedName = createName.trim();
                    if (!trimmedName) {
                        setCreateError("Machine name is required.");
                        return;
                    }

                    const base = Number(createBaseSpeed);
                    if (!Number.isFinite(base) || base <= 0) {
                        setCreateError("Base speed must be a positive number.");
                        return;
                    }

                    createMachineMutation.mutate({
                        name: trimmedName,
                        description: createDescription.trim()
                            ? createDescription.trim()
                            : null,
                        baseSpeed: base,
                        recipePuids: uniqueTrimmedPuids(createRecipePuids),
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
                            placeholder="Assembler"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createMachineMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Base speed
                        </label>
                        <input
                            value={createBaseSpeed}
                            onChange={(e) => setCreateBaseSpeed(e.target.value)}
                            inputMode="decimal"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createMachineMutation.isPending}
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
                            disabled={createMachineMutation.isPending}
                        />
                    </div>

                    <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                        <div className="text-sm font-medium text-slate-200">
                            Recipes
                        </div>

                        {sortedRecipes.length === 0 ? (
                            <div className="mt-2 text-sm text-slate-500">
                                No recipes available in this project.
                            </div>
                        ) : null}

                        <div className="mt-3 flex flex-col gap-2">
                            <RecipeMultiSelect
                                value={createRecipePuids}
                                onChange={setCreateRecipePuids}
                                disabled={createMachineMutation.isPending}
                            />

                            {uniqueTrimmedPuids(createRecipePuids).length ===
                            0 ? (
                                <div className="text-sm text-slate-500">
                                    No recipes selected
                                </div>
                            ) : (
                                <div className="flex flex-col gap-2">
                                    {uniqueTrimmedPuids(createRecipePuids)
                                        .slice()
                                        .sort((a, b) => {
                                            const an =
                                                recipeNameByPuid.get(a) ?? a;
                                            const bn =
                                                recipeNameByPuid.get(b) ?? b;
                                            return an.localeCompare(
                                                bn,
                                                undefined,
                                                { sensitivity: "base" },
                                            );
                                        })
                                        .map((puid) => (
                                            <div
                                                key={puid}
                                                className="flex items-center justify-between gap-3 rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-2"
                                            >
                                                <div className="min-w-0 truncate text-sm text-slate-200">
                                                    {recipeNameByPuid.get(
                                                        puid,
                                                    ) ?? puid}
                                                </div>
                                                <button
                                                    type="button"
                                                    className="shrink-0 rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                    onClick={() =>
                                                        setCreateRecipePuids(
                                                            (prev) =>
                                                                prev.filter(
                                                                    (p) =>
                                                                        p !==
                                                                        puid,
                                                                ),
                                                        )
                                                    }
                                                    disabled={
                                                        createMachineMutation.isPending
                                                    }
                                                    title="Remove"
                                                    aria-label="Remove recipe"
                                                >
                                                    <IconTrash size={18} />
                                                </button>
                                            </div>
                                        ))}
                                </div>
                            )}
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
                title="Edit machine"
                description="Update machine details."
                initialFocusRef={editNameRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setEditOpen(false)}
                            disabled={updateMachineMutation.isPending}
                        >
                            Cancel
                        </button>
                        <button
                            type="button"
                            className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={() => {
                                setEditError(null);
                                const trimmedName = editName.trim();
                                if (!trimmedName) {
                                    setEditError("Machine name is required.");
                                    return;
                                }
                                if (!editTarget) {
                                    setEditError("No machine selected.");
                                    return;
                                }

                                const base = Number(editBaseSpeed);
                                if (!Number.isFinite(base) || base <= 0) {
                                    setEditError(
                                        "Base speed must be a positive number.",
                                    );
                                    return;
                                }

                                updateMachineMutation.mutate({
                                    name: trimmedName,
                                    description: editDescription.trim()
                                        ? editDescription.trim()
                                        : null,
                                    baseSpeed: base,
                                    recipePuids:
                                        uniqueTrimmedPuids(editRecipePuids),
                                });
                            }}
                            disabled={
                                updateMachineMutation.isPending ||
                                !canEdit ||
                                !editTarget
                            }
                        >
                            {updateMachineMutation.isPending
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
                            disabled={updateMachineMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Base speed
                        </label>
                        <input
                            value={editBaseSpeed}
                            onChange={(e) => setEditBaseSpeed(e.target.value)}
                            inputMode="decimal"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={updateMachineMutation.isPending}
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
                            disabled={updateMachineMutation.isPending}
                        />
                    </div>

                    <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                        <div className="text-sm font-medium text-slate-200">
                            Recipes
                        </div>
                        <div className="mt-3 flex flex-col gap-2">
                            <RecipeMultiSelect
                                value={editRecipePuids}
                                onChange={setEditRecipePuids}
                                disabled={updateMachineMutation.isPending}
                            />
                            {uniqueTrimmedPuids(editRecipePuids).length ===
                            0 ? (
                                <div className="text-sm text-slate-500">
                                    No recipes selected
                                </div>
                            ) : (
                                <div className="flex flex-col gap-2">
                                    {uniqueTrimmedPuids(editRecipePuids)
                                        .slice()
                                        .sort((a, b) => {
                                            const an =
                                                recipeNameByPuid.get(a) ?? a;
                                            const bn =
                                                recipeNameByPuid.get(b) ?? b;
                                            return an.localeCompare(
                                                bn,
                                                undefined,
                                                { sensitivity: "base" },
                                            );
                                        })
                                        .map((puid) => (
                                            <div
                                                key={puid}
                                                className="flex items-center justify-between gap-3 rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-2"
                                            >
                                                <div className="min-w-0 truncate text-sm text-slate-200">
                                                    {recipeNameByPuid.get(
                                                        puid,
                                                    ) ?? puid}
                                                </div>
                                                <button
                                                    type="button"
                                                    className="shrink-0 rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                    onClick={() =>
                                                        setEditRecipePuids(
                                                            (prev) =>
                                                                prev.filter(
                                                                    (p) =>
                                                                        p !==
                                                                        puid,
                                                                ),
                                                        )
                                                    }
                                                    disabled={
                                                        updateMachineMutation.isPending
                                                    }
                                                    title="Remove"
                                                    aria-label="Remove recipe"
                                                >
                                                    <IconTrash size={18} />
                                                </button>
                                            </div>
                                        ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </Popup>
        </ProjectPageLayout>
    );
}
