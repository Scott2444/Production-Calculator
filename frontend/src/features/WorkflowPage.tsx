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
    type ProductNode,
    type Target,
    type WorkflowChart,
} from "@/lib/workflowChart";
import { getLayoutedWorkflowElements } from "@/lib/workflowLayout";
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
    type NodeChange,
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
    IconRotateClockwise2,
    IconSettings,
    IconBolt,
    IconCpu,
    IconTools,
    IconTargetArrow,
    IconTrash,
} from "@tabler/icons-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouterState } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useState } from "react";
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
import { type Recipe } from "@/types/recipes";
import { type Machine } from "@/types/machines";
import { type Modifier } from "@/types/modifiers";
import { type AttributeSummary } from "@/types/attributes";
import "@xyflow/react/dist/style.css";

interface ProcessNodeData {
    [key: string]: unknown;
    puid: string;
    recipeName: string;
    machineName: string;
    demandMachineCount: number;
    supplyMachineCount: number;
    demandRecipeRate: number;
    supplyRecipeRate: number;
    modifierNames: string[];
    preferredRecipe: boolean;
    attributes: Array<{
        puid: string;
        name: string;
        unit: string | null;
        demand: number;
        supply: number;
    }>;
    incomingProductPuids: string[];
    outgoingProductPuids: string[];
}

interface ProductFlowNodeData {
    [key: string]: unknown;
    puid: string;
    handleProductPuid: string;
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
    return Number.isInteger(rounded) ? `${rounded}` : rounded.toFixed(2);
}

function asNonNegativeFinite(value: number | null | undefined): number {
    if (typeof value !== "number" || !Number.isFinite(value)) return 0;
    return Math.max(0, value);
}

function parseNonNegative(value: string): number | null {
    const trimmed = value.trim();
    if (!trimmed) return null;
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed) || parsed < 0) return null;
    return parsed;
}

function getIncomingHandleId(productPuid: string): string {
    return `in:${productPuid}`;
}

function getOutgoingHandleId(productPuid: string): string {
    return `out:${productPuid}`;
}

function getHandleTopOffset(index: number, total: number): string {
    if (total <= 1) return "50%";
    const step = 100 / (total + 1);
    return `${(index + 1) * step}%`;
}

