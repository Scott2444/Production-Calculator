"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import { useParams } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import {
    fetchProducts,
    type NewProductPayload,
    postNewProduct,
    updateProduct,
    deleteProduct,
} from "@/lib/products";
import { useProtectedApi } from "@/lib/api";
import Popup from "@/components/Popup";
import { useAuth } from "@/context/AuthContext";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { fetchProjects } from "@/lib/projects";
import { useSearch } from "@/hooks/Search";
import {
    IconCheck,
    IconEdit,
    IconPlus,
    IconSearch,
    IconTrash,
    IconX,
} from "@tabler/icons-react";
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

interface Product {
    puid: string;
    name: string;
    description: string | null;
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
    const params = useParams<{ username: string; project_name: string }>();
    const username = params?.username ?? "";
    const routeProjectName = params?.project_name
        ? safeDecodeURIComponent(params.project_name)
        : "";

    const { userId, loggedIn } = useAuth();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const projectsQuery = useQuery({
        queryKey: ["projects", userId],
        queryFn: () => fetchProjects(userId!, protectedApi),
        staleTime: 5 * 60 * 1000,
        enabled: Boolean(userId),
    });

    const currentProject = useMemo(() => {
        const projects = projectsQuery.data as Project[] | undefined;
        if (!projects || !routeProjectName) return null;
        return projects.find((p) => p.name === routeProjectName) ?? null;
    }, [projectsQuery.data, routeProjectName]);

    const projectId = currentProject?.puid ?? "";

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

    const {
        searchText,
        setSearchText,
        filteredItems: filteredProducts,
    } = useSearch(sortedProducts, {
        toText: (p) => `${p.name} ${p.description ?? ""}`,
    });

    const canEdit = loggedIn && Boolean(currentProject);

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

    const [confirmDeletePuid, setConfirmDeletePuid] = useState<string | null>(
        null,
    );
    const [deleteError, setDeleteError] = useState<string | null>(null);

    useEffect(() => {
        setConfirmDeletePuid(null);
        setDeleteError(null);
    }, [projectId]);

    useEffect(() => {
        if (!confirmDeletePuid) return;

        const handlePointerDown = (event: MouseEvent | TouchEvent) => {
            const target = event.target as HTMLElement | null;
            if (!target) return;

            const withinDeleteButton = target.closest(
                '[data-delete-confirm="true"]',
            );
            if (withinDeleteButton) return;

            setConfirmDeletePuid(null);
        };

        document.addEventListener("mousedown", handlePointerDown);
        document.addEventListener("touchstart", handlePointerDown);
        return () => {
            document.removeEventListener("mousedown", handlePointerDown);
            document.removeEventListener("touchstart", handlePointerDown);
        };
    }, [confirmDeletePuid]);

    const deleteProductMutation = useMutation({
        mutationFn: async (puid: string) => {
            if (!projectId) throw new Error("No project selected.");
            await deleteProduct(projectId, puid, protectedApi);
        },
        onSuccess: async () => {
            setConfirmDeletePuid(null);
            setDeleteError(null);
            await queryClient.invalidateQueries({
                queryKey: ["products", projectId],
            });
        },
        onError: (err) => {
            setConfirmDeletePuid(null);
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
                            {username ? (
                                <span> • Owner: {username}</span>
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
                                ? "Add product"
                                : "Sign in to manage products"
                        }
                    >
                        <IconPlus size={18} />
                        Add product
                    </button>
                </div>

                {projectsQuery.isLoading && (
                    <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                        Loading project…
                    </div>
                )}
                {!projectsQuery.isLoading && projectsQuery.error && (
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

                <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-4">
                    <div className="flex items-center gap-3">
                        <div className="text-slate-400">
                            <IconSearch size={18} />
                        </div>
                        <input
                            value={searchText}
                            onChange={(e) => setSearchText(e.target.value)}
                            placeholder="Search products…"
                            className="w-full rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={!projectId}
                        />
                    </div>
                </div>

                {deleteError && (
                    <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                        <div className="flex items-start justify-between gap-3">
                            <div className="min-w-0 align-middle">
                                {deleteError}
                            </div>
                            <button
                                type="button"
                                className="rounded-md p-1 text-red-200/90 transition-colors hover:bg-red-900/20 hover:text-red-100 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                                onClick={() => setDeleteError(null)}
                                aria-label="Dismiss error"
                                title="Dismiss"
                            >
                                <IconX size={18} />
                            </button>
                        </div>
                    </div>
                )}

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
                    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
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
                                                confirmDeletePuid ===
                                                product.puid
                                                    ? "rounded-lg border border-red-500/60 bg-red-600/30 p-2 text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                    : "rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                            }
                                            title={
                                                confirmDeletePuid ===
                                                product.puid
                                                    ? "Click again to confirm"
                                                    : "Delete product"
                                            }
                                            aria-label={
                                                confirmDeletePuid ===
                                                product.puid
                                                    ? "Confirm delete product"
                                                    : "Delete product"
                                            }
                                            onClick={() => {
                                                if (!canEdit) return;

                                                setDeleteError(null);

                                                if (
                                                    confirmDeletePuid ===
                                                    product.puid
                                                ) {
                                                    deleteProductMutation.mutate(
                                                        product.puid,
                                                    );
                                                    return;
                                                }

                                                setConfirmDeletePuid(
                                                    product.puid,
                                                );
                                            }}
                                            disabled={
                                                !canEdit ||
                                                deleteProductMutation.isPending
                                            }
                                        >
                                            {confirmDeletePuid ===
                                            product.puid ? (
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
            </div>

            <Popup
                open={createOpen}
                onOpenChange={(next) => {
                    setCreateOpen(next);
                    if (next) setCreateError(null);
                }}
                title="Add product"
                description="Create a new product in this project."
                initialFocusRef={createNameRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setCreateOpen(false)}
                            disabled={createProductMutation.isPending}
                        >
                            Cancel
                        </button>
                        <button
                            type="button"
                            className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={() => {
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
                            disabled={
                                createProductMutation.isPending ||
                                !canEdit ||
                                !projectId
                            }
                        >
                            {createProductMutation.isPending
                                ? "Creating…"
                                : "Create"}
                        </button>
                    </div>
                }
            >
                <div className="flex flex-col gap-4">
                    {createError && (
                        <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                            {createError}
                        </div>
                    )}

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
                            disabled={createProductMutation.isPending}
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
                            disabled={createProductMutation.isPending}
                        />
                    </div>
                </div>
            </Popup>

            <Popup
                open={editOpen}
                onOpenChange={(next) => {
                    setEditOpen(next);
                    if (next) setEditError(null);
                }}
                title="Edit product"
                description="Update product details."
                initialFocusRef={editNameRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setEditOpen(false)}
                            disabled={updateProductMutation.isPending}
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
                            disabled={
                                updateProductMutation.isPending ||
                                !canEdit ||
                                !editTarget
                            }
                        >
                            {updateProductMutation.isPending
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
                            disabled={updateProductMutation.isPending}
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
                            disabled={updateProductMutation.isPending}
                        />
                    </div>
                </div>
            </Popup>
        </ProjectPageLayout>
    );
}
