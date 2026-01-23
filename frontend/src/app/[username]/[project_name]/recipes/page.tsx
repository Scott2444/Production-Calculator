"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import DropDown from "@/components/DropDown";
import Popup from "@/components/Popup";
import ItemCard from "@/components/ItemCard";
import SearchBar from "@/components/SearchBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { useAuth } from "@/context/AuthContext";
import { useProject } from "@/context/ProjectContext";
import { useProtectedApi } from "@/lib/api";
import { fetchProducts } from "@/lib/products";
import {
    deleteRecipe,
    fetchRecipes,
    type NewRecipePayload,
    postNewRecipe,
    updateRecipe,
} from "@/lib/recipes";
import {
    IconCheck,
    IconEdit,
    IconPlus,
    IconSearch,
    IconTrash,
} from "@tabler/icons-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearch } from "@/hooks/Search";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { useEffect, useMemo, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";

interface Product {
    puid: string;
    name: string;
    description: string | null;
    createdAt: string;
    updatedAt: string;
}

type RecipeExchange = { puid: string; quantity: number };

interface Recipe {
    puid: string;
    name: string;
    description: string | null;
    baseCraftingTime: number;
    inputs: RecipeExchange[];
    outputs: RecipeExchange[];
    createdAt: string;
    updatedAt: string;
}

