"use client";

import ErrorDisplay from "@/components/ErrorDisplay";
import DropDown from "@/components/DropDown";
import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import { useProject } from "@/context/ProjectContext";
import { useProtectedApi } from "@/lib/api";
import { type Workflow } from "@/lib/workflow";
import {
    setWorkflowProductExternal,
    updateWorkflowChart,
    updateWorkflowNode,
    updateWorkflowPreferredRecipes,
    updateWorkflowTargets,
    type AttributeRate,
    type WorkflowModifier,
    type ProductNode,
    type Target,
    type WorkflowChart,
} from "@/lib/workflowChart";
import { buildWorkflowLayout } from "@/lib/workflowLayout";
import { calculateWorkflowAttributes } from "@/lib/workflowAttributes";
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
    IconChevronDown,
    IconChevronUp,
    IconChecklist,
    IconDatabaseImport,
    IconRefresh,
    IconSettings,
    IconTargetArrow,
    IconTrash,
} from "@tabler/icons-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouterState } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";
import {
    useAttributesQuery,
    useMachinesQuery,
    useModifiersQuery,
    useProductsQuery,
    useRecipesQuery,
    useWorkflowChartQuery,
    useWorkflowsQuery,
} from "@/hooks/useQueries";
import { type ProductSummary } from "@/types/products";
import { type RecipeSummary } from "@/types/recipes";
import { type MachineSummary } from "@/types/machines";
import { type ModifierSummary } from "@/types/modifiers";
import { type AttributeSummary } from "@/types/attributes";
import "@xyflow/react/dist/style.css";

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
    attributes: Array<{
        puid: string;
        name: string;
        unit: string | null;
        value: number;
    }>;
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

type GlobalMenu = "targets" | "preferredRecipes" | "externalProducts" | null;

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
            className={`w-65 rounded-xl border p-3 shadow-sm ${
                selected
                    ? "border-purple-400 bg-slate-800"
                    : "border-slate-700 bg-slate-900"
            }`}
        >
            <Handle
                type="target"
                position={Position.Left}
                className="h-2! w-2! bg-slate-200"
            />
            <Handle
                type="source"
                position={Position.Right}
                className="h-2!w-2 bg-slate-200"
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
            <div className="mt-2 rounded-md border border-slate-700/70 bg-slate-950/60 p-2">
                <div className="text-[11px] font-medium uppercase tracking-wide text-slate-400">
                    Attributes
                </div>
                {data.attributes.length === 0 ? (
                    <div className="mt-1 text-[11px] text-slate-500">None</div>
                ) : (
                    <div className="mt-1 flex max-h-22.5 flex-col gap-1 overflow-y-auto pr-1 text-[11px] text-slate-300">
                        {data.attributes.map((attribute) => {
                            const unit = attribute.unit?.trim()
                                ? ` ${attribute.unit}`
                                : "";
                            return (
                                <div
                                    key={attribute.puid}
                                    className="flex items-center justify-between gap-2"
                                >
                                    <span className="truncate text-slate-300">
                                        {attribute.name}
                                    </span>
                                    <span className="shrink-0 text-slate-200">
                                        {formatRate(attribute.value)}
                                        {unit}
                                    </span>
                                </div>
                            );
                        })}
                    </div>
                )}
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
            className={`w-55 rounded-xl border p-3 shadow-sm ${
                selected
                    ? "border-cyan-400 bg-slate-800"
                    : "border-cyan-900/70 bg-slate-900"
            }`}
        >
            <Handle
                type="target"
                position={Position.Left}
                className="h-2 w-2 bg-cyan-200"
            />
            <Handle
                type="source"
                position={Position.Right}
                className="h-2 w-2 bg-cyan-200"
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

function uniqueTrimmedPuids(values: string[]): string[] {
    return Array.from(new Set(values.map((v) => v.trim()).filter(Boolean)));
}

