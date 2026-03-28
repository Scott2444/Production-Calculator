"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import SearchBar from "@/components/SearchBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { useEffect, useMemo, useRef, useState } from "react";
import {
    type NewProductPayload,
    postNewProduct,
    updateProduct,
    deleteProduct,
} from "@/lib/products";
import { useProtectedApi } from "@/lib/api";
import ProductEditorDialog from "@/components/ProductEditorDialog";
import { useAuth } from "@/context/AuthContext";
import { useProject } from "@/context/ProjectContext";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSearch } from "@/hooks/Search";
import { useDeleteConfirmation } from "@/hooks/DeleteConfirmation";
import { IconCheck, IconEdit, IconPlus, IconTrash } from "@tabler/icons-react";
import ReactMarkdown from "react-markdown";
import { useProductsQuery } from "@/hooks/useQueries";
import { type Product } from "@/types/products";

function coerceProducts(value: unknown): Product[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Product[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Product[];
    }
    return [];
}

export default function Products() {
    const { loggedIn } = useAuth();
    const { routeUsername, routeProjectName, projectId, canEdit } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const productsQuery = useProductsQuery(projectId);

    const products = useMemo(
        () => coerceProducts(productsQuery.data),
        [productsQuery.data],
    );

    const sortedProducts = useMemo(() => {
        return [...products].sort((a, b) =>
            a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
        );
    }, [products]);

    const {
        searchText,
        setSearchText,
        filteredItems: filteredProducts,
    } = useSearch(sortedProducts, {
        toText: (p) => `${p.name} ${p.description ?? ""}`,
    });

    const [createOpen, setCreateOpen] = useState(false);
    const [createName, setCreateName] = useState("");
    const [createDescription, setCreateDescription] = useState("");
    const [createError, setCreateError] = useState<string | null>(null);
    const createNameRef = useRef<HTMLInputElement>(null);

    const createProductMutation = useMutation({
        mutationFn: async (payload: NewProductPayload) => {
            if (!projectId) throw new Error("No project selected.");
            return postNewProduct(
                projectId,
                protectedApi,
                payload,
            ) as Promise<Product>;
        },
        onSuccess: async () => {
            setCreateError(null);
            setCreateName("");
            setCreateDescription("");
            setCreateOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["products", projectId],
            });
        },
        onError: (err) => {
            setCreateError(
                err instanceof Error
                    ? err.message
                    : "Failed to create product.",
            );
        },
    });

    const [editOpen, setEditOpen] = useState(false);
    const [editTarget, setEditTarget] = useState<Product | null>(null);
    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");
    const [editError, setEditError] = useState<string | null>(null);
    const editNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!editOpen) return;
        setEditError(null);
        setEditName(editTarget?.name ?? "");
        setEditDescription(editTarget?.description ?? "");
    }, [editOpen, editTarget]);

    const updateProductMutation = useMutation({
        mutationFn: async (payload: NewProductPayload) => {
            if (!projectId) throw new Error("No project selected.");
            if (!editTarget) throw new Error("No product selected.");
            return updateProduct(
                projectId,
                editTarget.puid,
                protectedApi,
                payload,
            ) as Promise<Product>;
        },
        onSuccess: async () => {
            setEditError(null);
            setEditOpen(false);
            await queryClient.invalidateQueries({
                queryKey: ["products", projectId],
            });
        },
        onError: (err) => {
            setEditError(
                err instanceof Error
                    ? err.message
                    : "Failed to update product.",
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

    const deleteProductMutation = useMutation({
        mutationFn: async (puid: string) => {
            if (!projectId) throw new Error("No project selected.");
            await deleteProduct(projectId, puid, protectedApi);
        },
        onSuccess: async () => {
            deleteConfirm.reset();
            setDeleteError(null);
            await queryClient.invalidateQueries({
                queryKey: ["products", projectId],
            });
        },
        onError: (err) => {
            deleteConfirm.reset();
            setDeleteError(
                err instanceof Error
                    ? err.message
                    : "Failed to delete product.",
            );
        },
    });

    return (
        <ProjectPageLayout>
            <div className="flex flex-col gap-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            Products
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
                                    ? "Add product"
                                    : loggedIn
                                      ? "Only the project owner can manage products"
                                      : "Sign in to manage products"
                            }
                        >
                            <IconPlus size={18} />
                            Add product
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

                    {productsQuery.isLoading && projectId && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                            Loading products…
                        </div>
                    )}

                    {!productsQuery.isLoading && productsQuery.error && (
                        <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            Failed to load products.
                        </div>
                    )}

                    {!productsQuery.isLoading &&
                        !productsQuery.error &&
                        projectId &&
                        filteredProducts.length === 0 && (
                            <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-sm text-slate-300">
                                {searchText.trim()
                                    ? "No products match your search."
                                    : "No products yet."}
                            </div>
                        )}

                    {filteredProducts.length > 0 && (
                        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 2xl:grid-cols-3">
                            {filteredProducts.map((product) => (
                                <div
                                    key={product.puid}
                                    className="rounded-xl border border-slate-800 bg-slate-900/40 p-4"
                                >
                                    <div className="flex items-start justify-between gap-3">
                                        <div className="min-w-0">
                                            <div className="truncate text-base font-semibold text-slate-100">
                                                {product.name}
                                            </div>
                                            {product.description ? (
                                                <div className="mt-1 text-sm text-slate-300">
                                                    <ReactMarkdown>
                                                        {product.description}
                                                    </ReactMarkdown>
                                                </div>
                                            ) : (
                                                <div className="mt-1 text-sm text-slate-500">
                                                    No description
                                                </div>
                                            )}
                                        </div>

                                        {canEdit && (
                                            <div className="flex gap-2">
                                                <button
                                                    type="button"
                                                    className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                    title="Edit product"
                                                    aria-label="Edit product"
                                                    onClick={() => {
                                                        setEditTarget(product);
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
                                                            product.puid,
                                                        )
                                                            ? "rounded-lg border border-red-500/60 bg-red-600/30 p-2 text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                            : "rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                    }
                                                    title={
                                                        deleteConfirm.isConfirming(
                                                            product.puid,
                                                        )
                                                            ? "Click again to confirm"
                                                            : "Delete product"
                                                    }
                                                    aria-label={
                                                        deleteConfirm.isConfirming(
                                                            product.puid,
                                                        )
                                                            ? "Confirm delete product"
                                                            : "Delete product"
                                                    }
                                                    onClick={() => {
                                                        if (!canEdit) return;

                                                        setDeleteError(null);

                                                        deleteConfirm.confirmOrRequest(
                                                            product.puid,
                                                            () => {
                                                                deleteProductMutation.mutate(
                                                                    product.puid,
                                                                );
                                                            },
                                                        );
                                                    }}
                                                    disabled={
                                                        !canEdit ||
                                                        deleteProductMutation.isPending
                                                    }
                                                >
                                                    {deleteConfirm.isConfirming(
                                                        product.puid,
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
            </div>

            <ProductEditorDialog
                mode="create"
                open={createOpen}
                onOpenChange={(next) => {
                    setCreateOpen(next);
                    if (next) setCreateError(null);
                }}
                name={createName}
                description={createDescription}
                onNameChange={setCreateName}
                onDescriptionChange={setCreateDescription}
                error={createError}
                onDismissError={() => setCreateError(null)}
                initialFocusRef={createNameRef}
                submitting={createProductMutation.isPending}
                submitDisabled={!canEdit || !projectId}
                onCancel={() => setCreateOpen(false)}
                onSubmit={() => {
                    setCreateError(null);
                    const trimmed = createName.trim();
                    if (!trimmed) {
                        setCreateError("Product name is required.");
                        return;
                    }

                    createProductMutation.mutate({
                        name: trimmed,
                        description: createDescription.trim()
                            ? createDescription.trim()
                            : null,
                    });
                }}
            />

            <ProductEditorDialog
                mode="edit"
                open={editOpen}
                onOpenChange={(next) => {
                    setEditOpen(next);
                    if (next) setEditError(null);
                }}
                name={editName}
                description={editDescription}
                onNameChange={setEditName}
                onDescriptionChange={setEditDescription}
                error={editError}
                onDismissError={() => setEditError(null)}
                initialFocusRef={editNameRef}
                submitting={updateProductMutation.isPending}
                submitDisabled={
                    updateProductMutation.isPending || !canEdit || !editTarget
                }
                onCancel={() => setEditOpen(false)}
                onSubmit={() => {
                    setEditError(null);
                    const trimmed = editName.trim();
                    if (!trimmed) {
                        setEditError("Product name is required.");
                        return;
                    }
                    if (!editTarget) {
                        setEditError("No product selected.");
                        return;
                    }

                    updateProductMutation.mutate({
                        name: trimmed,
                        description: editDescription.trim()
                            ? editDescription.trim()
                            : null,
                    });
                }}
            />
        </ProjectPageLayout>
    );
}
