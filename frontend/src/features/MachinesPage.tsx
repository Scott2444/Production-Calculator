"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import DropDown from "@/components/DropDown";
import MachineEditorDialog from "@/components/MachineEditorDialog";
import SearchBar from "@/components/SearchBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { useProject } from "@/context/ProjectContext";
import { useProtectedApi } from "@/lib/api";
import {
    deleteMachine,
    postNewMachine,
    type NewMachinePayload,
    updateMachine,
} from "@/lib/machines";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { useSearch } from "@/hooks/Search";
import {
    IconCheck,
    IconEdit,
    IconPlus,
    IconTrash,
    IconGauge,
    IconClipboardList,
    IconSettings,
} from "@tabler/icons-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import {
    useAttributesQuery,
    useMachinesQuery,
    useRecipesQuery,
} from "@/hooks/useQueries";
import { type RecipeSummary } from "@/types/recipes";
import { type Attribute } from "@/types/attributes";
import { type Machine, type MachineAttributeRate } from "@/types/machines";

function coerceMachines(value: unknown): Machine[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Machine[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Machine[];
    }
    return [];
}

function coerceRecipes(value: unknown): RecipeSummary[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as RecipeSummary[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as RecipeSummary[];
    }
    return [];
}

function coerceAttributes(value: unknown): Attribute[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Attribute[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Attribute[];
    }
    return [];
}

function normalizeStringArray(value: unknown): string[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as string[];
    return [];
}

function normalizeAttributeRates(value: unknown): MachineAttributeRate[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as MachineAttributeRate[];
    return [];
}

function validateAttributeRates(
    attributes: MachineAttributeRate[],
    label: string,
): string | null {
    const trimmed = attributes
        .map((a) => ({
            puid: a.puid?.trim?.() ?? a.puid,
            rate: a.rate,
        }))
        .filter((a) => Boolean(a.puid));

    const puids = trimmed.map((a) => a.puid);
    const duplicates = puids
        .filter((p, idx) => puids.indexOf(p) !== idx)
        .filter((p, idx, arr) => arr.indexOf(p) === idx);
    if (duplicates.length > 0) {
        return `${label} has duplicate attributes selected.`;
    }

    for (const attr of trimmed) {
        if (!attr.puid) return `${label} has a missing attribute.`;
        if (!(typeof attr.rate === "number") || Number.isNaN(attr.rate)) {
            return `${label} has an invalid rate.`;
        }
    }

    return null;
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

function pickDefaultAttributePuid(
    attributes: Attribute[],
    used: Set<string>,
): string {
    const firstUnused = attributes.find((a) => !used.has(a.puid));
    return (firstUnused ?? attributes[0])?.puid ?? "";
}

function RecipeMultiSelect({
    value,
    onChange,
    disabled,
    sortedRecipes,
    recipeNameByPuid,
}: {
    value: string[];
    onChange: (next: string[]) => void;
    disabled?: boolean;
    sortedRecipes: RecipeSummary[];
    recipeNameByPuid: Map<string, string>;
}) {
    const effectiveDisabled = Boolean(disabled) || sortedRecipes.length === 0;
    const selected = uniqueTrimmedPuids(value);

    const recipeOptions = sortedRecipes.map((r) => ({
        value: r.puid,
        label: r.name,
        searchText: `${r.name} ${r.description ?? ""}`,
    }));

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
            mode="multi"
            options={recipeOptions}
            values={selected}
            onChangeValues={onChange}
            searchPlaceholder="Search recipes"
            searchAriaLabel="Search recipes"
            emptyFilteredText="No recipes match your search."
            emptyOptionsText="No recipes yet."
        />
    );
}