function ProcessFlowNode({
    data,
    selected,
}: NodeProps<FlowNode<ProcessNodeData>>) {
    const incomingProducts = data.incomingProductPuids;
    const outgoingProducts = data.outgoingProductPuids;

    const isUnderSupplied =
        data.supplyMachineCount < data.demandMachineCount * 0.99;
    const isOverSupplied =
        data.supplyMachineCount > data.demandMachineCount * 1.01;

    const machineColor = isUnderSupplied
        ? "text-amber-400"
        : isOverSupplied
          ? "text-cyan-400"
          : "text-emerald-400";
    const rateColor =
        data.supplyRecipeRate < data.demandRecipeRate * 0.99
            ? "text-amber-400"
            : "text-emerald-400";

    return (
        <div
            className={`w-72 rounded-xl border p-4 shadow-lg transition-all ${
                selected
                    ? "border-purple-500/80 bg-slate-800 ring-2 ring-purple-500/20"
                    : "border-slate-700/80 bg-slate-900/95"
            }`}
        >
            {incomingProducts.map((productPuid, index) => (
                <Handle
                    key={`in-${productPuid}`}
                    id={getIncomingHandleId(productPuid)}
                    type="target"
                    position={Position.Left}
                    style={{
                        top: getHandleTopOffset(index, incomingProducts.length),
                    }}
                    className="h-3! w-3! border-2 border-slate-900 bg-purple-500"
                />
            ))}
            {outgoingProducts.map((productPuid, index) => (
                <Handle
                    key={`out-${productPuid}`}
                    id={getOutgoingHandleId(productPuid)}
                    type="source"
                    position={Position.Right}
                    style={{
                        top: getHandleTopOffset(index, outgoingProducts.length),
                    }}
                    className="h-3! w-3! border-2 border-slate-900 bg-purple-500"
                />
            ))}

            <div className="mb-3">
                <div className="truncate text-base font-bold text-slate-100">
                    {data.recipeName}
                </div>
                <div className="flex items-center gap-1.5 text-xs font-medium text-slate-400">
                    <IconCpu size={14} className="shrink-0" />
                    <span className="truncate">{data.machineName}</span>
                </div>
            </div>

            <div className="space-y-2 rounded-lg bg-slate-950/40 p-3">
                <div className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-2 text-slate-400 font-medium">
                        <IconTools size={14} className="shrink-0" />
                        <span>Machines</span>
                    </div>
                    <div className="font-mono">
                        <span className={machineColor}>
                            {formatRate(data.supplyMachineCount)}
                        </span>
                        <span className="mx-1 text-slate-600">/</span>
                        <span className="text-slate-300">
                            {formatRate(data.demandMachineCount)}
                        </span>
                        <span className="ml-1 text-[10px] text-slate-500">
                            &nbsp;&nbsp;&nbsp;&nbsp;
                        </span>
                    </div>
                </div>

                <div className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-2 text-slate-400 font-medium">
                        <IconRotateClockwise2 size={14} className="shrink-0" />
                        <span>Rate</span>
                    </div>
                    <div className="font-mono">
                        <span className={rateColor}>
                            {formatRate(data.supplyRecipeRate)}
                        </span>
                        <span className="mx-1 text-slate-600">/</span>
                        <span className="text-slate-300 font-medium">
                            {formatRate(data.demandRecipeRate)}
                        </span>
                        <span className="ml-1 text-[10px] text-slate-500">
                            r/s
                        </span>
                    </div>
                </div>
            </div>

            {data.attributes.length > 0 && (
                <div className="mt-3 space-y-2 border-t border-slate-800/60 pt-3">
                    <div className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
                        Attributes
                    </div>
                    <div className="space-y-1.5">
                        {data.attributes.map((attr) => {
                            const isAtTarget =
                                attr.supply >= attr.demand * 0.99;
                            const attrColor = isAtTarget
                                ? "text-slate-100"
                                : "text-amber-400/90";
                            const unit = attr.unit?.trim()
                                ? ` ${attr.unit}`
                                : "";

                            return (
                                <div
                                    key={attr.puid}
                                    className="flex items-center justify-between text-[11px]"
                                >
                                    <div className="flex items-center gap-2 text-slate-400 truncate pr-2">
                                        <IconBolt
                                            size={12}
                                            className="shrink-0 text-slate-500"
                                        />
                                        <span className="truncate">
                                            {attr.name}
                                        </span>
                                    </div>
                                    <div className="font-mono shrink-0">
                                        <span className={attrColor}>
                                            {formatRate(attr.supply)}
                                        </span>
                                        <span className="mx-1 text-slate-600">
                                            /
                                        </span>
                                        <span className="text-slate-400">
                                            {formatRate(attr.demand)}
                                        </span>
                                        <span className="ml-0.5 text-[9px] text-slate-600">
                                            {unit}
                                        </span>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}

            <div className="mt-3 border-t border-slate-800/60 pt-3">
                <div className="mb-2 flex items-center justify-between">
                    <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
                        Modifiers
                    </span>
                    {data.preferredRecipe && (
                        <span className="rounded bg-amber-500/10 px-1.5 py-0.5 text-[9px] font-bold text-amber-500 ring-1 ring-inset ring-amber-500/20">
                            PREFERRED
                        </span>
                    )}
                </div>
                {data.modifierNames.length === 0 ? (
                    <div className="text-[10px] italic text-slate-600">
                        No modifiers applied
                    </div>
                ) : (
                    <div className="flex flex-wrap gap-1">
                        {data.modifierNames.map((name) => (
                            <span
                                key={name}
                                className="rounded bg-slate-800/80 px-2 py-0.5 text-[10px] text-slate-300 ring-1 ring-slate-700/50"
                            >
                                {name}
                            </span>
                        ))}
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
    const demandRate = asNonNegativeFinite(data.calculatedFlowRate);
    const supplyInRate = asNonNegativeFinite(data.actualFlowRateIn);
    const supplyOutRate = asNonNegativeFinite(data.actualFlowRateOut);

    const getSupplyColor = (supplyRate: number): string => {
        if (demandRate <= 0) {
            return supplyRate > 0 ? "text-cyan-400" : "text-slate-300";
        }
        if (supplyRate < demandRate * 0.99) return "text-amber-400";
        if (supplyRate > demandRate * 1.01) return "text-cyan-400";
        return "text-emerald-400";
    };

    const supplyInColor = getSupplyColor(supplyInRate);
    const supplyOutColor = getSupplyColor(supplyOutRate);

    const netFlow = supplyInRate - supplyOutRate;
    const netTolerance = Math.max(demandRate * 0.01, 0.001);
    const netFlowColor =
        Math.abs(netFlow) <= netTolerance
            ? "text-emerald-400"
            : netFlow > 0
              ? "text-cyan-400"
              : "text-amber-400";

    return (
        <div
            className={`w-72 rounded-xl border p-4 shadow-lg transition-all ${
                selected
                    ? "border-cyan-500/80 bg-slate-800 ring-2 ring-cyan-500/20"
                    : "border-slate-700/80 bg-slate-900/95"
            }`}
        >
            <Handle
                id={getIncomingHandleId(data.handleProductPuid)}
                type="target"
                position={Position.Left}
                className="h-3! w-3! border-2 border-slate-900 bg-cyan-400"
            />
            <Handle
                id={getOutgoingHandleId(data.handleProductPuid)}
                type="source"
                position={Position.Right}
                className="h-3! w-3! border-2 border-slate-900 bg-cyan-400"
            />

            <div className="mb-3 flex items-start justify-between gap-2">
                <div className="min-w-0">
                    <div className="truncate text-base font-bold text-slate-100">
                        {data.productName}
                    </div>
                    <div className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
                        Product Flow
                    </div>
                </div>
                {data.isExternal && (
                    <span className="rounded px-1.5 py-0.5 text-[9px] font-bold ring-1 ring-inset bg-cyan-500/10 text-cyan-300 ring-cyan-400/30">
                        EXTERNAL
                    </span>
                )}
            </div>

            <div className="space-y-2 rounded-lg bg-slate-950/40 p-3">
                <div className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-2 text-slate-400 font-medium">
                        <IconTargetArrow size={14} className="shrink-0" />
                        <span>Demand (In = Out)</span>
                    </div>
                    <div className="font-mono text-slate-100">
                        {formatRate(demandRate)}
                        <span className="ml-1 text-[10px] text-slate-500">
                            /s
                        </span>
                    </div>
                </div>

                <div className="border-t border-slate-800/70 pt-2 space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                        <span className="text-slate-400">
                            {data.isExternal
                                ? "Supply In (External)"
                                : "Supply In"}
                        </span>
                        <div className="font-mono">
                            <span className={supplyInColor}>
                                {formatRate(supplyInRate)}
                            </span>
                            <span className="ml-1 text-[10px] text-slate-500">
                                /s
                            </span>
                        </div>
                    </div>
                    <div className="flex items-center justify-between text-xs">
                        <span className="text-slate-400">Supply Out</span>
                        <div className="font-mono">
                            <span className={supplyOutColor}>
                                {formatRate(supplyOutRate)}
                            </span>
                            <span className="ml-1 text-[10px] text-slate-500">
                                /s
                            </span>
                        </div>
                    </div>
                </div>
            </div>

            <div className="mt-3 flex items-center justify-between border-t border-slate-800/60 pt-3 text-[11px]">
                <span className="text-slate-500">Net Flow (In - Out)</span>
                <span className={`font-mono ${netFlowColor}`}>
                    {netFlow > 0 ? "+" : ""}
                    {formatRate(netFlow)}
                    <span className="ml-1 text-[10px] text-slate-500">/s</span>
                </span>
            </div>

            {data.targetRate !== null ? (
                <div className="mt-2 rounded-md border border-purple-600/50 bg-purple-700/20 px-2 py-1 text-[11px] text-purple-100">
                    Target demand: {formatRate(data.targetRate)}/s
                </div>
            ) : null}
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
        () => coerceItems<Recipe>(recipesQuery.data),
        [recipesQuery.data],
    );
    const machines = useMemo(
        () => coerceItems<Machine>(machinesQuery.data),
        [machinesQuery.data],
    );
    const modifiers = useMemo(
        () => coerceItems<Modifier>(modifiersQuery.data),
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
        () =>
            calculateWorkflowAttributes(
                chart.nodes,
                recipes,
                machines,
                modifiers,
            ),
        [chart.nodes, recipes, machines, modifiers],
    );

    const globalAttributeTotals = useMemo(() => {
        const totals = new Map<string, { demand: number; supply: number }>();

        for (const attributesByNode of nodeAttributeValues.values()) {
            for (const [
                attributeId,
                value,
            ] of attributesByNode.demand.entries()) {
                const existing = totals.get(attributeId) ?? {
                    demand: 0,
                    supply: 0,
                };
                totals.set(attributeId, {
                    demand: existing.demand + value,
                    supply: existing.supply,
                });
            }

            for (const [
                attributeId,
                value,
            ] of attributesByNode.supply.entries()) {
                const existing = totals.get(attributeId) ?? {
                    demand: 0,
                    supply: 0,
                };
                totals.set(attributeId, {
                    demand: existing.demand,
                    supply: existing.supply + value,
                });
            }
        }

        return [...totals.entries()]
            .map(([puid, value]) => {
                const attribute = attributeByPuid.get(puid);
                return {
                    puid,
                    name: attribute?.name ?? puid,
                    unit: attribute?.unit ?? null,
                    demand: value.demand,
                    supply: value.supply,
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

    const selectedNodeId = useMemo(() => {
        if (!selection) return null;
        return `${selection.type}:${selection.puid}`;
    }, [selection]);

    const selectedProcessNode = useMemo(() => {
        if (!selection || selection.type !== "process") return null;
        return chart.nodes.find((node) => node.puid === selection.puid) ?? null;
    }, [selection, chart.nodes]);

    const selectedProcessRecipe = useMemo(() => {
        if (!selectedProcessNode) return null;
        return (
            recipes.find(
                (recipe) => recipe.puid === selectedProcessNode.recipePuid,
            ) ?? null
        );
    }, [recipes, selectedProcessNode]);

    const selectedProcessMachine = useMemo(() => {
        if (!selectedProcessNode || !selectedProcessNode.machinePuid)
            return null;
        return (
            machines.find(
                (machine) => machine.puid === selectedProcessNode.machinePuid,
            ) ?? null
        );
    }, [machines, selectedProcessNode]);

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
        setNodeModifierPuids(selectedProcessNode.modifierPuids ?? []);
    }, [selectedProcessNode, compatibleMachines]);

    const [interactionError, setInteractionError] = useState<string | null>(
        null,
    );
    const [measuredNodeSizes, setMeasuredNodeSizes] = useState<
        Record<string, { width: number; height: number }>
    >({});

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
            modifierPuids: string[];
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
                    modifierPuids: payload.modifierPuids,
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

    const onFlowNodesChange = useCallback(
        (changes: NodeChange<FlowNode<FlowNodeData>>[]) => {
            setMeasuredNodeSizes((previous) => {
                let changed = false;
                let next = previous;

                for (const change of changes) {
                    if (change.type !== "dimensions") continue;
                    if (!change.dimensions) continue;

                    const { width, height } = change.dimensions;
                    if (
                        typeof width !== "number" ||
                        typeof height !== "number" ||
                        width <= 0 ||
                        height <= 0
                    ) {
                        continue;
                    }

                    const existing = next[change.id];
                    if (
                        existing &&
                        existing.width === width &&
                        existing.height === height
                    ) {
                        continue;
                    }

                    if (!changed) {
                        next = { ...previous };
                        changed = true;
                    }
                    next[change.id] = { width, height };
                }

                return changed ? next : previous;
            });
        },
        [],
    );

    const flowData = useMemo(() => {
        const productIds = new Set<string>();
        const incomingProductsByProcess = new Map<string, Set<string>>();
        const outgoingProductsByProcess = new Map<string, Set<string>>();

        for (const productNode of chart.productNodes) {
            productIds.add(`product:${productNode.productPuid}`);
        }

        for (const edge of chart.edges) {
            if (
                edge.producerNodePuid !== null &&
                edge.producerNodePuid !== undefined
            ) {
                const existing =
                    outgoingProductsByProcess.get(edge.producerNodePuid) ??
                    new Set<string>();
                existing.add(edge.productPuid);
                outgoingProductsByProcess.set(edge.producerNodePuid, existing);
            }

            if (
                edge.consumerNodePuid !== null &&
                edge.consumerNodePuid !== undefined
            ) {
                const existing =
                    incomingProductsByProcess.get(edge.consumerNodePuid) ??
                    new Set<string>();
                existing.add(edge.productPuid);
                incomingProductsByProcess.set(edge.consumerNodePuid, existing);
            }

            if (
                edge.producerNodePuid === null ||
                edge.producerNodePuid === undefined ||
                edge.consumerNodePuid === null ||
                edge.consumerNodePuid === undefined
            ) {
                productIds.add(`product:${edge.productPuid}`);
            }
        }

        const targetProductNodeIds = chart.targets.map(
            (target) => `product:${target.productPuid}`,
        );

        const sortProductPuids = (values: Iterable<string>): string[] =>
            [...new Set(values)].sort((a, b) => {
                const aName = productNameByPuid.get(a) ?? a;
                const bName = productNameByPuid.get(b) ?? b;
                return aName.localeCompare(bName, undefined, {
                    sensitivity: "base",
                });
            });

        const nodes: FlowNode<FlowNodeData>[] = [
            ...chart.nodes.map((node) => {
                const id = `process:${node.puid}`;
                const nodeAttributes = nodeAttributeValues.get(node.puid);
                const incomingProductPuids = sortProductPuids(
                    incomingProductsByProcess.get(node.puid) ?? [],
                );
                const outgoingProductPuids = sortProductPuids(
                    outgoingProductsByProcess.get(node.puid) ?? [],
                );
                const attributeIds = nodeAttributes
                    ? new Set<string>([
                          ...nodeAttributes.demand.keys(),
                          ...nodeAttributes.supply.keys(),
                      ])
                    : new Set<string>();

                const attributes = [...attributeIds]
                    .map((puid) => {
                        const attribute = attributeByPuid.get(puid);
                        return {
                            puid,
                            name: attribute?.name ?? puid,
                            unit: attribute?.unit ?? null,
                            demand: nodeAttributes?.demand.get(puid) ?? 0,
                            supply: nodeAttributes?.supply.get(puid) ?? 0,
                        };
                    })
                    .sort((a, b) => a.name.localeCompare(b.name));

                const modifierNames = node.modifierPuids
                    .map(
                        (modifierPuid) =>
                            modifierNameByPuid.get(modifierPuid) ??
                            modifierPuid,
                    )
                    .sort((a, b) =>
                        a.localeCompare(b, undefined, { sensitivity: "base" }),
                    );

                return {
                    id,
                    type: "processNode",
                    position: { x: 0, y: 0 },
                    data: {
                        puid: node.puid,
                        recipeName:
                            recipeNameByPuid.get(node.recipePuid) ??
                            node.recipePuid,
                        machineName: node.machinePuid
                            ? (machineNameByPuid.get(node.machinePuid) ??
                              node.machinePuid)
                            : "Unassigned",
                        demandMachineCount: asNonNegativeFinite(
                            node.calculatedMachineCount,
                        ),
                        supplyMachineCount: asNonNegativeFinite(
                            node.actualMachineCount,
                        ),
                        demandRecipeRate: asNonNegativeFinite(
                            node.calculatedTargetRate,
                        ),
                        supplyRecipeRate: asNonNegativeFinite(
                            node.calculatedActualRate,
                        ),
                        modifierNames,
                        preferredRecipe: chart.preferredRecipes.includes(
                            node.recipePuid,
                        ),
                        attributes,
                        incomingProductPuids,
                        outgoingProductPuids,
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
                    position: { x: 0, y: 0 },
                    data: {
                        puid,
                        handleProductPuid: puid,
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
                    sourceHandle: getOutgoingHandleId(edge.productPuid),
                    targetHandle: getIncomingHandleId(edge.productPuid),
                    type: "bezier",
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

        const attachedEdgeIds = new Set<string>();
        const connectedNodeIds = new Set<string>();
        if (selectedNodeId) {
            connectedNodeIds.add(selectedNodeId);

            for (const edge of edges) {
                if (
                    edge.source !== selectedNodeId &&
                    edge.target !== selectedNodeId
                ) {
                    continue;
                }
                attachedEdgeIds.add(edge.id);
                connectedNodeIds.add(edge.source);
                connectedNodeIds.add(edge.target);
            }
        }

        const measuredNodes: FlowNode<FlowNodeData>[] = nodes.map((node) => {
            const measured = measuredNodeSizes[node.id];
            const isSelectedNode = selectedNodeId === node.id;
            const isConnectedNode = selectedNodeId
                ? connectedNodeIds.has(node.id)
                : true;

            const baseNode = !selectedNodeId
                ? node
                : {
                      ...node,
                      selected: isSelectedNode,
                      zIndex: isSelectedNode ? 12 : isConnectedNode ? 8 : 1,
                      style: {
                          ...(node.style ?? {}),
                          opacity: isConnectedNode ? 1 : 0.28,
                          filter: isConnectedNode
                              ? "grayscale(0)"
                              : "grayscale(0.88)",
                      },
                  };

            if (!measured) return baseNode;

            if (
                baseNode.width === measured.width &&
                baseNode.height === measured.height
            ) {
                return baseNode;
            }

            return {
                ...baseNode,
                width: measured.width,
                height: measured.height,
            };
        });

        const styledEdges = !selectedNodeId
            ? edges
            : edges.map((edge) => {
                  const isAttached = attachedEdgeIds.has(edge.id);
                  const edgeColor = isAttached ? "#38bdf8" : "#475569";
                  const markerEnd =
                      edge.markerEnd && typeof edge.markerEnd === "object"
                          ? {
                                ...edge.markerEnd,
                                color: edgeColor,
                            }
                          : edge.markerEnd;

                  return {
                      ...edge,
                      animated: isAttached,
                      zIndex: isAttached ? 20 : 1,
                      style: {
                          ...(edge.style ?? {}),
                          stroke: edgeColor,
                          strokeWidth: isAttached ? 3.5 : 1.5,
                          opacity: isAttached ? 1 : 0.2,
                      },
                      markerEnd,
                      labelStyle: {
                          ...(edge.labelStyle ?? {}),
                          fill: isAttached ? "#e0f2fe" : "#94a3b8",
                          fontWeight: isAttached ? 600 : 500,
                          opacity: isAttached ? 1 : 0.35,
                      },
                      labelBgStyle: {
                          ...(edge.labelBgStyle ?? {}),
                          fill: isAttached ? "#082f49" : "#0f172a",
                          fillOpacity: isAttached ? 0.85 : 0.35,
                      },
                  } as FlowEdge;
              });

        return getLayoutedWorkflowElements(measuredNodes, styledEdges, {
            productNodeIds: [...productIds],
            targetProductNodeIds,
        });
    }, [
        chart.nodes,
        chart.edges,
        chart.targets,
        chart.productNodes,
        chart.preferredRecipes,
        nodeAttributeValues,
        attributeByPuid,
        modifierNameByPuid,
        machineNameByPuid,
        productNameByPuid,
        productNodeByPuid,
        recipeNameByPuid,
        measuredNodeSizes,
        selectedNodeId,
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

        saveNodeMutation.mutate({
            nodePuid: selectedProcessNode.puid,
            machinePuid: nodeMachinePuid,
            actualMachineCount: count,
            modifierPuids: nodeModifierPuids,
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
                                            onNodesChange={onFlowNodesChange}
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
                                                                ? "border-purple-500/70 bg-purple-700/85 text-purple-100"
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
                                                                ? "border-purple-500/70 bg-purple-700/85 text-purple-100"
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
                                                                ? "border-purple-500/70 bg-purple-700/85 text-purple-100"
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
                                                                {selectedProcessRecipe
                                                                    ?.attributes
                                                                    .length ??
                                                                    0}
                                                            </div>
                                                            {(
                                                                selectedProcessRecipe?.attributes ??
                                                                []
                                                            ).map((item) => {
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
                                                                        {unit}
                                                                    </div>
                                                                );
                                                            })}
                                                            <div className="mt-2">
                                                                Machine
                                                                attributes:{" "}
                                                                {selectedProcessMachine
                                                                    ?.attributes
                                                                    .length ??
                                                                    0}
                                                            </div>
                                                            {(
                                                                selectedProcessMachine?.attributes ??
                                                                []
                                                            ).map((item) => {
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
                                                                        {unit}
                                                                    </div>
                                                                );
                                                            })}
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

                                            <div className="pointer-events-auto absolute bottom-4 left-1/2 w-95 -translate-x-1/2 rounded-xl border border-slate-700/90 bg-slate-900/92 p-3 shadow-xl backdrop-blur">
                                                <div className="flex items-center justify-between gap-3">
                                                    <div className="text-xs font-semibold uppercase tracking-wide text-slate-200">
                                                        Attribute Totals
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
                                                            <div className="space-y-1.5">
                                                                {globalAttributeTotals.map(
                                                                    (
                                                                        attribute,
                                                                    ) => {
                                                                        const isAtTarget =
                                                                            attribute.supply >=
                                                                            attribute.demand *
                                                                                0.99;
                                                                        const attrColor =
                                                                            isAtTarget
                                                                                ? "text-slate-100"
                                                                                : "text-amber-400/90";
                                                                        const unit =
                                                                            attribute.unit?.trim()
                                                                                ? ` ${attribute.unit}`
                                                                                : "";
                                                                        return (
                                                                            <div
                                                                                key={
                                                                                    attribute.puid
                                                                                }
                                                                                className="flex items-center justify-between text-[11px]"
                                                                            >
                                                                                <div className="flex items-center gap-2 text-slate-400 truncate pr-2">
                                                                                    <IconBolt
                                                                                        size={
                                                                                            12
                                                                                        }
                                                                                        className="shrink-0 text-slate-500"
                                                                                    />
                                                                                    <span className="truncate">
                                                                                        {
                                                                                            attribute.name
                                                                                        }
                                                                                    </span>
                                                                                </div>
                                                                                <div className="font-mono shrink-0">
                                                                                    <span
                                                                                        className={
                                                                                            attrColor
                                                                                        }
                                                                                    >
                                                                                        {formatRate(
                                                                                            attribute.supply,
                                                                                        )}
                                                                                    </span>
                                                                                    <span className="mx-1 text-slate-600">
                                                                                        /
                                                                                    </span>
                                                                                    <span className="text-slate-400">
                                                                                        {formatRate(
                                                                                            attribute.demand,
                                                                                        )}
                                                                                    </span>
                                                                                    <span className="ml-0.5 text-[9px] text-slate-600">
                                                                                        {
                                                                                            unit
                                                                                        }
                                                                                    </span>
                                                                                </div>
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
