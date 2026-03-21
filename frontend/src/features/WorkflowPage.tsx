"use client";

import ErrorDisplay from "@/components/ErrorDisplay";
import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import { useProject } from "@/context/ProjectContext";
import { useProtectedApi } from "@/lib/api";
import { fetchAttributes } from "@/lib/attributes";
import { fetchMachines } from "@/lib/machines";
import { fetchModifiers } from "@/lib/modifiers";
import { fetchProducts } from "@/lib/products";
import { fetchRecipes } from "@/lib/recipes";
import { fetchWorkflows, type Workflow } from "@/lib/workflow";
import {
    fetchWorkflowChart,
    setWorkflowProductExternal,
    updateWorkflowChart,
    updateWorkflowNode,
    updateWorkflowPreferredRecipes,
    updateWorkflowTargets,
    type AttributeRate,
    type Modifier,
    type ProductNode,
    type Target,
    type WorkflowChart,
} from "@/lib/workflowChart";
import { buildWorkflowLayout } from "@/lib/workflowLayout";
import {
    Background,
    Controls,
    Handle,
    MarkerType,
    MiniMap,
    Position,
    ReactFlow,
    type Edge as FlowEdge,
    type Node as FlowNode,
    type NodeProps,
} from "@xyflow/react";
import {
    IconArrowsShuffle,
    IconChecklist,
    IconDatabaseImport,
    IconRefresh,
    IconSettings,
    IconTargetArrow,
} from "@tabler/icons-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouterState } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import "@xyflow/react/dist/style.css";

interface Product {
    puid: string;
    name: string;
}

interface Recipe {
    puid: string;
    name: string;
}

interface Machine {
    puid: string;
    name: string;
    recipePuids: string[];
}

interface ModifierItem {
    puid: string;
    name: string;
}

interface Attribute {
    puid: string;
    name: string;
    unit: string | null;
}

interface ProcessNodeData {
    [key: string]: unknown;
    puid: string;
    recipeName: string;
    machineName: string;
    calculatedMachineCount: number | null;
    actualMachineCount: number | null;
    calculatedTargetRate: number | null;
    calculatedActualRate: number | null;
    modifierCount: number;
    preferredRecipe: boolean;
}

interface ProductFlowNodeData {
    [key: string]: unknown;
    puid: string;
    productName: string;
    calculatedFlowRate: number;
    actualFlowRateIn: number;
    actualFlowRateOut: number;
    isExternal: boolean;
    targetRate: number | null;
}

type UiSelection =
    | { type: "process"; puid: string }
    | { type: "product"; puid: string }
    | null;

type FlowNodeData = ProcessNodeData | ProductFlowNodeData;

function normalizePath(path: string): string {
    if (!path) return "/";
    if (path.length > 1 && path.endsWith("/")) {
        return path.slice(0, -1);
    }
    return path;
}

function safeDecodeURIComponent(value: string): string {
    try {
        return decodeURIComponent(value);
    } catch {
        return value;
    }
}

function getPathSegments(pathname: string): string[] {
    return normalizePath(pathname)
        .split("/")
        .filter(Boolean)
        .map(safeDecodeURIComponent);
}

function getWorkflowRouteSegment(workflow: Workflow): string {
    const trimmedName = workflow.name?.trim();
    return trimmedName && trimmedName.length > 0 ? trimmedName : workflow.puid;
}

function coerceItems<T>(value: unknown): T[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as T[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as T[];
    }
    return [];
}

function coerceWorkflowChart(value: unknown): WorkflowChart {
    if (!value || typeof value !== "object") {
        return {
            nodes: [],
            edges: [],
            targets: [],
            productNodes: [],
            preferredRecipes: [],
        };
    }

    const data = value as Partial<WorkflowChart>;
    return {
        nodes: Array.isArray(data.nodes) ? data.nodes : [],
        edges: Array.isArray(data.edges) ? data.edges : [],
        targets: Array.isArray(data.targets) ? data.targets : [],
        productNodes: Array.isArray(data.productNodes) ? data.productNodes : [],
        preferredRecipes: Array.isArray(data.preferredRecipes)
            ? data.preferredRecipes
            : [],
    };
}

function formatRate(value: number | null | undefined): string {
    if (value === null || value === undefined || Number.isNaN(value))
        return "-";
    const rounded = Math.round(value * 1000) / 1000;
    return Number.isInteger(rounded) ? `${rounded}` : rounded.toFixed(3);
}

function parseNonNegative(value: string): number | null {
    const trimmed = value.trim();
    if (!trimmed) return null;
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed) || parsed < 0) return null;
    return parsed;
}

function ProcessFlowNode({
    data,
    selected,
}: NodeProps<FlowNode<ProcessNodeData>>) {
    return (
        <div
            className={`w-[260px] rounded-xl border p-3 shadow-sm ${
                selected
                    ? "border-purple-400 bg-slate-800"
                    : "border-slate-700 bg-slate-900"
            }`}
        >
            <Handle
                type="target"
                position={Position.Left}
                className="!h-2 !w-2 !bg-slate-200"
            />
            <Handle
                type="source"
                position={Position.Right}
                className="!h-2 !w-2 !bg-slate-200"
            />

            <div className="text-xs uppercase tracking-wide text-slate-400">
                Recipe
            </div>
            <div className="truncate text-sm font-semibold text-slate-100">
                {data.recipeName}
            </div>
            <div className="mt-2 text-xs text-slate-300">
                Machine: {data.machineName}
            </div>
            <div className="mt-1 grid grid-cols-2 gap-2 text-xs text-slate-300">
                <div>Need: {formatRate(data.calculatedMachineCount)}</div>
                <div>Built: {formatRate(data.actualMachineCount)}</div>
                <div>Target: {formatRate(data.calculatedTargetRate)}</div>
                <div>Actual: {formatRate(data.calculatedActualRate)}</div>
            </div>
            <div className="mt-2 flex items-center justify-between text-[11px] text-slate-400">
                <span>Modifiers: {data.modifierCount}</span>
                {data.preferredRecipe ? (
                    <span className="rounded-md border border-amber-600/50 bg-amber-700/20 px-2 py-0.5 text-amber-200">
                        Preferred
                    </span>
                ) : null}
            </div>
        </div>
    );
}

