"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import DropDown from "@/components/DropDown";
import SearchBar from "@/components/SearchBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import Popup from "@/components/Popup";
import ItemCard from "@/components/ItemCard";
import { useAuth } from "@/context/AuthContext";
import { useProject } from "@/context/ProjectContext";
import { useProtectedApi } from "@/lib/api";
import {
    deleteModifier,
    type NewModifierPayload,
    postNewModifier,
    updateModifier,
} from "@/lib/modifiers";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { useSearch } from "@/hooks/Search";
import {
    IconCheck,
    IconEdit,
    IconPlus,
    IconSearch,
    IconTrash,
    IconGauge,
    IconPackage,
    IconSettings,
} from "@tabler/icons-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useRef, useState } from "react";
import { useModifiersQuery, useAttributesQuery } from "@/hooks/useQueries";
import ReactMarkdown from "react-markdown";

interface Attribute {
    puid: string;
    name: string;
    description: string | null;
    unit: string | null;
    createdAt: string;
    updatedAt: string;
}

type ModifierAttributeBonus = {
    puid: string;
    flatBonus: number;
    percentBonus: number;
    multiplicativeBonus: number;
};

interface Modifier {
    puid: string;
    name: string;
    description: string | null;
    flatBonus: number;
    percentBonus: number;
    multiplicativeBonus: number;
    inputPercent: number;
    outputPercent: number;
    attributes: ModifierAttributeBonus[];
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

function coerceAttributes(value: unknown): Attribute[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Attribute[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Attribute[];
    }
    return [];
}

function normalizeAttributeBonuses(value: unknown): ModifierAttributeBonus[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as ModifierAttributeBonus[];
    return [];
}

function parseRequiredNumber(value: string): number | null {
    const trimmed = value.trim();
    if (!trimmed) return null;
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed)) return null;
    return parsed;
}