export default function WorkflowPage() {
    const { routeProjectName, projectId, isOwner } = useProject();
    const protectedApi = useProtectedApi();
    const queryClient = useQueryClient();

    const pathname = useRouterState({
        select: (state) => state.location.pathname,
    });
    const workflowRouteSegment = useMemo(() => {
        const segments = getPathSegments(pathname);
        return segments[3] ?? "";
    }, [pathname]);

    const workflowsQuery = useWorkflowsQuery(projectId, { enabled: isOwner });

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

    const chartQuery = useWorkflowChartQuery(projectId, workflowId, {
        enabled: isOwner,
    });

    const productsQuery = useProductsQuery(projectId);

    const recipesQuery = useRecipesQuery(projectId);

    const machinesQuery = useMachinesQuery(projectId);

    const modifiersQuery = useModifiersQuery(projectId);

    const attributesQuery = useAttributesQuery(projectId);

    const chart = useMemo(
        () => coerceWorkflowChart(chartQuery.data),
        [chartQuery.data],
    );
    const products = useMemo(
        () => coerceItems<ProductSummary>(productsQuery.data),
        [productsQuery.data],
    );
    const recipes = useMemo(
        () => coerceItems<RecipeSummary>(recipesQuery.data),
        [recipesQuery.data],
    );
    const machines = useMemo(
        () => coerceItems<MachineSummary>(machinesQuery.data),
        [machinesQuery.data],
    );
    const modifiers = useMemo(
        () => coerceItems<ModifierSummary>(modifiersQuery.data),
        [modifiersQuery.data],
    );
    const attributes = useMemo(
        () => coerceItems<AttributeSummary>(attributesQuery.data),
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
        const map = new Map<string, AttributeSummary>();
        for (const item of attributes) map.set(item.puid, item);
        return map;
    }, [attributes]);

    const nodeAttributeValues = useMemo(
        () => calculateWorkflowAttributes(chart.nodes),
        [chart.nodes],
    );

    const globalAttributeTotals = useMemo(() => {
        const totals = new Map<string, number>();

        for (const attributesByNode of nodeAttributeValues.values()) {
            for (const [attributeId, value] of attributesByNode.entries()) {
                totals.set(attributeId, (totals.get(attributeId) ?? 0) + value);
            }
        }

        return [...totals.entries()]
            .map(([puid, value]) => {
                const attribute = attributeByPuid.get(puid);
                return {
                    puid,
                    name: attribute?.name ?? puid,
                    unit: attribute?.unit ?? null,
                    value,
                };
            })
            .sort((a, b) => a.name.localeCompare(b.name));
    }, [nodeAttributeValues, attributeByPuid]);

    const modifierNameByPuid = useMemo(() => {
        const map = new Map<string, string>();
        for (const item of modifiers) map.set(item.puid, item.name);
        return map;
    }, [modifiers]);

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

    const [externalProductDrafts, setExternalProductDrafts] = useState<
        Array<{ productPuid: string; externalRate: string }>
    >([]);

    useEffect(() => {
        setExternalProductDrafts(
            chart.productNodes
                .filter((node) => node.isExternal)
                .map((node) => ({
                    productPuid: node.productPuid,
                    externalRate: `${Math.max(0, node.actualFlowRateIn - node.actualFlowRateOut)}`,
                })),
        );
    }, [chart.productNodes]);

    const [activeGlobalMenu, setActiveGlobalMenu] = useState<GlobalMenu>(null);
    const [globalStatsCollapsed, setGlobalStatsCollapsed] = useState(false);

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
            modifiers: WorkflowModifier[];
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

    const saveExternalProductsMutation = useMutation({
        mutationFn: async (payload: {
            updates: Array<{
                productPuid: string;
                isExternal: boolean;
                externalRate: number | null;
            }>;
        }) => {
            if (!projectId || !workflowId)
                throw new Error("No workflow selected.");

            if (payload.updates.length === 0) {
                return chart;
            }

            let next = chart;
            for (const update of payload.updates) {
                const data = await setWorkflowProductExternal(
                    projectId,
                    workflowId,
                    update.productPuid,
                    protectedApi,
                    {
                        isExternal: update.isExternal,
                        externalRate: update.externalRate,
                    },
                );
                next = coerceWorkflowChart(data);
            }
            return next;
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
                const attributeValues = nodeAttributeValues.get(node.puid);
                const attributes = attributeValues
                    ? [...attributeValues.entries()]
                          .map(([puid, value]) => {
                              const attribute = attributeByPuid.get(puid);
                              return {
                                  puid,
                                  name: attribute?.name ?? puid,
                                  unit: attribute?.unit ?? null,
                                  value,
                              };
                          })
                          .sort((a, b) => a.name.localeCompare(b.name))
                    : [];

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
                        attributes,
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
        nodeAttributeValues,
        attributeByPuid,
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
        saveExternalProductsMutation.isPending;

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

        const productOptions = sortedProducts.map((product) => ({
            value: product.puid,
            label: product.name,
            searchText: product.name,
        }));

        return (
            <DropDown
                label={
                    <div className="min-w-0">
                        <div className="text-xs text-slate-200">
                            {selectedName ??
                                (effectiveDisabled
                                    ? "No products"
                                    : "Select product")}
                        </div>
                    </div>
                }
                align="left"
                disabled={effectiveDisabled}
                className="w-full"
                buttonClassName="rounded-lg px-2 py-2"
                matchTriggerWidth
                mode="single"
                options={productOptions}
                value={value}
                onSelect={onSelect}
                searchPlaceholder="Search products"
                searchAriaLabel="Search products"
                emptyFilteredText="No products match your search."
                emptyOptionsText="No products yet."
                checkIconSize={14}
                optionClassName="text-xs"
                optionTextClassName="text-xs"
                searchInputClassName="text-xs"
                menuClassName="min-w-[250px]"
            />
        );
    };

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

        const existingModifierByPuid = new Map<string, WorkflowModifier>();
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

    const onSaveExternalProducts = () => {
        const desired = new Map<string, number>();

        for (const draft of externalProductDrafts) {
            const productPuid = draft.productPuid.trim();
            const externalRate = parseNonNegative(draft.externalRate);

            if (!productPuid) {
                setInteractionError(
                    "Each externally supplied product row must select a product.",
                );
                return;
            }
            if (desired.has(productPuid)) {
                setInteractionError(
                    "Externally supplied products cannot contain duplicates.",
                );
                return;
            }
            if (externalRate === null) {
                setInteractionError(
                    "External rates must be non-negative numbers.",
                );
                return;
            }

            desired.set(productPuid, externalRate);
        }

        const currentlyExternal = new Set(
            chart.productNodes
                .filter((node) => node.isExternal)
                .map((node) => node.productPuid),
        );

        const updates: Array<{
            productPuid: string;
            isExternal: boolean;
            externalRate: number | null;
        }> = [];

        for (const [productPuid, externalRate] of desired.entries()) {
            updates.push({
                productPuid,
                isExternal: true,
                externalRate,
            });
            currentlyExternal.delete(productPuid);
        }

        for (const productPuid of currentlyExternal) {
            updates.push({
                productPuid,
                isExternal: false,
                externalRate: null,
            });
        }

        saveExternalProductsMutation.mutate({ updates });
    };

    return (
        <ProjectPageLayout padding={false}>
            <div className="flex h-full min-h-0 flex-col gap-3">
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
                                    <div className="relative h-full min-h-155 w-full overflow-hidden bg-slate-950">
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
                                                className="bg-slate-900!"
                                                nodeStrokeWidth={3}
                                            />
                                            <Controls className="bg-slate-900!" />
                                        </ReactFlow>

                                        <div className="pointer-events-none absolute inset-0 z-20">
                                            <div className="pointer-events-auto flex items-start justify-between gap-4 p-4">
                                                <div className="min-w-0 rounded-xl border border-slate-700/70 bg-slate-900/85 px-4 py-3 backdrop-blur">
                                                    <h1 className="truncate text-xl font-semibold text-slate-100">
                                                        {selectedWorkflow?.name?.trim() ||
                                                            selectedWorkflow?.puid ||
                                                            workflowRouteSegment}
                                                    </h1>
                                                    <div className="mt-1 truncate text-xs text-slate-300">
                                                        {routeProjectName ? (
                                                            <span>
                                                                Project:{" "}
                                                                {
                                                                    routeProjectName
                                                                }
                                                            </span>
                                                        ) : null}
                                                        <span>
                                                            {" "}
                                                            • Workflow Chart
                                                        </span>
                                                    </div>
                                                </div>

                                                <div className="flex flex-wrap items-center gap-2">
                                                    <button
                                                        type="button"
                                                        className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/85 px-4 py-2 text-sm font-medium text-slate-200 backdrop-blur transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/90 disabled:cursor-not-allowed disabled:opacity-50"
                                                        onClick={() => {
                                                            setInteractionError(
                                                                null,
                                                            );
                                                            void chartQuery.refetch();
                                                        }}
                                                        disabled={
                                                            !workflowId ||
                                                            chartQuery.isFetching ||
                                                            anyMutationPending
                                                        }
                                                    >
                                                        <IconRefresh
                                                            size={16}
                                                        />
                                                        Reload
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className="inline-flex items-center gap-2 rounded-lg bg-purple-600/40 px-4 py-2 text-sm font-medium text-purple-100 backdrop-blur transition-colors cursor-pointer hover:bg-purple-600/55 disabled:cursor-not-allowed disabled:opacity-50"
                                                        onClick={() => {
                                                            setInteractionError(
                                                                null,
                                                            );
                                                            refreshMutation.mutate();
                                                        }}
                                                        disabled={
                                                            !workflowId ||
                                                            anyMutationPending
                                                        }
                                                    >
                                                        <IconArrowsShuffle
                                                            size={16}
                                                        />
                                                        Rebuild Chart
                                                    </button>
                                                </div>
                                            </div>

                                            <div className="pointer-events-auto absolute left-4 top-24 flex max-h-[calc(100%-7rem)] items-start gap-3">
                                                <div className="flex flex-col gap-2">
                                                    <button
                                                        type="button"
                                                        className={`inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-xs font-medium transition-colors cursor-pointer ${
                                                            activeGlobalMenu ===
                                                            "targets"
                                                                ? "border-purple-500/70 bg-purple-700/35 text-purple-100"
                                                                : "border-slate-700 bg-slate-900/85 text-slate-200 hover:border-purple-500/60 hover:bg-slate-800/90"
                                                        }`}
                                                        onClick={() =>
                                                            setActiveGlobalMenu(
                                                                (prev) =>
                                                                    prev ===
                                                                    "targets"
                                                                        ? null
                                                                        : "targets",
                                                            )
                                                        }
                                                    >
                                                        <IconTargetArrow
                                                            size={14}
                                                        />
                                                        Targets
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className={`inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-xs font-medium transition-colors cursor-pointer ${
                                                            activeGlobalMenu ===
                                                            "preferredRecipes"
                                                                ? "border-purple-500/70 bg-purple-700/35 text-purple-100"
                                                                : "border-slate-700 bg-slate-900/85 text-slate-200 hover:border-purple-500/60 hover:bg-slate-800/90"
                                                        }`}
                                                        onClick={() =>
                                                            setActiveGlobalMenu(
                                                                (prev) =>
                                                                    prev ===
                                                                    "preferredRecipes"
                                                                        ? null
                                                                        : "preferredRecipes",
                                                            )
                                                        }
                                                    >
                                                        <IconChecklist
                                                            size={14}
                                                        />
                                                        Preferred Recipes
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className={`inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-xs font-medium transition-colors cursor-pointer ${
                                                            activeGlobalMenu ===
                                                            "externalProducts"
                                                                ? "border-purple-500/70 bg-purple-700/35 text-purple-100"
                                                                : "border-slate-700 bg-slate-900/85 text-slate-200 hover:border-purple-500/60 hover:bg-slate-800/90"
                                                        }`}
                                                        onClick={() =>
                                                            setActiveGlobalMenu(
                                                                (prev) =>
                                                                    prev ===
                                                                    "externalProducts"
                                                                        ? null
                                                                        : "externalProducts",
                                                            )
                                                        }
                                                    >
                                                        <IconDatabaseImport
                                                            size={14}
                                                        />
                                                        External Products
                                                    </button>
                                                </div>

                                                {activeGlobalMenu && (
                                                    <div className="max-h-[calc(100%-1rem)] w-95 overflow-y-auto rounded-xl border border-slate-700 bg-slate-900/92 p-4 shadow-xl backdrop-blur">
                                                        {activeGlobalMenu ===
                                                            "targets" && (
                                                            <div className="flex flex-col gap-3">
                                                                <div className="text-sm font-semibold text-slate-100">
                                                                    Targets
                                                                </div>
                                                                {targetDrafts.map(
                                                                    (
                                                                        draft,
                                                                        index,
                                                                    ) => (
                                                                        <div
                                                                            key={`${draft.productPuid}-${index}`}
                                                                            className="grid grid-cols-[1fr_110px_auto] gap-2"
                                                                        >
                                                                            <ProductDropDown
                                                                                value={
                                                                                    draft.productPuid
                                                                                }
                                                                                onSelect={(
                                                                                    next,
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
                                                                                                                  next,
                                                                                                          }
                                                                                                        : item,
                                                                                            ),
                                                                                    );
                                                                                }}
                                                                                disabled={
                                                                                    anyMutationPending
                                                                                }
                                                                            />
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
                                                                                <IconTrash
                                                                                    size={
                                                                                        14
                                                                                    }
                                                                                />
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
                                                                                sortedProducts.find(
                                                                                    (
                                                                                        product,
                                                                                    ) =>
                                                                                        !used.has(
                                                                                            product.puid,
                                                                                        ),
                                                                                ) ??
                                                                                sortedProducts[0];

                                                                            setTargetDrafts(
                                                                                (
                                                                                    prev,
                                                                                ) => [
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
                                                                            sortedProducts.length ===
                                                                                0
                                                                        }
                                                                    >
                                                                        Add
                                                                        Target
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
                                                                        Save
                                                                    </button>
                                                                </div>
                                                            </div>
                                                        )}

                                                        {activeGlobalMenu ===
                                                            "preferredRecipes" && (
                                                            <div className="flex flex-col gap-3">
                                                                <div className="text-sm font-semibold text-slate-100">
                                                                    Preferred
                                                                    Recipes
                                                                </div>
                                                                <DropDown
                                                                    label={
                                                                        <div className="truncate text-xs text-slate-200">
                                                                            {preferredRecipePuids.length ===
                                                                            0
                                                                                ? "Select preferred recipes"
                                                                                : preferredRecipePuids.length ===
                                                                                    1
                                                                                  ? (recipeNameByPuid.get(
                                                                                        preferredRecipePuids[0],
                                                                                    ) ??
                                                                                    preferredRecipePuids[0])
                                                                                  : `${preferredRecipePuids.length} recipes selected`}
                                                                        </div>
                                                                    }
                                                                    mode="multi"
                                                                    options={sortedRecipes.map(
                                                                        (
                                                                            r,
                                                                        ) => ({
                                                                            value: r.puid,
                                                                            label: r.name,
                                                                            searchText:
                                                                                r.name,
                                                                        }),
                                                                    )}
                                                                    values={
                                                                        preferredRecipePuids
                                                                    }
                                                                    onChangeValues={(
                                                                        next,
                                                                    ) =>
                                                                        setPreferredRecipePuids(
                                                                            uniqueTrimmedPuids(
                                                                                next,
                                                                            ),
                                                                        )
                                                                    }
                                                                    disabled={
                                                                        anyMutationPending
                                                                    }
                                                                    matchTriggerWidth
                                                                    className="w-full"
                                                                    buttonClassName="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-left"
                                                                    menuClassName="min-w-[250px]"
                                                                />

                                                                {preferredRecipePuids.length >
                                                                    0 && (
                                                                    <div className="flex max-h-52 flex-col gap-2 overflow-y-auto pr-1">
                                                                        {preferredRecipePuids
                                                                            .slice()
                                                                            .sort(
                                                                                (
                                                                                    a,
                                                                                    b,
                                                                                ) => {
                                                                                    const an =
                                                                                        recipeNameByPuid.get(
                                                                                            a,
                                                                                        ) ??
                                                                                        a;
                                                                                    const bn =
                                                                                        recipeNameByPuid.get(
                                                                                            b,
                                                                                        ) ??
                                                                                        b;
                                                                                    return an.localeCompare(
                                                                                        bn,
                                                                                        undefined,
                                                                                        {
                                                                                            sensitivity:
                                                                                                "base",
                                                                                        },
                                                                                    );
                                                                                },
                                                                            )
                                                                            .map(
                                                                                (
                                                                                    puid,
                                                                                ) => (
                                                                                    <div
                                                                                        key={
                                                                                            puid
                                                                                        }
                                                                                        className="flex items-center justify-between gap-3 rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-1.5"
                                                                                    >
                                                                                        <div className="min-w-0 truncate text-[11px] text-slate-300">
                                                                                            {recipeNameByPuid.get(
                                                                                                puid,
                                                                                            ) ??
                                                                                                puid}
                                                                                        </div>
                                                                                        <button
                                                                                            type="button"
                                                                                            className="shrink-0 rounded-md border border-slate-700 bg-slate-900/60 p-1 text-slate-400 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100"
                                                                                            onClick={() =>
                                                                                                setPreferredRecipePuids(
                                                                                                    (
                                                                                                        prev,
                                                                                                    ) =>
                                                                                                        prev.filter(
                                                                                                            (
                                                                                                                p,
                                                                                                            ) =>
                                                                                                                p !==
                                                                                                                puid,
                                                                                                        ),
                                                                                                )
                                                                                            }
                                                                                            disabled={
                                                                                                anyMutationPending
                                                                                            }
                                                                                        >
                                                                                            <IconTrash
                                                                                                size={
                                                                                                    12
                                                                                                }
                                                                                            />
                                                                                        </button>
                                                                                    </div>
                                                                                ),
                                                                            )}
                                                                    </div>
                                                                )}

                                                                <button
                                                                    type="button"
                                                                    className="rounded-lg bg-purple-600/30 px-3 py-2 text-xs font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 disabled:cursor-not-allowed disabled:opacity-50"
                                                                    onClick={
                                                                        onSavePreferredRecipes
                                                                    }
                                                                    disabled={
                                                                        anyMutationPending
                                                                    }
                                                                >
                                                                    Save
                                                                </button>
                                                            </div>
                                                        )}

                                                        {activeGlobalMenu ===
                                                            "externalProducts" && (
                                                            <div className="flex flex-col gap-3">
                                                                <div className="text-sm font-semibold text-slate-100">
                                                                    Externally
                                                                    Supplied
                                                                    Products
                                                                </div>
                                                                {externalProductDrafts.map(
                                                                    (
                                                                        draft,
                                                                        index,
                                                                    ) => (
                                                                        <div
                                                                            key={`${draft.productPuid}-${index}`}
                                                                            className="grid grid-cols-[1fr_110px_auto] gap-2"
                                                                        >
                                                                            <ProductDropDown
                                                                                value={
                                                                                    draft.productPuid
                                                                                }
                                                                                onSelect={(
                                                                                    next,
                                                                                ) => {
                                                                                    setExternalProductDrafts(
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
                                                                                                                  next,
                                                                                                          }
                                                                                                        : item,
                                                                                            ),
                                                                                    );
                                                                                }}
                                                                                disabled={
                                                                                    anyMutationPending
                                                                                }
                                                                            />
                                                                            <input
                                                                                type="number"
                                                                                min="0"
                                                                                step="any"
                                                                                className="rounded-lg border border-slate-700 bg-slate-900 px-2 py-2 text-xs text-slate-100 focus:border-purple-500/60 focus:outline-none"
                                                                                value={
                                                                                    draft.externalRate
                                                                                }
                                                                                onChange={(
                                                                                    event,
                                                                                ) => {
                                                                                    const value =
                                                                                        event
                                                                                            .target
                                                                                            .value;
                                                                                    setExternalProductDrafts(
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
                                                                                                              externalRate:
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
                                                                                    setExternalProductDrafts(
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
                                                                                <IconTrash
                                                                                    size={
                                                                                        14
                                                                                    }
                                                                                />
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
                                                                                    externalProductDrafts.map(
                                                                                        (
                                                                                            item,
                                                                                        ) =>
                                                                                            item.productPuid,
                                                                                    ),
                                                                                );
                                                                            const next =
                                                                                sortedProducts.find(
                                                                                    (
                                                                                        product,
                                                                                    ) =>
                                                                                        !used.has(
                                                                                            product.puid,
                                                                                        ),
                                                                                ) ??
                                                                                sortedProducts[0];

                                                                            setExternalProductDrafts(
                                                                                (
                                                                                    prev,
                                                                                ) => [
                                                                                    ...prev,
                                                                                    {
                                                                                        productPuid:
                                                                                            next?.puid ??
                                                                                            "",
                                                                                        externalRate:
                                                                                            "0",
                                                                                    },
                                                                                ],
                                                                            );
                                                                        }}
                                                                        disabled={
                                                                            anyMutationPending ||
                                                                            sortedProducts.length ===
                                                                                0
                                                                        }
                                                                    >
                                                                        Add
                                                                        Product
                                                                    </button>
                                                                    <button
                                                                        type="button"
                                                                        className="rounded-lg bg-purple-600/30 px-3 py-2 text-xs font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 disabled:cursor-not-allowed disabled:opacity-50"
                                                                        onClick={
                                                                            onSaveExternalProducts
                                                                        }
                                                                        disabled={
                                                                            anyMutationPending
                                                                        }
                                                                    >
                                                                        Save
                                                                    </button>
                                                                </div>
                                                            </div>
                                                        )}
                                                    </div>
                                                )}
                                            </div>

                                            {selectedProcessNode && (
                                                <div className="pointer-events-auto absolute right-4 top-24 max-h-[calc(100%-7rem)] w-97.5 overflow-y-auto rounded-xl border border-slate-700 bg-slate-900/92 p-4 shadow-xl backdrop-blur">
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

                                                        <div className="flex flex-col gap-1 text-xs text-slate-300">
                                                            Machine
                                                            <DropDown
                                                                label={
                                                                    <div className="truncate text-xs text-slate-200">
                                                                        {nodeMachinePuid
                                                                            ? (machineNameByPuid.get(
                                                                                  nodeMachinePuid,
                                                                              ) ??
                                                                              nodeMachinePuid)
                                                                            : "Select machine"}
                                                                    </div>
                                                                }
                                                                mode="single"
                                                                options={compatibleMachines.map(
                                                                    (m) => ({
                                                                        value: m.puid,
                                                                        label: m.name,
                                                                        searchText:
                                                                            m.name,
                                                                    }),
                                                                )}
                                                                value={
                                                                    nodeMachinePuid
                                                                }
                                                                onSelect={
                                                                    setNodeMachinePuid
                                                                }
                                                                disabled={
                                                                    anyMutationPending
                                                                }
                                                                matchTriggerWidth
                                                                className="w-full"
                                                                buttonClassName="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-left"
                                                            />
                                                        </div>

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
                                                            <div className="flex flex-col gap-2">
                                                                <DropDown
                                                                    label={
                                                                        <div className="truncate text-xs text-slate-200">
                                                                            {nodeModifierPuids.length ===
                                                                            0
                                                                                ? "Select modifiers"
                                                                                : nodeModifierPuids.length ===
                                                                                    1
                                                                                  ? (modifierNameByPuid.get(
                                                                                        nodeModifierPuids[0],
                                                                                    ) ??
                                                                                    nodeModifierPuids[0])
                                                                                  : `${nodeModifierPuids.length} modifiers selected`}
                                                                        </div>
                                                                    }
                                                                    mode="multi"
                                                                    options={sortedModifiers.map(
                                                                        (
                                                                            m,
                                                                        ) => ({
                                                                            value: m.puid,
                                                                            label: m.name,
                                                                            searchText:
                                                                                m.name,
                                                                        }),
                                                                    )}
                                                                    values={
                                                                        nodeModifierPuids
                                                                    }
                                                                    onChangeValues={(
                                                                        next,
                                                                    ) =>
                                                                        setNodeModifierPuids(
                                                                            uniqueTrimmedPuids(
                                                                                next,
                                                                            ),
                                                                        )
                                                                    }
                                                                    disabled={
                                                                        anyMutationPending
                                                                    }
                                                                    matchTriggerWidth
                                                                    className="w-full"
                                                                    buttonClassName="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-left"
                                                                    menuClassName="min-w-[250px]"
                                                                />

                                                                {nodeModifierPuids.length >
                                                                    0 && (
                                                                    <div className="flex flex-col gap-2">
                                                                        {nodeModifierPuids
                                                                            .slice()
                                                                            .sort(
                                                                                (
                                                                                    a,
                                                                                    b,
                                                                                ) => {
                                                                                    const an =
                                                                                        modifierNameByPuid.get(
                                                                                            a,
                                                                                        ) ??
                                                                                        a;
                                                                                    const bn =
                                                                                        modifierNameByPuid.get(
                                                                                            b,
                                                                                        ) ??
                                                                                        b;
                                                                                    return an.localeCompare(
                                                                                        bn,
                                                                                        undefined,
                                                                                        {
                                                                                            sensitivity:
                                                                                                "base",
                                                                                        },
                                                                                    );
                                                                                },
                                                                            )
                                                                            .map(
                                                                                (
                                                                                    puid,
                                                                                ) => (
                                                                                    <div
                                                                                        key={
                                                                                            puid
                                                                                        }
                                                                                        className="flex items-center justify-between gap-3 rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-1.5"
                                                                                    >
                                                                                        <div className="min-w-0 truncate text-[11px] text-slate-300">
                                                                                            {modifierNameByPuid.get(
                                                                                                puid,
                                                                                            ) ??
                                                                                                puid}
                                                                                        </div>
                                                                                        <button
                                                                                            type="button"
                                                                                            className="shrink-0 rounded-md border border-slate-700 bg-slate-900/60 p-1 text-slate-400 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100"
                                                                                            onClick={() =>
                                                                                                setNodeModifierPuids(
                                                                                                    (
                                                                                                        prev,
                                                                                                    ) =>
                                                                                                        prev.filter(
                                                                                                            (
                                                                                                                p,
                                                                                                            ) =>
                                                                                                                p !==
                                                                                                                puid,
                                                                                                        ),
                                                                                                )
                                                                                            }
                                                                                            disabled={
                                                                                                anyMutationPending
                                                                                            }
                                                                                        >
                                                                                            <IconTrash
                                                                                                size={
                                                                                                    12
                                                                                                }
                                                                                            />
                                                                                        </button>
                                                                                    </div>
                                                                                ),
                                                                            )}
                                                                    </div>
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
                                                </div>
                                            )}

                                            {selection?.type === "product" &&
                                                !selectedProcessNode && (
                                                    <div className="pointer-events-none absolute right-4 top-24 rounded-xl border border-slate-700/80 bg-slate-900/80 px-3 py-2 text-xs text-slate-300 backdrop-blur">
                                                        Product node selected.
                                                        Use External Products to
                                                        edit global supply.
                                                    </div>
                                                )}

                                            <div className="pointer-events-auto absolute bottom-4 left-1/2 w-95 -translate-x-1/2 rounded-xl border border-slate-700/90 bg-slate-900/92 p-3 shadow-xl backdrop-blur">
                                                <div className="flex items-center justify-between gap-3">
                                                    <div className="text-xs font-semibold uppercase tracking-wide text-slate-200">
                                                        Global Attribute Totals
                                                    </div>
                                                    <button
                                                        type="button"
                                                        className="inline-flex items-center gap-1 rounded-md border border-slate-700 bg-slate-900/70 px-2 py-1 text-[11px] text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/90"
                                                        onClick={() =>
                                                            setGlobalStatsCollapsed(
                                                                (prev) => !prev,
                                                            )
                                                        }
                                                    >
                                                        {globalStatsCollapsed ? (
                                                            <>
                                                                <IconChevronUp
                                                                    size={12}
                                                                />
                                                                Expand
                                                            </>
                                                        ) : (
                                                            <>
                                                                <IconChevronDown
                                                                    size={12}
                                                                />
                                                                Collapse
                                                            </>
                                                        )}
                                                    </button>
                                                </div>

                                                {!globalStatsCollapsed && (
                                                    <div className="mt-2 max-h-44 overflow-y-auto rounded-lg border border-slate-800 bg-slate-950/50 p-2">
                                                        {globalAttributeTotals.length ===
                                                        0 ? (
                                                            <div className="text-xs text-slate-500">
                                                                No attribute
                                                                totals yet.
                                                            </div>
                                                        ) : (
                                                            <div className="grid gap-1 text-xs text-slate-300">
                                                                {globalAttributeTotals.map(
                                                                    (
                                                                        attribute,
                                                                    ) => {
                                                                        const unit =
                                                                            attribute.unit?.trim()
                                                                                ? ` ${attribute.unit}`
                                                                                : "";
                                                                        return (
                                                                            <div
                                                                                key={
                                                                                    attribute.puid
                                                                                }
                                                                                className="flex items-center justify-between gap-2"
                                                                            >
                                                                                <span className="truncate text-slate-300">
                                                                                    {
                                                                                        attribute.name
                                                                                    }
                                                                                </span>
                                                                                <span className="shrink-0 text-slate-100">
                                                                                    {formatRate(
                                                                                        attribute.value,
                                                                                    )}
                                                                                    {
                                                                                        unit
                                                                                    }
                                                                                </span>
                                                                            </div>
                                                                        );
                                                                    },
                                                                )}
                                                            </div>
                                                        )}
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
