import { useQuery } from "@tanstack/react-query";
import { useProtectedApi } from "@/lib/api";
import { fetchProducts } from "@/lib/products";
import { fetchRecipes } from "@/lib/recipes";
import { fetchMachines } from "@/lib/machines";
import { fetchModifiers } from "@/lib/modifiers";
import { fetchAttributes } from "@/lib/attributes";
import { fetchWorkflows } from "@/lib/workflow";
import { fetchWorkflowChart } from "@/lib/workflowChart";
import { fetchUser } from "@/lib/user";
import type { User } from "@/types/user";

export const STALE_TIME = {
    // Project_objects are relatively static and shared across many queries
    PROJECT_OBJECTS: 60 * 1000 * 60, // 60 minutes
    WORKFLOW_CHART: 0,
    USER: 5 * 60 * 1000, // 5 minutes
};

export function useUserQuery(
    userId: string | null | undefined,
    options: { enabled?: boolean } = {},
) {
    const protectedApi = useProtectedApi();
    return useQuery<User>({
        queryKey: ["user", userId],
        queryFn: () => fetchUser(userId!, protectedApi),
        enabled: Boolean(userId) && (options.enabled ?? true),
        staleTime: STALE_TIME.USER,
    });
}

export function useProductsQuery(projectId: string | null | undefined) {
    const protectedApi = useProtectedApi();
    return useQuery({
        queryKey: ["products", projectId],
        queryFn: () => fetchProducts(projectId!, protectedApi),
        enabled: Boolean(projectId),
        staleTime: STALE_TIME.PROJECT_OBJECTS,
    });
}

export function useRecipesQuery(projectId: string | null | undefined) {
    const protectedApi = useProtectedApi();
    return useQuery({
        queryKey: ["recipes", projectId],
        queryFn: () => fetchRecipes(projectId!, protectedApi),
        enabled: Boolean(projectId),
        staleTime: STALE_TIME.PROJECT_OBJECTS,
    });
}

export function useMachinesQuery(projectId: string | null | undefined) {
    const protectedApi = useProtectedApi();
    return useQuery({
        queryKey: ["machines", projectId],
        queryFn: () => fetchMachines(projectId!, protectedApi),
        enabled: Boolean(projectId),
        staleTime: STALE_TIME.PROJECT_OBJECTS,
    });
}

export function useModifiersQuery(projectId: string | null | undefined) {
    const protectedApi = useProtectedApi();
    return useQuery({
        queryKey: ["modifiers", projectId],
        queryFn: () => fetchModifiers(projectId!, protectedApi),
        enabled: Boolean(projectId),
        staleTime: STALE_TIME.PROJECT_OBJECTS,
    });
}

export function useAttributesQuery(projectId: string | null | undefined) {
    const protectedApi = useProtectedApi();
    return useQuery({
        queryKey: ["attributes", projectId],
        queryFn: () => fetchAttributes(projectId!, protectedApi),
        enabled: Boolean(projectId),
        staleTime: STALE_TIME.PROJECT_OBJECTS,
    });
}

export function useWorkflowsQuery(
    projectId: string | null | undefined,
    options: { enabled?: boolean } = {},
) {
    const protectedApi = useProtectedApi();
    return useQuery({
        queryKey: ["workflows", projectId],
        queryFn: () => fetchWorkflows(projectId!, protectedApi),
        enabled: Boolean(projectId) && (options.enabled ?? true),
        staleTime: STALE_TIME.PROJECT_OBJECTS,
    });
}

export function useWorkflowChartQuery(
    projectId: string | null | undefined,
    workflowId: string | null | undefined,
    options: { enabled?: boolean } = {},
) {
    const protectedApi = useProtectedApi();
    return useQuery({
        queryKey: ["workflow-chart", projectId, workflowId],
        queryFn: () =>
            fetchWorkflowChart(projectId!, workflowId!, protectedApi),
        enabled: Boolean(projectId && workflowId) && (options.enabled ?? true),
        staleTime: STALE_TIME.WORKFLOW_CHART,
    });
}