function validateAttributeBonuses(
    attributes: ModifierAttributeBonus[],
    label: string,
): string | null {
    const trimmed = attributes
        .map((a) => ({
            puid: a.puid?.trim?.() ?? a.puid,
            flatBonus: a.flatBonus,
            percentBonus: a.percentBonus,
            multiplicativeBonus: a.multiplicativeBonus,
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
        if (
            !(typeof attr.flatBonus === "number") ||
            Number.isNaN(attr.flatBonus)
        ) {
            return `${label} has an invalid flat bonus.`;
        }
        if (
            !(typeof attr.percentBonus === "number") ||
            Number.isNaN(attr.percentBonus)
        ) {
            return `${label} has an invalid percent bonus.`;
        }
        if (
            !(typeof attr.multiplicativeBonus === "number") ||
            Number.isNaN(attr.multiplicativeBonus)
        ) {
            return `${label} has an invalid multiplicative bonus.`;
        }
    }

    return null;
}

function pickDefaultAttributePuid(
    attributes: Attribute[],
    used: Set<string>,
): string {
    const firstUnused = attributes.find((a) => !used.has(a.puid));
    return (firstUnused ?? attributes[0])?.puid ?? "";
}

export default function Modifiers() {
    const { loggedIn } = useAuth();
    const { routeUsername, routeProjectName, projectId, canEdit } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const modifiersQuery = useModifiersQuery(projectId);

    const modifiers = useMemo(
        () => coerceModifiers(modifiersQuery.data),
        [modifiersQuery.data],
    );

    const sortedModifiers = useMemo(() => {
        return [...modifiers].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
        );
    }, [modifiers]);

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

    const {
        searchText,
        setSearchText,
        filteredItems: filteredModifiers,
    } = useSearch(sortedModifiers, {
        toText: (m) => {
            const attrNames = normalizeAttributeBonuses(m.attributes)
                .map((a) => attributeNameByPuid.get(a.puid) ?? a.puid)
                .join(" ");
            return `${m.name} ${m.description ?? ""} ${m.flatBonus} ${m.percentBonus} ${m.multiplicativeBonus} ${m.inputPercent} ${m.outputPercent} ${attrNames}`;
        },
    });

    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createFlatBonus, setCreateFlatBonus] = useState("0");
    const [createPercentBonus, setCreatePercentBonus] = useState("0");
    const [createMultiplicativeBonus, setCreateMultiplicativeBonus] =
        useState("0");
    const [createInputPercent, setCreateInputPercent] = useState("0");
    const [createOutputPercent, setCreateOutputPercent] = useState("0");
    const [createAttributes, setCreateAttributes] = useState<
        ModifierAttributeBonus[]
    >([]);
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
            setCreateFlatBonus("0");
            setCreatePercentBonus("0");
            setCreateMultiplicativeBonus("0");
            setCreateInputPercent("0");
            setCreateOutputPercent("0");
            setCreateAttributes([]);
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
    const [editFlatBonus, setEditFlatBonus] = useState("0");
    const [editPercentBonus, setEditPercentBonus] = useState("0");
    const [editMultiplicativeBonus, setEditMultiplicativeBonus] = useState("0");
    const [editInputPercent, setEditInputPercent] = useState("0");
    const [editOutputPercent, setEditOutputPercent] = useState("0");
    const [editAttributes, setEditAttributes] = useState<
        ModifierAttributeBonus[]
    >([]);
    const [editError, setEditError] = useState<string | null>(null);
    const editNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!editOpen) return;
        setEditError(null);
        setEditName(editTarget?.name ?? "");
        setEditDescription(editTarget?.description ?? "");
        setEditFlatBonus(editTarget ? String(editTarget.flatBonus) : "0");
        setEditPercentBonus(editTarget ? String(editTarget.percentBonus) : "0");
        setEditMultiplicativeBonus(
            editTarget ? String(editTarget.multiplicativeBonus) : "0",
        );
        setEditInputPercent(editTarget ? String(editTarget.inputPercent) : "0");
        setEditOutputPercent(
            editTarget ? String(editTarget.outputPercent) : "0",
        );
        setEditAttributes(normalizeAttributeBonuses(editTarget?.attributes));
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

        const {
            searchText: menuSearchText,
            setSearchText: setMenuSearchText,
            filteredItems: filteredAttributes,
        } = useSearch(sortedAttributes, {
            toText: (a) => `${a.name} ${a.description ?? ""} ${a.unit ?? ""}`,
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
            >
                {({ close }) => (
                    <div className="p-2">
                        <div className="flex flex-col gap-1">
                            <div className="sticky top-0 z-10 rounded-lg p-2">
                                <div className="flex items-center gap-2">
                                    <div className="text-slate-400">
                                        <IconSearch size={16} />
                                    </div>
                                    <input
                                        value={menuSearchText}
                                        onChange={(e) =>
                                            setMenuSearchText(e.target.value)
                                        }
                                        placeholder="Search attributes"
                                        className="w-full rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={effectiveDisabled}
                                        aria-label="Search attributes"
                                    />
                                </div>
                            </div>

                            {filteredAttributes.map((a) => {
                                const selected = a.puid === value;
                                const suffix = a.unit?.trim()
                                    ? ` (${a.unit})`
                                    : "";
                                return (
                                    <button
                                        key={a.puid}
                                        type="button"
                                        className={`group flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors cursor-pointer hover:bg-slate-800/70 ${
                                            selected
                                                ? "bg-purple-600/15 text-slate-100"
                                                : "text-slate-200"
                                        }`}
                                        onClick={() => {
                                            onSelect(a.puid);
                                            setMenuSearchText("");
                                            close();
                                        }}
                                    >
                                        <span className="min-w-0 truncate">
                                            {a.name}
                                            {suffix}
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

                            {sortedAttributes.length > 0 &&
                                filteredAttributes.length === 0 && (
                                    <div className="px-3 py-2 text-sm text-slate-400">
                                        No attributes match your search.
                                    </div>
                                )}

                            {sortedAttributes.length === 0 && (
                                <div className="px-3 py-2 text-sm text-slate-400">
                                    No attributes yet.
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

    const renderAttributeBonuses = (value: unknown) => {
        const items = normalizeAttributeBonuses(value);
        if (items.length === 0) {
            return <div className="text-sm text-slate-500">None</div>;
        }

        return (
            <div className="flex flex-col gap-2">
                {items.map((item, idx) => {
                    const name =
                        attributeNameByPuid.get(item.puid) ?? item.puid;
                    return (
                        <div
                            key={`${item.puid}-${idx}`}
                            className="rounded-md border border-slate-800 bg-slate-900/30 p-2"
                        >
                            <div className="text-sm text-slate-200">{name}</div>
                            <div className="mt-1 grid grid-cols-3 gap-2 text-xs text-slate-400">
                                <div>Flat: {item.flatBonus}</div>
                                <div>Add: {item.percentBonus}</div>
                                <div>Mult: {item.multiplicativeBonus}</div>
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
                                    ? "Add modifier"
                                    : loggedIn
                                      ? "Only the project owner can manage modifiers"
                                      : "Sign in to manage modifiers"
                            }
                        >
                            <IconPlus size={18} />
                            Add modifier
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

                    {modifiersQuery.isLoading && projectId && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                            Loading modifiers...
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

                    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 2xl:grid-cols-3">
                        {filteredModifiers.map((modifier) => (
                            <div
                                key={modifier.puid}
                                className="rounded-xl border border-slate-800 bg-slate-900/40 p-4 shrink-0 flex flex-col"
                            >
                                <div className="flex items-start justify-between gap-3 flex-1">
                                    <div className="min-w-0 flex-1">
                                        <div className="flex flex-row justify-between">
                                            <div className="flex flex-col">
                                                <div className="truncate text-base font-semibold text-slate-100">
                                                    {modifier.name}
                                                </div>

                                                {modifier.description ? (
                                                    <div className="mt-1 text-sm text-slate-300">
                                                        <ReactMarkdown>
                                                            {
                                                                modifier.description
                                                            }
                                                        </ReactMarkdown>
                                                    </div>
                                                ) : (
                                                    <div className="mt-1 text-sm text-slate-500">
                                                        No description
                                                    </div>
                                                )}
                                            </div>
                                            {canEdit && (
                                                <div className="flex gap-2 h-min">
                                                    <button
                                                        type="button"
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                        title="Edit modifier"
                                                        aria-label="Edit modifier"
                                                        onClick={() => {
                                                            setEditTarget(
                                                                modifier,
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
                                                            if (!canEdit)
                                                                return;
                                                            setDeleteError(
                                                                null,
                                                            );
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
                                                    Speed Modifiers
                                                </div>
                                                <div className="mt-2 grid grid-cols-1 gap-1 sm:grid-cols-3">
                                                    <div>
                                                        <span className="text-slate-400">
                                                            Flat:
                                                        </span>{" "}
                                                        {modifier.flatBonus}
                                                    </div>
                                                    <div>
                                                        <span className="text-slate-400">
                                                            Add:
                                                        </span>{" "}
                                                        {modifier.percentBonus}
                                                    </div>
                                                    <div>
                                                        <span className="text-slate-400">
                                                            Mult:
                                                        </span>{" "}
                                                        {
                                                            modifier.multiplicativeBonus
                                                        }
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                                <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-emerald-400">
                                                    <IconPackage size={14} />
                                                    Yield Modifiers
                                                </div>
                                                <div className="mt-2 grid grid-cols-1 gap-1 sm:grid-cols-2">
                                                    <div>
                                                        <span className="text-slate-400">
                                                            Input:
                                                        </span>{" "}
                                                        {modifier.inputPercent}
                                                    </div>
                                                    <div>
                                                        <span className="text-slate-400">
                                                            Output:
                                                        </span>{" "}
                                                        {modifier.outputPercent}
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                                <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-blue-400">
                                                    <IconSettings size={14} />
                                                    User Defined Attributes
                                                </div>
                                                <div className="mt-2">
                                                    {renderAttributeBonuses(
                                                        modifier.attributes,
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
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
                submittingLabel="Creating..."
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

                    const flat = parseRequiredNumber(createFlatBonus);
                    if (flat === null) {
                        setCreateError("Flat bonus is required.");
                        return;
                    }

                    const percent = parseRequiredNumber(createPercentBonus);
                    if (percent === null) {
                        setCreateError("Additive bonus is required.");
                        return;
                    }

                    const mult = parseRequiredNumber(createMultiplicativeBonus);
                    if (mult === null) {
                        setCreateError("Multiplicative bonus is required.");
                        return;
                    }

                    const inputPercent =
                        parseRequiredNumber(createInputPercent);
                    if (inputPercent === null) {
                        setCreateError("Input bonus is required.");
                        return;
                    }

                    const outputPercent =
                        parseRequiredNumber(createOutputPercent);
                    if (outputPercent === null) {
                        setCreateError("Output bonus is required.");
                        return;
                    }

                    const attributesErr = validateAttributeBonuses(
                        createAttributes,
                        "Attributes",
                    );
                    if (attributesErr) {
                        setCreateError(attributesErr);
                        return;
                    }

                    createModifierMutation.mutate({
                        name: trimmed,
                        description: createDescription.trim()
                            ? createDescription.trim()
                            : null,
                        flatBonus: flat,
                        percentBonus: percent,
                        multiplicativeBonus: mult,
                        inputPercent,
                        outputPercent,
                        attributes: createAttributes.map((a) => ({
                            puid: a.puid,
                            flatBonus: Number(a.flatBonus),
                            percentBonus: Number(a.percentBonus),
                            multiplicativeBonus: Number(a.multiplicativeBonus),
                        })),
                    });
                }}
            >
                <div className="flex flex-col gap-4 min-w-0">
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
                            placeholder="Productivity module"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createModifierMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Description (Optional)
                        </label>
                        <textarea
                            value={createDescription}
                            onChange={(e) =>
                                setCreateDescription(e.target.value)
                            }
                            rows={3}
                            placeholder="A brief description about this modifier..."
                            className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createModifierMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-6">
                        <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                            <div className="flex items-center gap-2 mb-4 text-xs font-semibold uppercase tracking-wide text-purple-400">
                                <IconGauge size={16} />
                                Speed Modifiers
                            </div>
                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Flat bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={createFlatBonus}
                                        onChange={(e) =>
                                            setCreateFlatBonus(e.target.value)
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            createModifierMutation.isPending
                                        }
                                    />
                                </div>

                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Additive bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={createPercentBonus}
                                        onChange={(e) =>
                                            setCreatePercentBonus(
                                                e.target.value,
                                            )
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            createModifierMutation.isPending
                                        }
                                    />
                                </div>

                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Multiplicative bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={createMultiplicativeBonus}
                                        onChange={(e) =>
                                            setCreateMultiplicativeBonus(
                                                e.target.value,
                                            )
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            createModifierMutation.isPending
                                        }
                                    />
                                </div>
                            </div>
                        </div>

                        <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                            <div className="flex items-center gap-2 mb-4 text-xs font-semibold uppercase tracking-wide text-emerald-400">
                                <IconPackage size={16} />
                                Yield Modifiers
                            </div>
                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Input bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={createInputPercent}
                                        onChange={(e) =>
                                            setCreateInputPercent(
                                                e.target.value,
                                            )
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            createModifierMutation.isPending
                                        }
                                    />
                                </div>

                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Output bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={createOutputPercent}
                                        onChange={(e) =>
                                            setCreateOutputPercent(
                                                e.target.value,
                                            )
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            createModifierMutation.isPending
                                        }
                                    />
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                        <div className="flex items-center justify-between gap-3 mb-4">
                            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-blue-400">
                                <IconSettings size={16} />
                                Attribute Bonuses
                            </div>
                            <button
                                type="button"
                                className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                onClick={() => {
                                    const used = new Set(
                                        createAttributes.map((a) => a.puid),
                                    );
                                    const nextPuid = pickDefaultAttributePuid(
                                        sortedAttributes,
                                        used,
                                    );
                                    if (!nextPuid) return;
                                    setCreateAttributes((prev) => [
                                        ...prev,
                                        {
                                            puid: nextPuid,
                                            flatBonus: 0,
                                            percentBonus: 0,
                                            multiplicativeBonus: 0,
                                        },
                                    ]);
                                }}
                                disabled={
                                    createModifierMutation.isPending ||
                                    sortedAttributes.length === 0
                                }
                                title={
                                    sortedAttributes.length === 0
                                        ? "Create attributes first"
                                        : "Add attribute"
                                }
                            >
                                <IconPlus size={16} />
                                Add
                            </button>
                        </div>

                        {sortedAttributes.length === 0 ? (
                            <div className="mt-2 text-sm text-slate-500">
                                No attributes available in this project.
                            </div>
                        ) : null}

                        <div className="mt-3 flex flex-col gap-2 min-w-0">
                            {createAttributes.length === 0 && (
                                <div className="text-sm text-slate-500">
                                    No attributes
                                </div>
                            )}
                            {createAttributes.map((row, idx) => (
                                <div
                                    key={`create-attr-${idx}`}
                                    className="flex flex-col gap-1"
                                >
                                    <label className="text-sm font-small text-slate-200">
                                        Attribute
                                    </label>
                                    <div className="flex flex-row gap-1">
                                        <div className="flex flex-col min-w-0 flex-1">
                                            <div>
                                                <AttributeDropDown
                                                    value={row.puid}
                                                    disabled={
                                                        createModifierMutation.isPending
                                                    }
                                                    onSelect={(next) => {
                                                        setCreateAttributes(
                                                            (prev) =>
                                                                prev.map(
                                                                    (p, i) =>
                                                                        i ===
                                                                        idx
                                                                            ? {
                                                                                  ...p,
                                                                                  puid: next,
                                                                              }
                                                                            : p,
                                                                ),
                                                        );
                                                    }}
                                                />
                                            </div>
                                            <div className="flex flex-row gap-2 mt-1 w-full overflow-hidden">
                                                <div className="min-w-0 flex-1 flex flex-col gap-1">
                                                    <label className="text-sm font-small text-slate-200">
                                                        Flat bonus
                                                    </label>
                                                    <input
                                                        value={String(
                                                            row.flatBonus,
                                                        )}
                                                        onChange={(e) => {
                                                            const next = Number(
                                                                e.target.value,
                                                            );
                                                            setCreateAttributes(
                                                                (prev) =>
                                                                    prev.map(
                                                                        (
                                                                            p,
                                                                            i,
                                                                        ) =>
                                                                            i ===
                                                                            idx
                                                                                ? {
                                                                                      ...p,
                                                                                      flatBonus:
                                                                                          next,
                                                                                  }
                                                                                : p,
                                                                    ),
                                                            );
                                                        }}
                                                        inputMode="decimal"
                                                        placeholder="Flat"
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                        disabled={
                                                            createModifierMutation.isPending
                                                        }
                                                    />
                                                </div>
                                                <div className="min-w-0 flex-1 flex flex-col gap-1">
                                                    <label className="text-sm font-small text-slate-200">
                                                        Additive bonus
                                                    </label>
                                                    <input
                                                        value={String(
                                                            row.percentBonus,
                                                        )}
                                                        onChange={(e) => {
                                                            const next = Number(
                                                                e.target.value,
                                                            );
                                                            setCreateAttributes(
                                                                (prev) =>
                                                                    prev.map(
                                                                        (
                                                                            p,
                                                                            i,
                                                                        ) =>
                                                                            i ===
                                                                            idx
                                                                                ? {
                                                                                      ...p,
                                                                                      percentBonus:
                                                                                          next,
                                                                                  }
                                                                                : p,
                                                                    ),
                                                            );
                                                        }}
                                                        inputMode="decimal"
                                                        placeholder="Additive"
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                        disabled={
                                                            createModifierMutation.isPending
                                                        }
                                                    />
                                                </div>
                                                <div className="min-w-0 flex-1 flex flex-col gap-1">
                                                    <label className="text-sm font-small text-slate-200">
                                                        Multiplicative bonus
                                                    </label>
                                                    <input
                                                        value={String(
                                                            row.multiplicativeBonus,
                                                        )}
                                                        onChange={(e) => {
                                                            const next = Number(
                                                                e.target.value,
                                                            );
                                                            setCreateAttributes(
                                                                (prev) =>
                                                                    prev.map(
                                                                        (
                                                                            p,
                                                                            i,
                                                                        ) =>
                                                                            i ===
                                                                            idx
                                                                                ? {
                                                                                      ...p,
                                                                                      multiplicativeBonus:
                                                                                          next,
                                                                                  }
                                                                                : p,
                                                                    ),
                                                            );
                                                        }}
                                                        inputMode="decimal"
                                                        placeholder="Multiplicative"
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                        disabled={
                                                            createModifierMutation.isPending
                                                        }
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                        <button
                                            type="button"
                                            className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                            onClick={() =>
                                                setCreateAttributes((prev) =>
                                                    prev.filter(
                                                        (_, i) => i !== idx,
                                                    ),
                                                )
                                            }
                                            disabled={
                                                createModifierMutation.isPending
                                            }
                                            title="Remove"
                                            aria-label="Remove attribute"
                                        >
                                            <IconTrash size={18} />
                                        </button>
                                    </div>
                                </div>
                            ))}
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

                                const flat = parseRequiredNumber(editFlatBonus);
                                if (flat === null) {
                                    setEditError("Flat bonus is required.");
                                    return;
                                }

                                const percent =
                                    parseRequiredNumber(editPercentBonus);
                                if (percent === null) {
                                    setEditError("Additive bonus is required.");
                                    return;
                                }

                                const mult = parseRequiredNumber(
                                    editMultiplicativeBonus,
                                );
                                if (mult === null) {
                                    setEditError(
                                        "Multiplicative bonus is required.",
                                    );
                                    return;
                                }

                                const inputPercent =
                                    parseRequiredNumber(editInputPercent);
                                if (inputPercent === null) {
                                    setEditError("Input bonus is required.");
                                    return;
                                }

                                const outputPercent =
                                    parseRequiredNumber(editOutputPercent);
                                if (outputPercent === null) {
                                    setEditError("Output bonus is required.");
                                    return;
                                }

                                const attributesErr = validateAttributeBonuses(
                                    editAttributes,
                                    "Attributes",
                                );
                                if (attributesErr) {
                                    setEditError(attributesErr);
                                    return;
                                }

                                updateModifierMutation.mutate({
                                    name: trimmed,
                                    description: editDescription.trim()
                                        ? editDescription.trim()
                                        : null,
                                    flatBonus: flat,
                                    percentBonus: percent,
                                    multiplicativeBonus: mult,
                                    inputPercent,
                                    outputPercent,
                                    attributes: editAttributes.map((a) => ({
                                        puid: a.puid,
                                        flatBonus: Number(a.flatBonus),
                                        percentBonus: Number(a.percentBonus),
                                        multiplicativeBonus: Number(
                                            a.multiplicativeBonus,
                                        ),
                                    })),
                                });
                            }}
                            disabled={
                                updateModifierMutation.isPending ||
                                !canEdit ||
                                !editTarget
                            }
                        >
                            {updateModifierMutation.isPending
                                ? "Saving..."
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
                            Description (Optional)
                        </label>
                        <textarea
                            value={editDescription}
                            onChange={(e) => setEditDescription(e.target.value)}
                            placeholder="A brief description about this modifier..."
                            rows={3}
                            className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={updateModifierMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-6">
                        <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                            <div className="flex items-center gap-2 mb-4 text-xs font-semibold uppercase tracking-wide text-purple-400">
                                <IconGauge size={16} />
                                Speed Modifiers
                            </div>
                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Flat bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={editFlatBonus}
                                        onChange={(e) =>
                                            setEditFlatBonus(e.target.value)
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            updateModifierMutation.isPending
                                        }
                                    />
                                </div>

                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Additive bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={editPercentBonus}
                                        onChange={(e) =>
                                            setEditPercentBonus(e.target.value)
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            updateModifierMutation.isPending
                                        }
                                    />
                                </div>

                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Multiplicative bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={editMultiplicativeBonus}
                                        onChange={(e) =>
                                            setEditMultiplicativeBonus(
                                                e.target.value,
                                            )
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            updateModifierMutation.isPending
                                        }
                                    />
                                </div>
                            </div>
                        </div>

                        <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                            <div className="flex items-center gap-2 mb-4 text-xs font-semibold uppercase tracking-wide text-emerald-400">
                                <IconPackage size={16} />
                                Yield Modifiers
                            </div>
                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Input bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={editInputPercent}
                                        onChange={(e) =>
                                            setEditInputPercent(e.target.value)
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            updateModifierMutation.isPending
                                        }
                                    />
                                </div>

                                <div className="flex flex-col gap-2">
                                    <label className="text-sm font-medium text-slate-200">
                                        Output bonus
                                    </label>
                                    <input
                                        type="number"
                                        step="any"
                                        value={editOutputPercent}
                                        onChange={(e) =>
                                            setEditOutputPercent(e.target.value)
                                        }
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            updateModifierMutation.isPending
                                        }
                                    />
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                        <div className="flex items-center justify-between gap-3 mb-4">
                            <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-blue-400">
                                <IconSettings size={16} />
                                Attribute Bonuses
                            </div>
                            <button
                                type="button"
                                className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                onClick={() => {
                                    const used = new Set(
                                        editAttributes.map((a) => a.puid),
                                    );
                                    const nextPuid = pickDefaultAttributePuid(
                                        sortedAttributes,
                                        used,
                                    );
                                    if (!nextPuid) return;
                                    setEditAttributes((prev) => [
                                        ...prev,
                                        {
                                            puid: nextPuid,
                                            flatBonus: 0,
                                            percentBonus: 0,
                                            multiplicativeBonus: 0,
                                        },
                                    ]);
                                }}
                                disabled={
                                    updateModifierMutation.isPending ||
                                    sortedAttributes.length === 0
                                }
                                title={
                                    sortedAttributes.length === 0
                                        ? "Create attributes first"
                                        : "Add attribute"
                                }
                            >
                                <IconPlus size={16} />
                                Add
                            </button>
                        </div>

                        <div className="mt-3 flex flex-col gap-2">
                            {editAttributes.length === 0 && (
                                <div className="text-sm text-slate-500">
                                    No attributes
                                </div>
                            )}
                            {editAttributes.map((row, idx) => (
                                <div
                                    key={`edit-attr-${idx}`}
                                    className="flex flex-col gap-1"
                                >
                                    <label className="text-sm font-small text-slate-200">
                                        Attribute
                                    </label>
                                    <div className="flex flex-row gap-1">
                                        <div className="flex flex-col min-w-0 flex-1">
                                            <div>
                                                <AttributeDropDown
                                                    value={row.puid}
                                                    disabled={
                                                        updateModifierMutation.isPending
                                                    }
                                                    onSelect={(next) => {
                                                        setEditAttributes(
                                                            (prev) =>
                                                                prev.map(
                                                                    (p, i) =>
                                                                        i ===
                                                                        idx
                                                                            ? {
                                                                                  ...p,
                                                                                  puid: next,
                                                                              }
                                                                            : p,
                                                                ),
                                                        );
                                                    }}
                                                />
                                            </div>
                                            <div className="flex flex-row gap-2 mt-1 w-full overflow-hidden">
                                                <div className="min-w-0 flex-1 flex flex-col gap-1">
                                                    <label className="text-sm font-small text-slate-200">
                                                        Flat bonus
                                                    </label>
                                                    <input
                                                        value={String(
                                                            row.flatBonus,
                                                        )}
                                                        onChange={(e) => {
                                                            const next = Number(
                                                                e.target.value,
                                                            );
                                                            setEditAttributes(
                                                                (prev) =>
                                                                    prev.map(
                                                                        (
                                                                            p,
                                                                            i,
                                                                        ) =>
                                                                            i ===
                                                                            idx
                                                                                ? {
                                                                                      ...p,
                                                                                      flatBonus:
                                                                                          next,
                                                                                  }
                                                                                : p,
                                                                    ),
                                                            );
                                                        }}
                                                        inputMode="decimal"
                                                        placeholder="Flat"
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                        disabled={
                                                            updateModifierMutation.isPending
                                                        }
                                                    />
                                                </div>
                                                <div className="min-w-0 flex-1 flex flex-col gap-1">
                                                    <label className="text-sm font-small text-slate-200">
                                                        Additive bonus
                                                    </label>
                                                    <input
                                                        value={String(
                                                            row.percentBonus,
                                                        )}
                                                        onChange={(e) => {
                                                            const next = Number(
                                                                e.target.value,
                                                            );
                                                            setEditAttributes(
                                                                (prev) =>
                                                                    prev.map(
                                                                        (
                                                                            p,
                                                                            i,
                                                                        ) =>
                                                                            i ===
                                                                            idx
                                                                                ? {
                                                                                      ...p,
                                                                                      percentBonus:
                                                                                          next,
                                                                                  }
                                                                                : p,
                                                                    ),
                                                            );
                                                        }}
                                                        inputMode="decimal"
                                                        placeholder="Additive"
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                        disabled={
                                                            updateModifierMutation.isPending
                                                        }
                                                    />
                                                </div>
                                                <div className="min-w-0 flex-1 flex flex-col gap-1">
                                                    <label className="text-sm font-small text-slate-200">
                                                        Multiplicative bonus
                                                    </label>
                                                    <input
                                                        value={String(
                                                            row.multiplicativeBonus,
                                                        )}
                                                        onChange={(e) => {
                                                            const next = Number(
                                                                e.target.value,
                                                            );
                                                            setEditAttributes(
                                                                (prev) =>
                                                                    prev.map(
                                                                        (
                                                                            p,
                                                                            i,
                                                                        ) =>
                                                                            i ===
                                                                            idx
                                                                                ? {
                                                                                      ...p,
                                                                                      multiplicativeBonus:
                                                                                          next,
                                                                                  }
                                                                                : p,
                                                                    ),
                                                            );
                                                        }}
                                                        inputMode="decimal"
                                                        placeholder="Multiplicative"
                                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                        disabled={
                                                            updateModifierMutation.isPending
                                                        }
                                                    />
                                                </div>
                                            </div>
                                        </div>
                                        <button
                                            type="button"
                                            className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                            onClick={() =>
                                                setEditAttributes((prev) =>
                                                    prev.filter(
                                                        (_, i) => i !== idx,
                                                    ),
                                                )
                                            }
                                            disabled={
                                                updateModifierMutation.isPending
                                            }
                                            title="Remove"
                                            aria-label="Remove attribute"
                                        >
                                            <IconTrash size={18} />
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </Popup>
        </ProjectPageLayout>
    );
}