function ProductFlowNode({
    data,
    selected,
}: NodeProps<FlowNode<ProductFlowNodeData>>) {
    return (
        <div
            className={`w-[220px] rounded-xl border p-3 shadow-sm ${
                selected
                    ? "border-cyan-400 bg-slate-800"
                    : "border-cyan-900/70 bg-slate-900"
            }`}
        >
            <Handle
                type="target"
                position={Position.Left}
                className="!h-2 !w-2 !bg-cyan-200"
            />
            <Handle
                type="source"
                position={Position.Right}
                className="!h-2 !w-2 !bg-cyan-200"
            />

            <div className="truncate text-sm font-semibold text-cyan-100">
                {data.productName}
            </div>
            <div className="mt-1 text-xs text-cyan-200/80">
                Flow: {formatRate(data.calculatedFlowRate)}/s
            </div>
            <div className="mt-1 grid grid-cols-2 gap-2 text-xs text-slate-300">
                <div>In: {formatRate(data.actualFlowRateIn)}</div>
                <div>Out: {formatRate(data.actualFlowRateOut)}</div>
            </div>
            <div className="mt-2 flex items-center justify-between text-[11px] text-slate-400">
                <span>{data.isExternal ? "External" : "Internal"}</span>
                {data.targetRate !== null ? (
                    <span className="rounded-md border border-purple-600/50 bg-purple-700/20 px-2 py-0.5 text-purple-100">
                        Target {formatRate(data.targetRate)}/s
                    </span>
                ) : null}
            </div>
        </div>
    );
}

const nodeTypes = {
    processNode: ProcessFlowNode,
    productNode: ProductFlowNode,
};