export default function Machines() {
    const { routeUsername, routeProjectName, projectId, canEdit } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const recipesQuery = useRecipesQuery(projectId);

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

    const attributeNameByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const a of sortedAttributes) map.set(a.puid, a.name);
        return map;
    }, [sortedAttributes]);

    const attributeUnitByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const a of sortedAttributes) {
            map.set(a.puid, a.unit?.trim() || "");
        }
        return map;
    }, [sortedAttributes]);

    const machinesQuery = useMachinesQuery(projectId);

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
            const attributeNames = normalizeAttributeRates(m.attributes)
                .map((a) => attributeNameByPuid.get(a.puid) ?? a.puid)
                .join(" ");
            return `${m.name} ${m.description ?? ""} ${m.baseSpeed} ${recipeNames} ${attributeNames}`;
        };
    }, [recipeNameByPuid, attributeNameByPuid]);

    const {
        searchText,
        setSearchText,
        filteredItems: filteredMachines,
    } = useSearch(sortedMachines, {
        toText: machineToText,
    });

    const isMachinesLoading =
        machinesQuery.isLoading ||
        recipesQuery.isLoading ||
        attributesQuery.isLoading;
    const isMachinesFetching =
        machinesQuery.isFetching ||
        recipesQuery.isFetching ||
        attributesQuery.isFetching;

    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createBaseSpeed, setCreateBaseSpeed] = useState<string>("1");
    const [createRecipePuids, setCreateRecipePuids] = useState<string[]>([]);
    const [createAttributes, setCreateAttributes] = useState<
        MachineAttributeRate[]
    >([]);
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
            setCreateAttributes([]);
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
    const [editAttributes, setEditAttributes] = useState<
        MachineAttributeRate[]
    >([]);
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
        setEditAttributes(normalizeAttributeRates(editTarget?.attributes));
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

    const AttributeDropDown = ({
        value,
        onSelect,
        disabled,
    }: {
        value: string;
        onSelect: (next: string) => void;
        disabled?: boolean;
    }) => {
        const selectedName = value ? attributeNameByPuid.get(value) : undefined;
        const effectiveDisabled =
            Boolean(disabled) || sortedAttributes.length === 0;

        const attributeOptions = sortedAttributes.map((a) => {
            const suffix = a.unit?.trim() ? ` (${a.unit})` : "";
            return {
                value: a.puid,
                label: `${a.name}${suffix}`,
                searchText: `${a.name} ${a.description ?? ""} ${a.unit ?? ""}`,
            };
        });

        return (
            <DropDown
                label={
                    <div className="min-w-0">
                        <div className="truncate text-sm text-slate-200">
                            {selectedName ??
                                (effectiveDisabled
                                    ? "No attributes"
                                    : "Select attribute")}
                        </div>
                    </div>
                }
                align="right"
                disabled={effectiveDisabled}
                className="w-full"
                buttonClassName="rounded-lg px-3 py-2"
                matchTriggerWidth
                mode="single"
                options={attributeOptions}
                value={value}
                onSelect={onSelect}
                searchPlaceholder="Search attributes"
                searchAriaLabel="Search attributes"
                emptyFilteredText="No attributes match your search."
                emptyOptionsText="No attributes yet."
            />
        );
    };

    const BoundRecipeMultiSelect = ({
        value,
        onChange,
        disabled,
    }: {
        value: string[];
        onChange: (next: string[]) => void;
        disabled?: boolean;
    }) => (
        <RecipeMultiSelect
            value={value}
            onChange={onChange}
            disabled={disabled}
            sortedRecipes={sortedRecipes}
            recipeNameByPuid={recipeNameByPuid}
        />
    );

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

    const renderAttributeRates = (value: unknown) => {
        const items = normalizeAttributeRates(value);
        if (items.length === 0) {
            return <div className="text-sm text-slate-500">None</div>;
        }

        return (
            <div className="flex flex-col gap-1">
                {items.map((item, idx) => {
                    const name =
                        attributeNameByPuid.get(item.puid) ?? item.puid;
                    const unit = attributeUnitByPuid.get(item.puid) || "";
                    return (
                        <div
                            key={`${item.puid}-${idx}`}
                            className="flex items-center justify-between gap-3"
                        >
                            <div className="min-w-0 truncate text-sm text-slate-200">
                                {name}
                            </div>
                            <div className="shrink-0 text-sm text-slate-400">
                                {item.rate}
                                {unit ? ` ${unit}` : ""}
                            </div>
                        </div>
                    );
                })}
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
                                    ? "Add machine"
                                    : "Only the project owner can manage machines"
                            }
                        >
                            <IconPlus size={18} />
                            Add machine
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

                    {machinesQuery.isLoading && projectId && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                            Loading machines...
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
                        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 2xl:grid-cols-3">
                            {filteredMachines.map((machine) => (
                                <div
                                    key={machine.puid}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 p-4"
                                >
                                    <div className="flex items-start justify-between gap-3">
                                        <div className="min-w-0 flex-1">
                                            <div className="flex flex-row justify-between">
                                                <div className="flex flex-col">
                                                    <div className="truncate text-base font-semibold text-slate-100">
                                                        {machine.name}
                                                    </div>

                                                    {
                                                        machine.description ? (
                                                            <div className="mt-1 text-sm text-slate-300">
                                                                <ReactMarkdown>
                                                                    {
                                                                        machine.description
                                                                    }
                                                                </ReactMarkdown>
                                                            </div>
                                                        ) : (
                                                            <div className="mt-1 text-sm text-slate-500">
                                                                No description
                                                            </div>
                                                        ) /* End description */
                                                    }
                                                </div>
                                                {canEdit && (
                                                    <div className="flex gap-2 h-min">
                                                        <button
                                                            type="button"
                                                            className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                            title="Edit machine"
                                                            aria-label="Edit machine"
                                                            onClick={() => {
                                                                setEditTarget(
                                                                    machine,
                                                                );
                                                                setEditError(
                                                                    null,
                                                                );
                                                                setEditOpen(
                                                                    true,
                                                                );
                                                            }}
                                                            disabled={!canEdit}
                                                        >
                                                            <IconEdit
                                                                size={20}
                                                            />
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
                                                                if (!canEdit)
                                                                    return;

                                                                setDeleteError(
                                                                    null,
                                                                );

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
                                                                <IconCheck
                                                                    size={20}
                                                                />
                                                            ) : (
                                                                <IconTrash
                                                                    size={20}
                                                                />
                                                            )}
                                                        </button>
                                                    </div>
                                                )}
                                            </div>

                                            <div className="mt-3 grid grid-cols-1 gap-4 text-sm text-slate-300">
                                                <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                                    <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-purple-400">
                                                        <IconGauge size={14} />
                                                        Base Speed
                                                    </div>
                                                    <div className="mt-2 text-sm text-slate-200">
                                                        {machine.baseSpeed}
                                                    </div>
                                                </div>

                                                <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                                    <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-emerald-400">
                                                        <IconClipboardList
                                                            size={14}
                                                        />
                                                        Compatible Recipes
                                                    </div>
                                                    <div className="mt-2">
                                                        {renderRecipeList(
                                                            machine.recipePuids,
                                                        )}
                                                    </div>
                                                </div>

                                                <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                                    <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-blue-400">
                                                        <IconSettings
                                                            size={14}
                                                        />
                                                        User Defined Attributes
                                                    </div>
                                                    <div className="mt-2">
                                                        {renderAttributeRates(
                                                            machine.attributes,
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </ProjectStatusGate>

                <div className="mt-auto border-t border-slate-800 pt-4">
                    <div className="flex flex-wrap items-center gap-3 text-sm text-slate-400">
                        <span>Expecting something else?</span>
                        {!isMachinesLoading && (
                            <button
                                type="button"
                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-60"
                                onClick={() => {
                                    void Promise.all([
                                        machinesQuery.refetch(),
                                        recipesQuery.refetch(),
                                        attributesQuery.refetch(),
                                    ]);
                                }}
                                disabled={isMachinesFetching}
                            >
                                {isMachinesFetching
                                    ? "Refreshing..."
                                    : "Refresh"}
                            </button>
                        )}
                    </div>
                </div>
            </div>

            <MachineEditorDialog
                mode="create"
                open={createOpen}
                onOpenChange={(next) => {
                    setCreateOpen(next);
                    if (next) setCreateError(null);
                }}
                name={createName}
                description={createDescription}
                baseSpeed={createBaseSpeed}
                recipePuids={createRecipePuids}
                attributes={createAttributes}
                onNameChange={setCreateName}
                onDescriptionChange={setCreateDescription}
                onBaseSpeedChange={setCreateBaseSpeed}
                onRecipePuidsChange={setCreateRecipePuids}
                onAddAttribute={() => {
                    const used = new Set(createAttributes.map((a) => a.puid));
                    const nextPuid = pickDefaultAttributePuid(
                        sortedAttributes,
                        used,
                    );
                    if (!nextPuid) return;
                    setCreateAttributes((prev) => [
                        ...prev,
                        { puid: nextPuid, rate: 0 },
                    ]);
                }}
                onAttributePuidChange={(index, puid) => {
                    setCreateAttributes((prev) =>
                        prev.map((p, i) =>
                            i === index
                                ? {
                                      ...p,
                                      puid,
                                  }
                                : p,
                        ),
                    );
                }}
                onAttributeRateChange={(index, rate) => {
                    setCreateAttributes((prev) =>
                        prev.map((p, i) =>
                            i === index
                                ? {
                                      ...p,
                                      rate,
                                  }
                                : p,
                        ),
                    );
                }}
                onRemoveAttribute={(index) => {
                    setCreateAttributes((prev) =>
                        prev.filter((_, i) => i !== index),
                    );
                }}
                onRemoveRecipe={(puid) => {
                    setCreateRecipePuids((prev) =>
                        prev.filter((p) => p !== puid),
                    );
                }}
                getRecipeLabel={(puid) => recipeNameByPuid.get(puid) ?? puid}
                sortedRecipesCount={sortedRecipes.length}
                sortedAttributesCount={sortedAttributes.length}
                error={createError}
                onDismissError={() => setCreateError(null)}
                initialFocusRef={createNameRef}
                submitting={createMachineMutation.isPending}
                submitDisabled={!canEdit || !projectId}
                onCancel={() => setCreateOpen(false)}
                AttributeDropDown={AttributeDropDown}
                RecipeMultiSelect={BoundRecipeMultiSelect}
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

                    const attributesErr = validateAttributeRates(
                        createAttributes,
                        "Attributes",
                    );
                    if (attributesErr) {
                        setCreateError(attributesErr);
                        return;
                    }

                    createMachineMutation.mutate({
                        name: trimmedName,
                        description: createDescription.trim()
                            ? createDescription.trim()
                            : null,
                        baseSpeed: base,
                        recipePuids: uniqueTrimmedPuids(createRecipePuids),
                        attributes: createAttributes.map((a) => ({
                            puid: a.puid,
                            rate: Number(a.rate),
                        })),
                    });
                }}
            />

            <MachineEditorDialog
                mode="edit"
                open={editOpen}
                onOpenChange={(next) => {
                    setEditOpen(next);
                    if (next) setEditError(null);
                }}
                name={editName}
                description={editDescription}
                baseSpeed={editBaseSpeed}
                recipePuids={editRecipePuids}
                attributes={editAttributes}
                onNameChange={setEditName}
                onDescriptionChange={setEditDescription}
                onBaseSpeedChange={setEditBaseSpeed}
                onRecipePuidsChange={setEditRecipePuids}
                onAddAttribute={() => {
                    const used = new Set(editAttributes.map((a) => a.puid));
                    const nextPuid = pickDefaultAttributePuid(
                        sortedAttributes,
                        used,
                    );
                    if (!nextPuid) return;
                    setEditAttributes((prev) => [
                        ...prev,
                        { puid: nextPuid, rate: 0 },
                    ]);
                }}
                onAttributePuidChange={(index, puid) => {
                    setEditAttributes((prev) =>
                        prev.map((p, i) =>
                            i === index
                                ? {
                                      ...p,
                                      puid,
                                  }
                                : p,
                        ),
                    );
                }}
                onAttributeRateChange={(index, rate) => {
                    setEditAttributes((prev) =>
                        prev.map((p, i) =>
                            i === index
                                ? {
                                      ...p,
                                      rate,
                                  }
                                : p,
                        ),
                    );
                }}
                onRemoveAttribute={(index) => {
                    setEditAttributes((prev) =>
                        prev.filter((_, i) => i !== index),
                    );
                }}
                onRemoveRecipe={(puid) => {
                    setEditRecipePuids((prev) =>
                        prev.filter((p) => p !== puid),
                    );
                }}
                getRecipeLabel={(puid) => recipeNameByPuid.get(puid) ?? puid}
                sortedRecipesCount={sortedRecipes.length}
                sortedAttributesCount={sortedAttributes.length}
                error={editError}
                onDismissError={() => setEditError(null)}
                initialFocusRef={editNameRef}
                submitting={updateMachineMutation.isPending}
                submitDisabled={
                    updateMachineMutation.isPending || !canEdit || !editTarget
                }
                onCancel={() => setEditOpen(false)}
                AttributeDropDown={AttributeDropDown}
                RecipeMultiSelect={BoundRecipeMultiSelect}
                onSubmit={() => {
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
                        setEditError("Base speed must be a positive number.");
                        return;
                    }

                    const attributesErr = validateAttributeRates(
                        editAttributes,
                        "Attributes",
                    );
                    if (attributesErr) {
                        setEditError(attributesErr);
                        return;
                    }

                    updateMachineMutation.mutate({
                        name: trimmedName,
                        description: editDescription.trim()
                            ? editDescription.trim()
                            : null,
                        baseSpeed: base,
                        recipePuids: uniqueTrimmedPuids(editRecipePuids),
                        attributes: editAttributes.map((a) => ({
                            puid: a.puid,
                            rate: Number(a.rate),
                        })),
                    });
                }}
            />
        </ProjectPageLayout>
    );
}