function safeDecodeURIComponent(value: string): string {
    try {
        return decodeURIComponent(value);
    } catch {
        return value;
    }
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

function coerceProducts(value: unknown): Product[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Product[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Product[];
    }
    return [];
}

function normalizeExchanges(value: unknown): RecipeExchange[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as RecipeExchange[];
    return [];
}

function validateExchanges(
    exchanges: RecipeExchange[],
    label: string,
): string | null {
    const trimmed = exchanges
        .map((e) => ({
            puid: e.puid?.trim?.() ?? e.puid,
            quantity: e.quantity,
        }))
        .filter((e) => Boolean(e.puid));

    const puids = trimmed.map((e) => e.puid);
    const duplicates = puids
        .filter((p, idx) => puids.indexOf(p) !== idx)
        .filter((p, idx, arr) => arr.indexOf(p) === idx);
    if (duplicates.length > 0) {
        return `${label} has duplicate products selected.`;
    }

    for (const ex of trimmed) {
        if (!ex.puid) return `${label} has a missing product.`;
        if (!(typeof ex.quantity === "number") || Number.isNaN(ex.quantity)) {
            return `${label} has an invalid quantity.`;
        }
        if (ex.quantity <= 0) return `${label} quantities must be positive.`;
    }
    return null;
}

function pickDefaultProductPuid(
    products: Product[],
    used: Set<string>,
): string {
    const firstUnused = products.find((p) => !used.has(p.puid));
    return (firstUnused ?? products[0])?.puid ?? "";
}

export default function Recipes() {
    const { loggedIn } = useAuth();
    const { routeUsername, routeProjectName, projectId, canEdit } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const productsQuery = useQuery({
        queryKey: ["products", projectId],
        queryFn: () => fetchProducts(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const products = useMemo(
        () => coerceProducts(productsQuery.data),
        [productsQuery.data],
    );

    const sortedProducts = useMemo(() => {
        return [...products].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
        );
    }, [products]);

    const productNameByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const p of sortedProducts) map.set(p.puid, p.name);
        return map;
    }, [sortedProducts]);

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

    const recipeToText = useMemo(() => {
        return (r: Recipe) => {
            const inputNames = normalizeExchanges(r.inputs)
                .map((i) => productNameByPuid.get(i.puid) ?? i.puid)
                .join(" ");
            const outputNames = normalizeExchanges(r.outputs)
                .map((o) => productNameByPuid.get(o.puid) ?? o.puid)
                .join(" ");
            return `${r.name} ${r.description ?? ""} ${inputNames} ${outputNames}`;
        };
    }, [productNameByPuid]);

    const {
        searchText,
        setSearchText,
        filteredItems: filteredRecipes,
    } = useSearch(sortedRecipes, {
        toText: recipeToText,
    });

    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createBaseTime, setCreateBaseTime] = useState<string>("1");
    const [createInputs, setCreateInputs] = useState<RecipeExchange[]>([]);
    const [createOutputs, setCreateOutputs] = useState<RecipeExchange[]>([]);
    const [createError, setCreateError] = useState<string | null>(null);
    const createNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!createOpen) return;
        setCreateError(null);
        if (createInputs.length === 0 && sortedProducts.length > 0) {
            setCreateInputs([{ puid: sortedProducts[0].puid, quantity: 1 }]);
        }
        if (createOutputs.length === 0 && sortedProducts.length > 0) {
            setCreateOutputs([{ puid: sortedProducts[0].puid, quantity: 1 }]);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [createOpen, sortedProducts.length]);

    const createRecipeMutation = useMutation({
        mutationFn: async (payload: NewRecipePayload) => {
            if (!projectId) throw new Error("No project selected.");
            return postNewRecipe(
                projectId,
                protectedApi,
                payload,
            ) as Promise<Recipe>;
        },
        onSuccess: async () => {
            setCreateError(null);
            setCreateName("");
            setCreateDescription("");
            setCreateBaseTime("1");
            setCreateInputs([]);
            setCreateOutputs([]);
            setCreateOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["recipes", projectId],
            });
        },
        onError: (err) => {
            setCreateError(
                err instanceof Error ? err.message : "Failed to create recipe.",
            );
        },
    });

    const [editOpen, setEditOpen] = useState(false);
    const [editTarget, setEditTarget] = useState<Recipe | null>(null);
    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");
    const [editBaseTime, setEditBaseTime] = useState<string>("1");
    const [editInputs, setEditInputs] = useState<RecipeExchange[]>([]);
    const [editOutputs, setEditOutputs] = useState<RecipeExchange[]>([]);
    const [editError, setEditError] = useState<string | null>(null);
    const editNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!editOpen) return;
        setEditError(null);
        setEditName(editTarget?.name ?? "");
        setEditDescription(editTarget?.description ?? "");
        setEditBaseTime(
            typeof editTarget?.baseCraftingTime === "number"
                ? String(editTarget.baseCraftingTime)
                : "1",
        );
        setEditInputs(normalizeExchanges(editTarget?.inputs));
        setEditOutputs(normalizeExchanges(editTarget?.outputs));
    }, [editOpen, editTarget]);

    const updateRecipeMutation = useMutation({
        mutationFn: async (payload: NewRecipePayload) => {
            if (!projectId) throw new Error("No project selected.");
            if (!editTarget) throw new Error("No recipe selected.");
            return updateRecipe(
                projectId,
                editTarget.puid,
                protectedApi,
                payload,
            ) as Promise<Recipe>;
        },
        onSuccess: async () => {
            setEditError(null);
            setEditOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["recipes", projectId],
            });
        },
        onError: (err) => {
            setEditError(
                err instanceof Error ? err.message : "Failed to update recipe.",
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

    const deleteRecipeMutation = useMutation({
        mutationFn: async (puid: string) => {
            if (!projectId) throw new Error("No project selected.");
            await deleteRecipe(projectId, puid, protectedApi);
        },
        onSuccess: async () => {
            deleteConfirm.reset();
            setDeleteError(null);
            await queryClient.invalidateQueries({
                queryKey: ["recipes", projectId],
            });
        },
        onError: (err) => {
            deleteConfirm.reset();
            setDeleteError(
                err instanceof Error ? err.message : "Failed to delete recipe.",
            );
        },
    });

    const ProductDropDown = ({
        value,
        onSelect,
        disabled,
    }: {
        value: string;
        onSelect: (next: string) => void;
        disabled?: boolean;
    }) => {
        const selectedName = value ? productNameByPuid.get(value) : undefined;
        const effectiveDisabled =
            Boolean(disabled) || sortedProducts.length === 0;

        return (
            <DropDown
                label={
                    <div className="min-w-0">
                        <div className="truncate text-sm text-slate-200">
                            {selectedName ??
                                (effectiveDisabled
                                    ? "No products"
                                    : "Select product")}
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
                            {sortedProducts.map((p) => {
                                const selected = p.puid === value;
                                return (
                                    <button
                                        key={p.puid}
                                        type="button"
                                        className={`group flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors cursor-pointer hover:bg-slate-800/70 ${
                                            selected
                                                ? "bg-purple-600/15 text-slate-100"
                                                : "text-slate-200"
                                        }`}
                                        onClick={() => {
                                            onSelect(p.puid);
                                            close();
                                        }}
                                    >
                                        <span className="min-w-0 truncate">
                                            {p.name}
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

                            {sortedProducts.length === 0 && (
                                <div className="px-3 py-2 text-sm text-slate-400">
                                    No products yet.
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </DropDown>
        );
    };

    const renderExchanges = (exchanges: RecipeExchange[]) => {
        const items = normalizeExchanges(exchanges);
        if (items.length === 0) {
            return <div className="text-sm text-slate-500">None</div>;
        }
        return (
            <div className="flex flex-col gap-1">
                {items.map((ex, idx) => {
                    const name = productNameByPuid.get(ex.puid) ?? ex.puid;
                    return (
                        <div
                            key={`${ex.puid}-${idx}`}
                            className="flex items-center justify-between gap-3"
                        >
                            <div className="min-w-0 truncate text-sm text-slate-200">
                                {name}
                            </div>
                            <div className="shrink-0 text-sm text-slate-400">
                                × {ex.quantity}
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
                            Recipes
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
                                ? "Add recipe"
                                : "Only the project owner can manage recipes"
                        }
                    >
                        <IconPlus size={18} />
                        Add recipe
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

                    {recipesQuery.isLoading && projectId && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                            Loading recipes…
                        </div>
                    )}

                    {!recipesQuery.isLoading && recipesQuery.error && (
                        <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            Failed to load recipes.
                        </div>
                    )}

                    {!recipesQuery.isLoading &&
                        !recipesQuery.error &&
                        projectId &&
                        filteredRecipes.length === 0 && (
                            <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-sm text-slate-300">
                                {searchText.trim()
                                    ? "No recipes match your search."
                                    : "No recipes yet."}
                            </div>
                        )}

                    {filteredRecipes.length > 0 && (
                        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                            {filteredRecipes.map((recipe) => (
                                <div
                                    key={recipe.puid}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 p-4"
                                >
                                    <div className="flex items-start justify-between gap-3">
                                        <div className="min-w-0">
                                            <div className="truncate text-base font-semibold text-slate-100">
                                                {recipe.name}
                                            </div>
                                            <div className="mt-1 text-sm text-slate-400">
                                                Base crafting time:{" "}
                                                {recipe.baseCraftingTime}s
                                            </div>
                                            {recipe.description ? (
                                                <div className="mt-2 text-sm text-slate-300">
                                                    <ReactMarkdown>
                                                        {recipe.description}
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
                                                title="Edit recipe"
                                                aria-label="Edit recipe"
                                                onClick={() => {
                                                    setEditTarget(recipe);
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
                                                        recipe.puid,
                                                    )
                                                        ? "rounded-lg border border-red-500/60 bg-red-600/30 p-2 text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                        : "rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                }
                                                title={
                                                    deleteConfirm.isConfirming(
                                                        recipe.puid,
                                                    )
                                                        ? "Click again to confirm"
                                                        : "Delete recipe"
                                                }
                                                aria-label={
                                                    deleteConfirm.isConfirming(
                                                        recipe.puid,
                                                    )
                                                        ? "Confirm delete recipe"
                                                        : "Delete recipe"
                                                }
                                                onClick={() => {
                                                    if (!canEdit) return;

                                                    setDeleteError(null);

                                                    deleteConfirm.confirmOrRequest(
                                                        recipe.puid,
                                                        () => {
                                                            deleteRecipeMutation.mutate(
                                                                recipe.puid,
                                                            );
                                                        },
                                                    );
                                                }}
                                                disabled={
                                                    !canEdit ||
                                                    deleteRecipeMutation.isPending
                                                }
                                            >
                                                {deleteConfirm.isConfirming(
                                                    recipe.puid,
                                                ) ? (
                                                    <IconCheck size={20} />
                                                ) : (
                                                    <IconTrash size={20} />
                                                )}
                                            </button>
                                        </div>
                                    </div>

                                    <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
                                        <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                            <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                                                Inputs
                                            </div>
                                            <div className="mt-2">
                                                {renderExchanges(recipe.inputs)}
                                            </div>
                                        </div>
                                        <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                                            <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                                                Outputs
                                            </div>
                                            <div className="mt-2">
                                                {renderExchanges(
                                                    recipe.outputs,
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
                title="Add recipe"
                description="Create a new recipe in this project."
                initialFocusRef={createNameRef}
                submitLabel="Create"
                submittingLabel="Creating…"
                submitting={createRecipeMutation.isPending}
                submitDisabled={!canEdit || !projectId}
                cancelDisabled={createRecipeMutation.isPending}
                onCancel={() => setCreateOpen(false)}
                onSubmit={() => {
                    setCreateError(null);
                    const trimmedName = createName.trim();
                    if (!trimmedName) {
                        setCreateError("Recipe name is required.");
                        return;
                    }

                    const base = Number(createBaseTime);
                    if (!Number.isFinite(base) || base <= 0) {
                        setCreateError(
                            "Base crafting time must be a positive number.",
                        );
                        return;
                    }

                    const inputsErr = validateExchanges(createInputs, "Inputs");
                    if (inputsErr) {
                        setCreateError(inputsErr);
                        return;
                    }
                    const outputsErr = validateExchanges(
                        createOutputs,
                        "Outputs",
                    );
                    if (outputsErr) {
                        setCreateError(outputsErr);
                        return;
                    }

                    createRecipeMutation.mutate({
                        name: trimmedName,
                        description: createDescription.trim()
                            ? createDescription.trim()
                            : null,
                        baseCraftingTime: base,
                        inputs: createInputs.map((i) => ({
                            puid: i.puid,
                            quantity: Number(i.quantity),
                        })),
                        outputs: createOutputs.map((o) => ({
                            puid: o.puid,
                            quantity: Number(o.quantity),
                        })),
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
                            placeholder="Iron plate"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createRecipeMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Base crafting time (seconds)
                        </label>
                        <input
                            value={createBaseTime}
                            onChange={(e) => setCreateBaseTime(e.target.value)}
                            inputMode="decimal"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createRecipeMutation.isPending}
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
                            disabled={createRecipeMutation.isPending}
                        />
                    </div>

                    <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                        <div className="flex items-center justify-between gap-3">
                            <div className="text-sm font-medium text-slate-200">
                                Inputs
                            </div>
                            <button
                                type="button"
                                className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                onClick={() => {
                                    const used = new Set(
                                        createInputs.map((i) => i.puid),
                                    );
                                    const nextPuid = pickDefaultProductPuid(
                                        sortedProducts,
                                        used,
                                    );
                                    if (!nextPuid) return;
                                    setCreateInputs((prev) => [
                                        ...prev,
                                        { puid: nextPuid, quantity: 1 },
                                    ]);
                                }}
                                disabled={
                                    createRecipeMutation.isPending ||
                                    sortedProducts.length === 0
                                }
                                title={
                                    sortedProducts.length === 0
                                        ? "Create products first"
                                        : "Add input"
                                }
                            >
                                <IconPlus size={16} />
                                Add
                            </button>
                        </div>

                        {sortedProducts.length === 0 ? (
                            <div className="mt-2 text-sm text-slate-500">
                                No products available in this project.
                            </div>
                        ) : null}

                        <div className="mt-3 flex flex-col gap-2">
                            {createInputs.length === 0 && (
                                <div className="text-sm text-slate-500">
                                    No inputs
                                </div>
                            )}
                            {createInputs.map((row, idx) => (
                                <div
                                    key={`create-in-${idx}`}
                                    className="flex items-center gap-2"
                                >
                                    <ProductDropDown
                                        value={row.puid}
                                        disabled={
                                            createRecipeMutation.isPending
                                        }
                                        onSelect={(next) => {
                                            setCreateInputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              puid: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                    />
                                    <input
                                        value={String(row.quantity)}
                                        onChange={(e) => {
                                            const next = Number(e.target.value);
                                            setCreateInputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              quantity: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                        inputMode="decimal"
                                        className="w-28 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            createRecipeMutation.isPending
                                        }
                                    />
                                    <button
                                        type="button"
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                        onClick={() =>
                                            setCreateInputs((prev) =>
                                                prev.filter(
                                                    (_, i) => i !== idx,
                                                ),
                                            )
                                        }
                                        disabled={
                                            createRecipeMutation.isPending
                                        }
                                        title="Remove"
                                        aria-label="Remove input"
                                    >
                                        <IconTrash size={18} />
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                        <div className="flex items-center justify-between gap-3">
                            <div className="text-sm font-medium text-slate-200">
                                Outputs
                            </div>
                            <button
                                type="button"
                                className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                onClick={() => {
                                    const used = new Set(
                                        createOutputs.map((o) => o.puid),
                                    );
                                    const nextPuid = pickDefaultProductPuid(
                                        sortedProducts,
                                        used,
                                    );
                                    if (!nextPuid) return;
                                    setCreateOutputs((prev) => [
                                        ...prev,
                                        { puid: nextPuid, quantity: 1 },
                                    ]);
                                }}
                                disabled={
                                    createRecipeMutation.isPending ||
                                    sortedProducts.length === 0
                                }
                                title={
                                    sortedProducts.length === 0
                                        ? "Create products first"
                                        : "Add output"
                                }
                            >
                                <IconPlus size={16} />
                                Add
                            </button>
                        </div>

                        {sortedProducts.length === 0 ? (
                            <div className="mt-2 text-sm text-slate-500">
                                No products available in this project.
                            </div>
                        ) : null}

                        <div className="mt-3 flex flex-col gap-2">
                            {createOutputs.length === 0 && (
                                <div className="text-sm text-slate-500">
                                    No outputs
                                </div>
                            )}
                            {createOutputs.map((row, idx) => (
                                <div
                                    key={`create-out-${idx}`}
                                    className="flex items-center gap-2"
                                >
                                    <ProductDropDown
                                        value={row.puid}
                                        disabled={
                                            createRecipeMutation.isPending
                                        }
                                        onSelect={(next) => {
                                            setCreateOutputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              puid: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                    />
                                    <input
                                        value={String(row.quantity)}
                                        onChange={(e) => {
                                            const next = Number(e.target.value);
                                            setCreateOutputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              quantity: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                        inputMode="decimal"
                                        className="w-28 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            createRecipeMutation.isPending
                                        }
                                    />
                                    <button
                                        type="button"
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                        onClick={() =>
                                            setCreateOutputs((prev) =>
                                                prev.filter(
                                                    (_, i) => i !== idx,
                                                ),
                                            )
                                        }
                                        disabled={
                                            createRecipeMutation.isPending
                                        }
                                        title="Remove"
                                        aria-label="Remove output"
                                    >
                                        <IconTrash size={18} />
                                    </button>
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
                title="Edit recipe"
                description="Update recipe details."
                initialFocusRef={editNameRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setEditOpen(false)}
                            disabled={updateRecipeMutation.isPending}
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
                                    setEditError("Recipe name is required.");
                                    return;
                                }
                                if (!editTarget) {
                                    setEditError("No recipe selected.");
                                    return;
                                }

                                const base = Number(editBaseTime);
                                if (!Number.isFinite(base) || base <= 0) {
                                    setEditError(
                                        "Base crafting time must be a positive number.",
                                    );
                                    return;
                                }

                                const inputsErr = validateExchanges(
                                    editInputs,
                                    "Inputs",
                                );
                                if (inputsErr) {
                                    setEditError(inputsErr);
                                    return;
                                }
                                const outputsErr = validateExchanges(
                                    editOutputs,
                                    "Outputs",
                                );
                                if (outputsErr) {
                                    setEditError(outputsErr);
                                    return;
                                }

                                updateRecipeMutation.mutate({
                                    name: trimmedName,
                                    description: editDescription.trim()
                                        ? editDescription.trim()
                                        : null,
                                    baseCraftingTime: base,
                                    inputs: editInputs.map((i) => ({
                                        puid: i.puid,
                                        quantity: Number(i.quantity),
                                    })),
                                    outputs: editOutputs.map((o) => ({
                                        puid: o.puid,
                                        quantity: Number(o.quantity),
                                    })),
                                });
                            }}
                            disabled={
                                updateRecipeMutation.isPending ||
                                !canEdit ||
                                !editTarget
                            }
                        >
                            {updateRecipeMutation.isPending
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
                            disabled={updateRecipeMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Base crafting time (seconds)
                        </label>
                        <input
                            value={editBaseTime}
                            onChange={(e) => setEditBaseTime(e.target.value)}
                            inputMode="decimal"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={updateRecipeMutation.isPending}
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
                            disabled={updateRecipeMutation.isPending}
                        />
                    </div>

                    <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                        <div className="flex items-center justify-between gap-3">
                            <div className="text-sm font-medium text-slate-200">
                                Inputs
                            </div>
                            <button
                                type="button"
                                className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                onClick={() => {
                                    const used = new Set(
                                        editInputs.map((i) => i.puid),
                                    );
                                    const nextPuid = pickDefaultProductPuid(
                                        sortedProducts,
                                        used,
                                    );
                                    if (!nextPuid) return;
                                    setEditInputs((prev) => [
                                        ...prev,
                                        { puid: nextPuid, quantity: 1 },
                                    ]);
                                }}
                                disabled={
                                    updateRecipeMutation.isPending ||
                                    sortedProducts.length === 0
                                }
                                title={
                                    sortedProducts.length === 0
                                        ? "Create products first"
                                        : "Add input"
                                }
                            >
                                <IconPlus size={16} />
                                Add
                            </button>
                        </div>
                        <div className="mt-3 flex flex-col gap-2">
                            {editInputs.length === 0 && (
                                <div className="text-sm text-slate-500">
                                    No inputs
                                </div>
                            )}
                            {editInputs.map((row, idx) => (
                                <div
                                    key={`edit-in-${idx}`}
                                    className="flex items-center gap-2"
                                >
                                    <ProductDropDown
                                        value={row.puid}
                                        disabled={
                                            updateRecipeMutation.isPending
                                        }
                                        onSelect={(next) => {
                                            setEditInputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              puid: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                    />
                                    <input
                                        value={String(row.quantity)}
                                        onChange={(e) => {
                                            const next = Number(e.target.value);
                                            setEditInputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              quantity: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                        inputMode="decimal"
                                        className="w-28 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            updateRecipeMutation.isPending
                                        }
                                    />
                                    <button
                                        type="button"
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                        onClick={() =>
                                            setEditInputs((prev) =>
                                                prev.filter(
                                                    (_, i) => i !== idx,
                                                ),
                                            )
                                        }
                                        disabled={
                                            updateRecipeMutation.isPending
                                        }
                                        title="Remove"
                                        aria-label="Remove input"
                                    >
                                        <IconTrash size={18} />
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="rounded-lg border border-slate-800 bg-slate-950/20 p-3">
                        <div className="flex items-center justify-between gap-3">
                            <div className="text-sm font-medium text-slate-200">
                                Outputs
                            </div>
                            <button
                                type="button"
                                className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                onClick={() => {
                                    const used = new Set(
                                        editOutputs.map((o) => o.puid),
                                    );
                                    const nextPuid = pickDefaultProductPuid(
                                        sortedProducts,
                                        used,
                                    );
                                    if (!nextPuid) return;
                                    setEditOutputs((prev) => [
                                        ...prev,
                                        { puid: nextPuid, quantity: 1 },
                                    ]);
                                }}
                                disabled={
                                    updateRecipeMutation.isPending ||
                                    sortedProducts.length === 0
                                }
                                title={
                                    sortedProducts.length === 0
                                        ? "Create products first"
                                        : "Add output"
                                }
                            >
                                <IconPlus size={16} />
                                Add
                            </button>
                        </div>
                        <div className="mt-3 flex flex-col gap-2">
                            {editOutputs.length === 0 && (
                                <div className="text-sm text-slate-500">
                                    No outputs
                                </div>
                            )}
                            {editOutputs.map((row, idx) => (
                                <div
                                    key={`edit-out-${idx}`}
                                    className="flex items-center gap-2"
                                >
                                    <ProductDropDown
                                        value={row.puid}
                                        disabled={
                                            updateRecipeMutation.isPending
                                        }
                                        onSelect={(next) => {
                                            setEditOutputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              puid: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                    />
                                    <input
                                        value={String(row.quantity)}
                                        onChange={(e) => {
                                            const next = Number(e.target.value);
                                            setEditOutputs((prev) =>
                                                prev.map((p, i) =>
                                                    i === idx
                                                        ? {
                                                              ...p,
                                                              quantity: next,
                                                          }
                                                        : p,
                                                ),
                                            );
                                        }}
                                        inputMode="decimal"
                                        className="w-28 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                        disabled={
                                            updateRecipeMutation.isPending
                                        }
                                    />
                                    <button
                                        type="button"
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                        onClick={() =>
                                            setEditOutputs((prev) =>
                                                prev.filter(
                                                    (_, i) => i !== idx,
                                                ),
                                            )
                                        }
                                        disabled={
                                            updateRecipeMutation.isPending
                                        }
                                        title="Remove"
                                        aria-label="Remove output"
                                    >
                                        <IconTrash size={18} />
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </Popup>
        </ProjectPageLayout>
    );
}