export default function WorkflowPage() {
    const { routeProjectName, routeUsername, projectId, isOwner } =
        useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const pathname = useRouterState({
        select: (state) => state.location.pathname,
    });
    const workflowRouteSegment = useMemo(() => {
        const segments = getPathSegments(pathname);
        return segments[3] ?? "";
    }, [pathname]);

    const workflowsQuery = useQuery({
        queryKey: ["workflows", projectId],
        queryFn: () => fetchWorkflows(projectId, protectedApi),
        enabled: Boolean(projectId) && isOwner,
        staleTime: 60 * 1000,
    });

    const workflows = useMemo(
        () => coerceItems<Workflow>(workflowsQuery.data),
        [workflowsQuery.data],
    );

    const selectedWorkflow = useMemo(() => {
        return workflows.find(
            (workflow) =>
                getWorkflowRouteSegment(workflow).toLowerCase() ===
                workflowRouteSegment.toLowerCase(),
        );
    }, [workflows, workflowRouteSegment]);

    const workflowId = selectedWorkflow?.puid ?? "";

    const chartQuery = useQuery({
        queryKey: ["workflow-chart", projectId, workflowId],
        queryFn: () => fetchWorkflowChart(projectId, workflowId, protectedApi),
        enabled: Boolean(projectId && workflowId) && isOwner,
        staleTime: 0,
    });

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

    const attributesQuery = useQuery({
        queryKey: ["attributes", projectId],
        queryFn: () => fetchAttributes(projectId, protectedApi),
        enabled: Boolean(projectId),
        staleTime: 60 * 1000,
    });

    const chart = useMemo(
        () => coerceWorkflowChart(chartQuery.data),
        [chartQuery.data],
    );
    useMemo(() => {
        console.log("Workflow chart data:", chart);
    }, [chart]);
    const products = useMemo(
        () => coerceItems<Product>(productsQuery.data),
        [productsQuery.data],
    );
    const recipes = useMemo(
        () => coerceItems<Recipe>(recipesQuery.data),
        [recipesQuery.data],
    );
    const machines = useMemo(
        () => coerceItems<Machine>(machinesQuery.data),
        [machinesQuery.data],
    );
    const modifiers = useMemo(
        () => coerceItems<ModifierItem>(modifiersQuery.data),
        [modifiersQuery.data],
    );
    const attributes = useMemo(
        () => coerceItems<Attribute>(attributesQuery.data),
        [attributesQuery.data],
    );

    const productNameByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const item of products) map.set(item.puid, item.name);
        return map;
    }, [products]);

    const recipeNameByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const item of recipes) map.set(item.puid, item.name);
        return map;
    }, [recipes]);

    const machineNameByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const item of machines) map.set(item.puid, item.name);
        return map;
    }, [machines]);

    const attributeByPuid = useMemo(() => {
        const map = new Map<string, Attribute>();
        for (const item of attributes) map.set(item.puid, item);
        return map;
    }, [attributes]);

    const targetByProduct = useMemo(() => {
        const map = new Map<string, number>();
        for (const target of chart.targets) {
            map.set(target.productPuid, target.targetRate);
        }
        return map;
    }, [chart.targets]);

    const productNodeByPuid = useMemo(() => {
        const map = new Map<string, ProductNode>();
        for (const productNode of chart.productNodes) {
            map.set(productNode.productPuid, productNode);
        }
        return map;
    }, [chart.productNodes]);

    const [selection, setSelection] = useState<UiSelection>(null);

    const selectedProcessNode = useMemo(() => {
        if (!selection || selection.type !== "process") return null;
        return chart.nodes.find((node) => node.puid === selection.puid) ?? null;
    }, [selection, chart.nodes]);

    const selectedProductNode = useMemo(() => {
        if (!selection || selection.type !== "product") return null;
        return productNodeByPuid.get(selection.puid) ?? null;
    }, [selection, productNodeByPuid]);

    useEffect(() => {
        if (!selection) return;
        if (
            selection.type === "process" &&
            !chart.nodes.some((node) => node.puid === selection.puid)
        ) {
            setSelection(null);
        }
        if (
            selection.type === "product" &&
            !chart.productNodes.some(
                (node) => node.productPuid === selection.puid,
            )
        ) {
            setSelection(null);
        }
    }, [selection, chart.nodes, chart.productNodes]);

    const [targetDrafts, setTargetDrafts] = useState<
        Array<{ productPuid: string; targetRate: string }>
    >([]);
    useEffect(() => {
        setTargetDrafts(
            chart.targets.map((target) => ({
                productPuid: target.productPuid,
                targetRate: `${target.targetRate}`,
            })),
        );
    }, [chart.targets]);

    const [preferredRecipePuids, setPreferredRecipePuids] = useState<string[]>(
        [],
    );
    useEffect(() => {
        setPreferredRecipePuids(chart.preferredRecipes);
    }, [chart.preferredRecipes]);

    const compatibleMachines = useMemo(() => {
        if (!selectedProcessNode) return [];
        return machines.filter((machine) =>
            Array.isArray(machine.recipePuids)
                ? machine.recipePuids.includes(selectedProcessNode.recipePuid)
                : false,
        );
    }, [machines, selectedProcessNode]);

    const [nodeMachinePuid, setNodeMachinePuid] = useState("");
    const [nodeActualMachineCount, setNodeActualMachineCount] = useState("");
    const [nodeModifierPuids, setNodeModifierPuids] = useState<string[]>([]);

    useEffect(() => {
        if (!selectedProcessNode) {
            setNodeMachinePuid("");
            setNodeActualMachineCount("");
            setNodeModifierPuids([]);
            return;
        }

        const fallbackMachine = compatibleMachines[0]?.puid ?? "";
        setNodeMachinePuid(selectedProcessNode.machinePuid ?? fallbackMachine);

        const count =
            selectedProcessNode.actualMachineCount ??
            selectedProcessNode.calculatedMachineCount ??
            0;
        setNodeActualMachineCount(`${count}`);
        setNodeModifierPuids(
            selectedProcessNode.modifiers.map((modifier) => modifier.puid),
        );
    }, [selectedProcessNode, compatibleMachines]);

    const [externalIsEnabled, setExternalIsEnabled] = useState(false);
    const [externalRate, setExternalRate] = useState("0");

    useEffect(() => {
        if (!selectedProductNode) {
            setExternalIsEnabled(false);
            setExternalRate("0");
            return;
        }

        setExternalIsEnabled(selectedProductNode.isExternal);
        const inferredRate = Math.max(
            0,
            selectedProductNode.actualFlowRateIn -
                selectedProductNode.actualFlowRateOut,
        );
        setExternalRate(`${inferredRate}`);
    }, [selectedProductNode]);

    const [interactionError, setInteractionError] = useState<string | null>(
        null,
    );

    const setChartData = (next: WorkflowChart) => {
        queryClient.setQueryData(
            ["workflow-chart", projectId, workflowId],
            next,
        );
    };

    const refreshMutation = useMutation({
        mutationFn: async () => {
            if (!projectId || !workflowId)
                throw new Error("No workflow selected.");
            const data = await updateWorkflowChart(
                projectId,
                workflowId,
                protectedApi,
            );
            return coerceWorkflowChart(data);
        },
        onSuccess: (next) => {
            setInteractionError(null);
            setChartData(next);
        },
        onError: (error) => {
            setInteractionError(
                error instanceof Error
                    ? error.message
                    : "Failed to rebuild workflow chart.",
            );
        },
    });

    const saveTargetsMutation = useMutation({
        mutationFn: async (payload: { targets: Target[] }) => {
            if (!projectId || !workflowId)
                throw new Error("No workflow selected.");
            const data = await updateWorkflowTargets(
                projectId,
                workflowId,
                protectedApi,
                payload,
            );
            return coerceWorkflowChart(data);
        },
        onSuccess: (next) => {
            setInteractionError(null);
            setChartData(next);
        },
        onError: (error) => {
            setInteractionError(
                error instanceof Error
                    ? error.message
                    : "Failed to update targets.",
            );
        },
    });

    const saveNodeMutation = useMutation({
        mutationFn: async (payload: {
            nodePuid: string;
            machinePuid: string;
            actualMachineCount: number;
            modifiers: Modifier[];
            recipeAttributes: AttributeRate[];
            machineAttributes: AttributeRate[];
        }) => {
            if (!projectId || !workflowId)
                throw new Error("No workflow selected.");
            const data = await updateWorkflowNode(
                projectId,
                workflowId,
                payload.nodePuid,
                protectedApi,
                {
                    machinePuid: payload.machinePuid,
                    actualMachineCount: payload.actualMachineCount,
                    modifiers: payload.modifiers,
                    recipeAttributes: payload.recipeAttributes,
                    machineAttributes: payload.machineAttributes,
                },
            );
            return coerceWorkflowChart(data);
        },
        onSuccess: (next) => {
            setInteractionError(null);
            setChartData(next);
        },
        onError: (error) => {
            setInteractionError(
                error instanceof Error
                    ? error.message
                    : "Failed to update node.",
            );
        },
    });

    const savePreferredRecipesMutation = useMutation({
        mutationFn: async (payload: { recipePuids: string[] }) => {
            if (!projectId || !workflowId)
                throw new Error("No workflow selected.");
            const data = await updateWorkflowPreferredRecipes(
                projectId,
                workflowId,
                protectedApi,
                payload,
            );
            return coerceWorkflowChart(data);
        },
        onSuccess: (next) => {
            setInteractionError(null);
            setChartData(next);
        },
        onError: (error) => {
            setInteractionError(
                error instanceof Error
                    ? error.message
                    : "Failed to update preferred recipes.",
            );
        },
    });

    const saveExternalMutation = useMutation({
        mutationFn: async (payload: {
            productPuid: string;
            isExternal: boolean;
            externalRate: number | null;
        }) => {
            if (!projectId || !workflowId)
                throw new Error("No workflow selected.");
            const data = await setWorkflowProductExternal(
                projectId,
                workflowId,
                payload.productPuid,
                protectedApi,
                {
                    isExternal: payload.isExternal,
                    externalRate: payload.externalRate,
                },
            );
            return coerceWorkflowChart(data);
        },
        onSuccess: (next) => {
            setInteractionError(null);
            setChartData(next);
        },
        onError: (error) => {
            setInteractionError(
                error instanceof Error
                    ? error.message
                    : "Failed to update external product state.",
            );
        },
    });

    const flowData = useMemo(() => {
        const processIds = chart.nodes.map((node) => `process:${node.puid}`);
        const productIds = new Set<string>();

        for (const productNode of chart.productNodes) {
            productIds.add(`product:${productNode.productPuid}`);
        }

        for (const edge of chart.edges) {
            if (
                edge.producerNodePuid === null ||
                edge.producerNodePuid === undefined ||
                edge.consumerNodePuid === null ||
                edge.consumerNodePuid === undefined
            ) {
                productIds.add(`product:${edge.productPuid}`);
            }
        }

        const layoutNodes = [
            ...processIds.map((id) => ({ id, kind: "node" as const })),
            ...[...productIds].map((id) => ({ id, kind: "product" as const })),
        ];

        const layoutEdges = chart.edges
            .map((edge) => {
                const source =
                    edge.producerNodePuid === null ||
                    edge.producerNodePuid === undefined
                        ? `product:${edge.productPuid}`
                        : `process:${edge.producerNodePuid}`;
                const target =
                    edge.consumerNodePuid === null ||
                    edge.consumerNodePuid === undefined
                        ? `product:${edge.productPuid}`
                        : `process:${edge.consumerNodePuid}`;
                return { source, target };
            })
            .filter((edge) => edge.source !== edge.target);

        const targetProductNodeIds = chart.targets.map(
            (target) => `product:${target.productPuid}`,
        );

        const positions = buildWorkflowLayout({
            nodes: layoutNodes,
            edges: layoutEdges,
            productNodeIds: [...productIds],
            targetProductNodeIds,
        });

        const nodes: FlowNode<FlowNodeData>[] = [
            ...chart.nodes.map((node) => {
                const id = `process:${node.puid}`;
                return {
                    id,
                    type: "processNode",
                    position: positions.get(id) ?? { x: 0, y: 0 },
                    data: {
                        puid: node.puid,
                        recipeName:
                            recipeNameByPuid.get(node.recipePuid) ??
                            node.recipePuid,
                        machineName: node.machinePuid
                            ? (machineNameByPuid.get(node.machinePuid) ??
                              node.machinePuid)
                            : "Unassigned",
                        calculatedMachineCount: node.calculatedMachineCount,
                        actualMachineCount: node.actualMachineCount,
                        calculatedTargetRate: node.calculatedTargetRate,
                        calculatedActualRate: node.calculatedActualRate,
                        modifierCount: node.modifiers.length,
                        preferredRecipe: chart.preferredRecipes.includes(
                            node.recipePuid,
                        ),
                    } satisfies ProcessNodeData,
                } as FlowNode<FlowNodeData>;
            }),
            ...[...productIds].map((id) => {
                const puid = id.replace("product:", "");
                const productNode =
                    productNodeByPuid.get(puid) ??
                    ({
                        productPuid: puid,
                        calculatedFlowRate: 0,
                        actualFlowRateIn: 0,
                        actualFlowRateOut: 0,
                        isExternal: false,
                    } as ProductNode);

                return {
                    id,
                    type: "productNode",
                    position: positions.get(id) ?? { x: 0, y: 0 },
                    data: {
                        puid,
                        productName: productNameByPuid.get(puid) ?? puid,
                        calculatedFlowRate: productNode.calculatedFlowRate,
                        actualFlowRateIn: productNode.actualFlowRateIn,
                        actualFlowRateOut: productNode.actualFlowRateOut,
                        isExternal: productNode.isExternal,
                        targetRate: targetByProduct.get(puid) ?? null,
                    } satisfies ProductFlowNodeData,
                } as FlowNode<FlowNodeData>;
            }),
        ];

        const edges: FlowEdge[] = chart.edges
            .map((edge, index) => {
                const source =
                    edge.producerNodePuid === null ||
                    edge.producerNodePuid === undefined
                        ? `product:${edge.productPuid}`
                        : `process:${edge.producerNodePuid}`;
                const target =
                    edge.consumerNodePuid === null ||
                    edge.consumerNodePuid === undefined
                        ? `product:${edge.productPuid}`
                        : `process:${edge.consumerNodePuid}`;

                if (source === target) return null;

                const ratio =
                    edge.calculatedFlowRate > 0
                        ? edge.actualFlowRate / edge.calculatedFlowRate
                        : 0;
                const edgeColor =
                    ratio >= 0.99
                        ? "#22c55e"
                        : ratio > 0
                          ? "#f59e0b"
                          : "#64748b";

                return {
                    id: `edge:${index}`,
                    source,
                    target,
                    type: "smoothstep",
                    animated: edge.actualFlowRate > 0,
                    style: { stroke: edgeColor, strokeWidth: 2 },
                    markerEnd: {
                        type: MarkerType.ArrowClosed,
                        color: edgeColor,
                    },
                    label: `${productNameByPuid.get(edge.productPuid) ?? edge.productPuid}  ${formatRate(edge.actualFlowRate)}/${formatRate(edge.calculatedFlowRate)} /s`,
                    labelStyle: {
                        fill: "#dbeafe",
                        fontSize: 11,
                        fontWeight: 500,
                    },
                    labelBgStyle: {
                        fill: "#0f172a",
                        fillOpacity: 0.8,
                    },
                } as FlowEdge;
            })
            .filter((edge): edge is FlowEdge => edge !== null);

        return { nodes, edges };
    }, [
        chart.nodes,
        chart.edges,
        chart.targets,
        chart.productNodes,
        chart.preferredRecipes,
        machineNameByPuid,
        productNameByPuid,
        productNodeByPuid,
        recipeNameByPuid,
        targetByProduct,
    ]);

    const anyMutationPending =
        refreshMutation.isPending ||
        saveTargetsMutation.isPending ||
        saveNodeMutation.isPending ||
        savePreferredRecipesMutation.isPending ||
        saveExternalMutation.isPending;

    const sortedProducts = useMemo(
        () => [...products].sort((a, b) => a.name.localeCompare(b.name)),
        [products],
    );

    const sortedRecipes = useMemo(
        () => [...recipes].sort((a, b) => a.name.localeCompare(b.name)),
        [recipes],
    );

    const sortedModifiers = useMemo(
        () => [...modifiers].sort((a, b) => a.name.localeCompare(b.name)),
        [modifiers],
    );

    const availableProductOptions = useMemo(
        () =>
            sortedProducts.filter((product) =>
                chart.productNodes.some(
                    (node) => node.productPuid === product.puid,
                ),
            ),
        [sortedProducts, chart.productNodes],
    );

    const onSaveNode = () => {
        if (!selectedProcessNode) return;
        const count = parseNonNegative(nodeActualMachineCount);
        if (count === null) {
            setInteractionError(
                "Actual machine count must be a non-negative number.",
            );
            return;
        }
        if (!nodeMachinePuid) {
            setInteractionError("Please select a machine.");
            return;
        }

        const existingModifierByPuid = new Map<string, Modifier>();
        for (const modifier of selectedProcessNode.modifiers) {
            existingModifierByPuid.set(modifier.puid, modifier);
        }

        const modifierPayload = nodeModifierPuids.map(
            (puid) =>
                existingModifierByPuid.get(puid) ?? {
                    puid,
                    attributes: [],
                },
        );

        saveNodeMutation.mutate({
            nodePuid: selectedProcessNode.puid,
            machinePuid: nodeMachinePuid,
            actualMachineCount: count,
            modifiers: modifierPayload,
            recipeAttributes: selectedProcessNode.recipeAttributes,
            machineAttributes: selectedProcessNode.machineAttributes,
        });
    };

    const onSaveTargets = () => {
        const used = new Set<string>();
        const normalized: Target[] = [];

        for (const draft of targetDrafts) {
            const productPuid = draft.productPuid.trim();
            const targetRate = parseNonNegative(draft.targetRate);

            if (!productPuid) {
                setInteractionError("Each target must select a product.");
                return;
            }
            if (used.has(productPuid)) {
                setInteractionError(
                    "Targets cannot contain duplicate products.",
                );
                return;
            }
            if (targetRate === null) {
                setInteractionError(
                    "Target rates must be non-negative numbers.",
                );
                return;
            }

            used.add(productPuid);
            normalized.push({ productPuid, targetRate });
        }

        saveTargetsMutation.mutate({ targets: normalized });
    };

    const onSavePreferredRecipes = () => {
        savePreferredRecipesMutation.mutate({
            recipePuids: preferredRecipePuids,
        });
    };

    const onSaveExternal = () => {
        if (!selectedProductNode) return;
        if (!externalIsEnabled) {
            saveExternalMutation.mutate({
                productPuid: selectedProductNode.productPuid,
                isExternal: false,
                externalRate: null,
            });
            return;
        }

        const rate = parseNonNegative(externalRate);
        if (rate === null) {
            setInteractionError("External rate must be a non-negative number.");
            return;
        }

        saveExternalMutation.mutate({
            productPuid: selectedProductNode.productPuid,
            isExternal: true,
            externalRate: rate,
        });
    };

    return (
        <ProjectPageLayout>
            <div className="flex flex-col gap-4">
                <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            Workflow Chart
                        </h1>
                        <div className="mt-1 text-sm text-slate-400">
                            {selectedWorkflow?.name?.trim() ||
                                selectedWorkflow?.puid ||
                                workflowRouteSegment}
                            {routeProjectName ? (
                                <span> • Project: {routeProjectName}</span>
                            ) : null}
                            {routeUsername ? (
                                <span> • Owner: {routeUsername}</span>
                            ) : null}
                        </div>
                    </div>

                    <div className="flex flex-wrap items-center gap-2">
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/70 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 disabled:cursor-not-allowed disabled:opacity-50"
                            onClick={() => {
                                setInteractionError(null);
                                void chartQuery.refetch();
                            }}
                            disabled={
                                !workflowId ||
                                chartQuery.isFetching ||
                                anyMutationPending
                            }
                        >
                            <IconRefresh size={16} />
                            Reload
                        </button>
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 disabled:cursor-not-allowed disabled:opacity-50"
                            onClick={() => {
                                setInteractionError(null);
                                refreshMutation.mutate();
                            }}
                            disabled={!workflowId || anyMutationPending}
                        >
                            <IconArrowsShuffle size={16} />
                            Rebuild Chart
                        </button>
                    </div>
                </div>

                <ProjectStatusGate>
                    {!isOwner ? (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                            Workflows can only be viewed by the project owner.
                        </div>
                    ) : (
                        <>
                            <ErrorDisplay errors={[interactionError]} />

                            {!workflowsQuery.isLoading &&
                                !selectedWorkflow &&
                                workflowRouteSegment && (
                                    <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                                        Workflow not found. It may have been
                                        renamed or deleted.
                                    </div>
                                )}

                            {(workflowsQuery.isLoading ||
                                chartQuery.isLoading) &&
                                workflowId && (
                                    <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                                        Loading workflow chart...
                                    </div>
                                )}

                            {!chartQuery.isLoading &&
                                chartQuery.error &&
                                workflowId && (
                                    <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                                        Failed to load workflow chart.{" "}
                                        {chartQuery.error.message}
                                    </div>
                                )}

                            {!chartQuery.isLoading &&
                                !chartQuery.error &&
                                workflowId && (
                                    <div className="grid grid-cols-1 gap-4 2xl:grid-cols-[minmax(0,1fr)_360px]">
                                        <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-2">
                                            <div className="h-[72dvh] min-h-[520px] w-full overflow-hidden rounded-lg border border-slate-800 bg-slate-950">
                                                <ReactFlow
                                                    nodes={flowData.nodes}
                                                    edges={flowData.edges}
                                                    nodeTypes={nodeTypes}
                                                    fitView
                                                    fitViewOptions={{
                                                        padding: 0.2,
                                                        maxZoom: 1,
                                                    }}
                                                    minZoom={0.2}
                                                    maxZoom={1.5}
                                                    onNodeClick={(_, node) => {
                                                        if (
                                                            node.id.startsWith(
                                                                "process:",
                                                            )
                                                        ) {
                                                            setSelection({
                                                                type: "process",
                                                                puid: node.id.replace(
                                                                    "process:",
                                                                    "",
                                                                ),
                                                            });
                                                        } else if (
                                                            node.id.startsWith(
                                                                "product:",
                                                            )
                                                        ) {
                                                            setSelection({
                                                                type: "product",
                                                                puid: node.id.replace(
                                                                    "product:",
                                                                    "",
                                                                ),
                                                            });
                                                        }
                                                    }}
                                                    onPaneClick={() =>
                                                        setSelection(null)
                                                    }
                                                    nodesDraggable={false}
                                                    nodesConnectable={false}
                                                    elementsSelectable
                                                    proOptions={{
                                                        hideAttribution: true,
                                                    }}
                                                >
                                                    <Background
                                                        gap={24}
                                                        size={1}
                                                        color="#334155"
                                                    />
                                                    <MiniMap
                                                        pannable
                                                        zoomable
                                                        className="!bg-slate-900"
                                                        nodeStrokeWidth={3}
                                                    />
                                                    <Controls className="!bg-slate-900" />
                                                </ReactFlow>
                                            </div>
                                        </div>

                                        <div className="flex flex-col gap-4">
                                            <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-4">
                                                <div className="flex items-center gap-2 text-sm font-semibold text-slate-100">
                                                    <IconTargetArrow
                                                        size={16}
                                                    />
                                                    Targets
                                                </div>
                                                <div className="mt-3 flex flex-col gap-2">
                                                    {targetDrafts.map(
                                                        (draft, index) => (
                                                            <div
                                                                key={`${draft.productPuid}-${index}`}
                                                                className="grid grid-cols-[1fr_110px_auto] gap-2"
                                                            >
                                                                <select
                                                                    className="rounded-lg border border-slate-700 bg-slate-900 px-2 py-2 text-xs text-slate-100 focus:border-purple-500/60 focus:outline-none"
                                                                    value={
                                                                        draft.productPuid
                                                                    }
                                                                    onChange={(
                                                                        event,
                                                                    ) => {
                                                                        setTargetDrafts(
                                                                            (
                                                                                prev,
                                                                            ) =>
                                                                                prev.map(
                                                                                    (
                                                                                        item,
                                                                                        itemIndex,
                                                                                    ) =>
                                                                                        itemIndex ===
                                                                                        index
                                                                                            ? {
                                                                                                  ...item,
                                                                                                  productPuid:
                                                                                                      event
                                                                                                          .target
                                                                                                          .value,
                                                                                              }
                                                                                            : item,
                                                                                ),
                                                                        );
                                                                    }}
                                                                    disabled={
                                                                        anyMutationPending
                                                                    }
                                                                >
                                                                    <option value="">
                                                                        Select
                                                                        product
                                                                    </option>
                                                                    {availableProductOptions.map(
                                                                        (
                                                                            product,
                                                                        ) => (
                                                                            <option
                                                                                key={
                                                                                    product.puid
                                                                                }
                                                                                value={
                                                                                    product.puid
                                                                                }
                                                                            >
                                                                                {
                                                                                    product.name
                                                                                }
                                                                            </option>
                                                                        ),
                                                                    )}
                                                                </select>
                                                                <input
                                                                    type="number"
                                                                    min="0"
                                                                    step="any"
                                                                    className="rounded-lg border border-slate-700 bg-slate-900 px-2 py-2 text-xs text-slate-100 focus:border-purple-500/60 focus:outline-none"
                                                                    value={
                                                                        draft.targetRate
                                                                    }
                                                                    onChange={(
                                                                        event,
                                                                    ) => {
                                                                        const value =
                                                                            event
                                                                                .target
                                                                                .value;
                                                                        setTargetDrafts(
                                                                            (
                                                                                prev,
                                                                            ) =>
                                                                                prev.map(
                                                                                    (
                                                                                        item,
                                                                                        itemIndex,
                                                                                    ) =>
                                                                                        itemIndex ===
                                                                                        index
                                                                                            ? {
                                                                                                  ...item,
                                                                                                  targetRate:
                                                                                                      value,
                                                                                              }
                                                                                            : item,
                                                                                ),
                                                                        );
                                                                    }}
                                                                    disabled={
                                                                        anyMutationPending
                                                                    }
                                                                />
                                                                <button
                                                                    type="button"
                                                                    className="rounded-lg border border-slate-700 px-2 text-xs text-slate-300 transition-colors cursor-pointer hover:border-red-500/50 hover:text-red-200"
                                                                    onClick={() => {
                                                                        setTargetDrafts(
                                                                            (
                                                                                prev,
                                                                            ) =>
                                                                                prev.filter(
                                                                                    (
                                                                                        _,
                                                                                        i,
                                                                                    ) =>
                                                                                        i !==
                                                                                        index,
                                                                                ),
                                                                        );
                                                                    }}
                                                                    disabled={
                                                                        anyMutationPending
                                                                    }
                                                                >
                                                                    Remove
                                                                </button>
                                                            </div>
                                                        ),
                                                    )}

                                                    <div className="flex gap-2 pt-1">
                                                        <button
                                                            type="button"
                                                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-xs text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60"
                                                            onClick={() => {
                                                                const used =
                                                                    new Set(
                                                                        targetDrafts.map(
                                                                            (
                                                                                item,
                                                                            ) =>
                                                                                item.productPuid,
                                                                        ),
                                                                    );
                                                                const next =
                                                                    availableProductOptions.find(
                                                                        (
                                                                            product,
                                                                        ) =>
                                                                            !used.has(
                                                                                product.puid,
                                                                            ),
                                                                    ) ??
                                                                    availableProductOptions[0];

                                                                setTargetDrafts(
                                                                    (prev) => [
                                                                        ...prev,
                                                                        {
                                                                            productPuid:
                                                                                next?.puid ??
                                                                                "",
                                                                            targetRate:
                                                                                "0",
                                                                        },
                                                                    ],
                                                                );
                                                            }}
                                                            disabled={
                                                                anyMutationPending ||
                                                                availableProductOptions.length ===
                                                                    0
                                                            }
                                                        >
                                                            Add Target
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="rounded-lg bg-purple-600/30 px-3 py-2 text-xs font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 disabled:cursor-not-allowed disabled:opacity-50"
                                                            onClick={
                                                                onSaveTargets
                                                            }
                                                            disabled={
                                                                anyMutationPending
                                                            }
                                                        >
                                                            Save Targets
                                                        </button>
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-4">
                                                <div className="flex items-center gap-2 text-sm font-semibold text-slate-100">
                                                    <IconChecklist size={16} />
                                                    Preferred Recipes
                                                </div>
                                                <div className="mt-3 max-h-40 space-y-1 overflow-y-auto pr-1">
                                                    {sortedRecipes.map(
                                                        (recipe) => {
                                                            const checked =
                                                                preferredRecipePuids.includes(
                                                                    recipe.puid,
                                                                );
                                                            return (
                                                                <label
                                                                    key={
                                                                        recipe.puid
                                                                    }
                                                                    className="flex items-center gap-2 rounded-md px-2 py-1 text-xs text-slate-200 hover:bg-slate-800/60"
                                                                >
                                                                    <input
                                                                        type="checkbox"
                                                                        checked={
                                                                            checked
                                                                        }
                                                                        onChange={(
                                                                            event,
                                                                        ) => {
                                                                            const isChecked =
                                                                                event
                                                                                    .target
                                                                                    .checked;
                                                                            setPreferredRecipePuids(
                                                                                (
                                                                                    prev,
                                                                                ) => {
                                                                                    if (
                                                                                        isChecked
                                                                                    ) {
                                                                                        if (
                                                                                            prev.includes(
                                                                                                recipe.puid,
                                                                                            )
                                                                                        ) {
                                                                                            return prev;
                                                                                        }
                                                                                        return [
                                                                                            ...prev,
                                                                                            recipe.puid,
                                                                                        ];
                                                                                    }
                                                                                    return prev.filter(
                                                                                        (
                                                                                            puid,
                                                                                        ) =>
                                                                                            puid !==
                                                                                            recipe.puid,
                                                                                    );
                                                                                },
                                                                            );
                                                                        }}
                                                                        disabled={
                                                                            anyMutationPending
                                                                        }
                                                                    />
                                                                    <span className="truncate">
                                                                        {
                                                                            recipe.name
                                                                        }
                                                                    </span>
                                                                </label>
                                                            );
                                                        },
                                                    )}
                                                </div>
                                                <button
                                                    type="button"
                                                    className="mt-3 w-full rounded-lg bg-purple-600/30 px-3 py-2 text-xs font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 disabled:cursor-not-allowed disabled:opacity-50"
                                                    onClick={
                                                        onSavePreferredRecipes
                                                    }
                                                    disabled={
                                                        anyMutationPending
                                                    }
                                                >
                                                    Save Preferred Recipes
                                                </button>
                                            </div>

                                            <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-4">
                                                {!selection && (
                                                    <div className="text-xs text-slate-400">
                                                        Select a process or
                                                        product node in the
                                                        chart to edit it.
                                                    </div>
                                                )}

                                                {selectedProcessNode && (
                                                    <div className="flex flex-col gap-3">
                                                        <div className="flex items-center gap-2 text-sm font-semibold text-slate-100">
                                                            <IconSettings
                                                                size={16}
                                                            />
                                                            Process Node
                                                        </div>
                                                        <div className="text-xs text-slate-400">
                                                            {recipeNameByPuid.get(
                                                                selectedProcessNode.recipePuid,
                                                            ) ??
                                                                selectedProcessNode.recipePuid}
                                                        </div>

                                                        <label className="flex flex-col gap-1 text-xs text-slate-300">
                                                            Machine
                                                            <select
                                                                className="rounded-lg border border-slate-700 bg-slate-900 px-2 py-2 text-xs text-slate-100 focus:border-purple-500/60 focus:outline-none"
                                                                value={
                                                                    nodeMachinePuid
                                                                }
                                                                onChange={(
                                                                    event,
                                                                ) =>
                                                                    setNodeMachinePuid(
                                                                        event
                                                                            .target
                                                                            .value,
                                                                    )
                                                                }
                                                                disabled={
                                                                    anyMutationPending
                                                                }
                                                            >
                                                                <option value="">
                                                                    Select
                                                                    machine
                                                                </option>
                                                                {compatibleMachines.map(
                                                                    (
                                                                        machine,
                                                                    ) => (
                                                                        <option
                                                                            key={
                                                                                machine.puid
                                                                            }
                                                                            value={
                                                                                machine.puid
                                                                            }
                                                                        >
                                                                            {
                                                                                machine.name
                                                                            }
                                                                        </option>
                                                                    ),
                                                                )}
                                                            </select>
                                                        </label>

                                                        <label className="flex flex-col gap-1 text-xs text-slate-300">
                                                            Actual Machine Count
                                                            <input
                                                                type="number"
                                                                min="0"
                                                                step="any"
                                                                className="rounded-lg border border-slate-700 bg-slate-900 px-2 py-2 text-xs text-slate-100 focus:border-purple-500/60 focus:outline-none"
                                                                value={
                                                                    nodeActualMachineCount
                                                                }
                                                                onChange={(
                                                                    event,
                                                                ) =>
                                                                    setNodeActualMachineCount(
                                                                        event
                                                                            .target
                                                                            .value,
                                                                    )
                                                                }
                                                                disabled={
                                                                    anyMutationPending
                                                                }
                                                            />
                                                        </label>

                                                        <div className="flex flex-col gap-1 text-xs text-slate-300">
                                                            Modifiers
                                                            <div className="max-h-32 space-y-1 overflow-y-auto rounded-lg border border-slate-800 bg-slate-950/40 p-2">
                                                                {sortedModifiers.length ===
                                                                    0 && (
                                                                    <div className="text-xs text-slate-500">
                                                                        No
                                                                        modifiers
                                                                        available.
                                                                    </div>
                                                                )}
                                                                {sortedModifiers.map(
                                                                    (
                                                                        modifier,
                                                                    ) => {
                                                                        const checked =
                                                                            nodeModifierPuids.includes(
                                                                                modifier.puid,
                                                                            );
                                                                        return (
                                                                            <label
                                                                                key={
                                                                                    modifier.puid
                                                                                }
                                                                                className="flex items-center gap-2 text-xs text-slate-200"
                                                                            >
                                                                                <input
                                                                                    type="checkbox"
                                                                                    checked={
                                                                                        checked
                                                                                    }
                                                                                    onChange={(
                                                                                        event,
                                                                                    ) => {
                                                                                        const isChecked =
                                                                                            event
                                                                                                .target
                                                                                                .checked;
                                                                                        setNodeModifierPuids(
                                                                                            (
                                                                                                prev,
                                                                                            ) => {
                                                                                                if (
                                                                                                    isChecked
                                                                                                ) {
                                                                                                    if (
                                                                                                        prev.includes(
                                                                                                            modifier.puid,
                                                                                                        )
                                                                                                    ) {
                                                                                                        return prev;
                                                                                                    }
                                                                                                    return [
                                                                                                        ...prev,
                                                                                                        modifier.puid,
                                                                                                    ];
                                                                                                }
                                                                                                return prev.filter(
                                                                                                    (
                                                                                                        puid,
                                                                                                    ) =>
                                                                                                        puid !==
                                                                                                        modifier.puid,
                                                                                                );
                                                                                            },
                                                                                        );
                                                                                    }}
                                                                                    disabled={
                                                                                        anyMutationPending
                                                                                    }
                                                                                />
                                                                                <span>
                                                                                    {
                                                                                        modifier.name
                                                                                    }
                                                                                </span>
                                                                            </label>
                                                                        );
                                                                    },
                                                                )}
                                                            </div>
                                                        </div>

                                                        <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-2 text-[11px] text-slate-400">
                                                            <div>
                                                                Recipe
                                                                attributes:{" "}
                                                                {
                                                                    selectedProcessNode
                                                                        .recipeAttributes
                                                                        .length
                                                                }
                                                            </div>
                                                            {selectedProcessNode.recipeAttributes.map(
                                                                (item) => {
                                                                    const attr =
                                                                        attributeByPuid.get(
                                                                            item.puid,
                                                                        );
                                                                    const unit =
                                                                        attr?.unit?.trim()
                                                                            ? ` ${attr.unit}`
                                                                            : "";
                                                                    return (
                                                                        <div
                                                                            key={`r-${item.puid}`}
                                                                            className="truncate"
                                                                        >
                                                                            {attr?.name ??
                                                                                item.puid}
                                                                            :{" "}
                                                                            {formatRate(
                                                                                item.rate,
                                                                            )}
                                                                            {
                                                                                unit
                                                                            }
                                                                        </div>
                                                                    );
                                                                },
                                                            )}
                                                            <div className="mt-2">
                                                                Machine
                                                                attributes:{" "}
                                                                {
                                                                    selectedProcessNode
                                                                        .machineAttributes
                                                                        .length
                                                                }
                                                            </div>
                                                            {selectedProcessNode.machineAttributes.map(
                                                                (item) => {
                                                                    const attr =
                                                                        attributeByPuid.get(
                                                                            item.puid,
                                                                        );
                                                                    const unit =
                                                                        attr?.unit?.trim()
                                                                            ? ` ${attr.unit}`
                                                                            : "";
                                                                    return (
                                                                        <div
                                                                            key={`m-${item.puid}`}
                                                                            className="truncate"
                                                                        >
                                                                            {attr?.name ??
                                                                                item.puid}
                                                                            :{" "}
                                                                            {formatRate(
                                                                                item.rate,
                                                                            )}
                                                                            {
                                                                                unit
                                                                            }
                                                                        </div>
                                                                    );
                                                                },
                                                            )}
                                                        </div>

                                                        <button
                                                            type="button"
                                                            className="rounded-lg bg-purple-600/30 px-3 py-2 text-xs font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 disabled:cursor-not-allowed disabled:opacity-50"
                                                            onClick={onSaveNode}
                                                            disabled={
                                                                anyMutationPending
                                                            }
                                                        >
                                                            Save Node
                                                        </button>
                                                    </div>
                                                )}

                                                {selectedProductNode && (
                                                    <div className="flex flex-col gap-3">
                                                        <div className="flex items-center gap-2 text-sm font-semibold text-slate-100">
                                                            <IconDatabaseImport
                                                                size={16}
                                                            />
                                                            Product Node
                                                        </div>
                                                        <div className="text-xs text-slate-400">
                                                            {productNameByPuid.get(
                                                                selectedProductNode.productPuid,
                                                            ) ??
                                                                selectedProductNode.productPuid}
                                                        </div>

                                                        <label className="flex items-center gap-2 text-xs text-slate-200">
                                                            <input
                                                                type="checkbox"
                                                                checked={
                                                                    externalIsEnabled
                                                                }
                                                                onChange={(
                                                                    event,
                                                                ) =>
                                                                    setExternalIsEnabled(
                                                                        event
                                                                            .target
                                                                            .checked,
                                                                    )
                                                                }
                                                                disabled={
                                                                    anyMutationPending
                                                                }
                                                            />
                                                            Mark as externally
                                                            supplied
                                                        </label>

                                                        <label className="flex flex-col gap-1 text-xs text-slate-300">
                                                            External Rate
                                                            <input
                                                                type="number"
                                                                min="0"
                                                                step="any"
                                                                className="rounded-lg border border-slate-700 bg-slate-900 px-2 py-2 text-xs text-slate-100 focus:border-purple-500/60 focus:outline-none"
                                                                value={
                                                                    externalRate
                                                                }
                                                                onChange={(
                                                                    event,
                                                                ) =>
                                                                    setExternalRate(
                                                                        event
                                                                            .target
                                                                            .value,
                                                                    )
                                                                }
                                                                disabled={
                                                                    anyMutationPending ||
                                                                    !externalIsEnabled
                                                                }
                                                            />
                                                        </label>

                                                        <button
                                                            type="button"
                                                            className="rounded-lg bg-purple-600/30 px-3 py-2 text-xs font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 disabled:cursor-not-allowed disabled:opacity-50"
                                                            onClick={
                                                                onSaveExternal
                                                            }
                                                            disabled={
                                                                anyMutationPending
                                                            }
                                                        >
                                                            Save Product
                                                            External State
                                                        </button>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                )}
                        </>
                    )}
                </ProjectStatusGate>
            </div>
        </ProjectPageLayout>
    );
}
