import dagre from "dagre";
import type { Edge, Node } from "@xyflow/react";

export interface WorkflowLayoutOptions {
    horizontalSpacing?: number;
    verticalSpacing?: number;
    marginX?: number;
    marginY?: number;
    ranker?: "network-simplex" | "tight-tree" | "longest-path";
    processNodeWidth?: number;
    processNodeHeight?: number;
    productNodeWidth?: number;
    productNodeHeight?: number;
    productNodeIds?: string[];
    targetProductNodeIds?: string[];
}

const DEFAULT_HORIZONTAL_SPACING = 180;
const DEFAULT_VERTICAL_SPACING = 120;
const DEFAULT_MARGIN_X = 20;
const DEFAULT_MARGIN_Y = 20;
const DEFAULT_NODE_WIDTH = 300;
const DEFAULT_NODE_HEIGHT = 260;
const DEFAULT_PRODUCT_NODE_WIDTH = 260;
const DEFAULT_PRODUCT_NODE_HEIGHT = 100;
const TARGET_EDGE_WEIGHT = 20;
const TARGET_SINK_NODE_ID = "__workflow_target_sink__";

function toSortedArray(values: Iterable<string>): string[] {
    return [...values].sort((a, b) =>
        a.localeCompare(b, undefined, { sensitivity: "base" }),
    );
}

function makeTargetSinkNodeId(nodeIds: Set<string>): string {
    if (!nodeIds.has(TARGET_SINK_NODE_ID)) return TARGET_SINK_NODE_ID;
    let index = 1;
    while (nodeIds.has(`${TARGET_SINK_NODE_ID}_${index}`)) {
        index += 1;
    }
    return `${TARGET_SINK_NODE_ID}_${index}`;
}

/**
 * Computes deterministic node positions using Dagre for React Flow elements.
 * Layout is horizontal (left-to-right) to match workflow chart expectations.
 */
export function getLayoutedWorkflowElements<
    TNode extends Node,
    TEdge extends Edge & { weight?: number; minLen?: number },
>(
    nodes: TNode[],
    edges: TEdge[],
    options: WorkflowLayoutOptions = {},
): { nodes: TNode[]; edges: TEdge[] } {
    const horizontalSpacing =
        options.horizontalSpacing ?? DEFAULT_HORIZONTAL_SPACING;
    const verticalSpacing = options.verticalSpacing ?? DEFAULT_VERTICAL_SPACING;
    const marginX = options.marginX ?? DEFAULT_MARGIN_X;
    const marginY = options.marginY ?? DEFAULT_MARGIN_Y;

    const processNodeWidth = options.processNodeWidth ?? DEFAULT_NODE_WIDTH;
    const processNodeHeight = options.processNodeHeight ?? DEFAULT_NODE_HEIGHT;
    const productNodeWidth =
        options.productNodeWidth ?? DEFAULT_PRODUCT_NODE_WIDTH;
    const productNodeHeight =
        options.productNodeHeight ?? DEFAULT_PRODUCT_NODE_HEIGHT;

    const nodeById = new Map<string, TNode>();
    for (const node of nodes) {
        nodeById.set(node.id, node);
    }

    const nodeIds = toSortedArray(nodeById.keys());
    const nodeIdSet = new Set(nodeIds);

    const productNodeIds = new Set<string>();
    for (const node of nodes) {
        if (node.type === "productNode" || node.id.startsWith("product:")) {
            productNodeIds.add(node.id);
        }
    }
    for (const productNodeId of options.productNodeIds ?? []) {
        if (nodeIdSet.has(productNodeId)) productNodeIds.add(productNodeId);
    }

    const dagreGraph = new dagre.graphlib.Graph({ multigraph: false });
    dagreGraph.setGraph({
        rankdir: "LR",
        ranksep: horizontalSpacing,
        nodesep: verticalSpacing,
        marginx: marginX,
        marginy: marginY,
        ranker: options.ranker ?? "network-simplex",
        acyclicer: "greedy",
    });
    dagreGraph.setDefaultEdgeLabel(() => ({}));

    for (const nodeId of nodeIds) {
        const node = nodeById.get(nodeId);
        const isProduct = productNodeIds.has(nodeId);
        const width =
            node?.width ?? (isProduct ? productNodeWidth : processNodeWidth);
        const height =
            node?.height ?? (isProduct ? productNodeHeight : processNodeHeight);
        dagreGraph.setNode(nodeId, { width, height });
    }

    const insertedEdges = new Set<string>();
    const sortedEdges = [...edges].sort((left, right) => {
        const sourceSort = left.source.localeCompare(right.source, undefined, {
            sensitivity: "base",
        });
        if (sourceSort !== 0) return sourceSort;
        return left.target.localeCompare(right.target, undefined, {
            sensitivity: "base",
        });
    });

    for (const edge of sortedEdges) {
        if (!nodeIdSet.has(edge.source) || !nodeIdSet.has(edge.target))
            continue;
        if (edge.source === edge.target) continue;
        const edgeKey = `${edge.source}=>${edge.target}`;
        if (insertedEdges.has(edgeKey)) continue;
        insertedEdges.add(edgeKey);
        dagreGraph.setEdge(edge.source, edge.target, {
            weight: edge.weight ?? 1,
            minlen: edge.minLen ?? 1,
        });
    }

    const targetProductNodeIds = toSortedArray(
        (options.targetProductNodeIds ?? []).filter((id) => nodeIdSet.has(id)),
    );

    let targetSinkNodeId: string | null = null;
    if (targetProductNodeIds.length > 0) {
        targetSinkNodeId = makeTargetSinkNodeId(nodeIdSet);
        dagreGraph.setNode(targetSinkNodeId, { width: 1, height: 1 });
        for (const targetNodeId of targetProductNodeIds) {
            const edgeKey = `${targetNodeId}=>${targetSinkNodeId}`;
            if (insertedEdges.has(edgeKey)) continue;
            insertedEdges.add(edgeKey);
            dagreGraph.setEdge(targetNodeId, targetSinkNodeId, {
                weight: TARGET_EDGE_WEIGHT,
                minlen: 1,
            });
        }
    }

    dagre.layout(dagreGraph);

    const layoutedNodes = nodes.map((node) => {
        const point = dagreGraph.node(node.id);
        if (!point) {
            return node;
        }

        return {
            ...node,
            targetPosition: "left",
            sourcePosition: "right",
            // Dagre reports center points; React Flow expects top-left coordinates.
            position: {
                x: point.x - point.width / 2,
                y: point.y - point.height / 2,
            },
        };
    });

    return {
        nodes: layoutedNodes,
        edges,
    };
}
