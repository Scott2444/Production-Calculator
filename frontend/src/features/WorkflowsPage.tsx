"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";
import SearchBar from "@/components/SearchBar";
import WorkflowEditorDialog from "@/components/WorkflowEditorDialog";
import DeleteWorkflow from "@/components/DeleteWorkflow";
import { useProject } from "@/context/ProjectContext";
import { type Workflow } from "@/lib/workflow";
import { Link, useNavigate } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useSearch } from "@/hooks/Search";
import { formatTimestamp } from "@/lib/timestamp";
import { useWorkflowsQuery } from "@/hooks/useQueries";
import { IconPencil, IconTrash } from "@tabler/icons-react";

function coerceWorkflows(value: unknown): Workflow[] {
    if (!value) return [];
    if (Array.isArray(value)) return value as Workflow[];
    if (typeof value === "object") {
        const maybeItems = (value as { items?: unknown }).items;
        if (Array.isArray(maybeItems)) return maybeItems as Workflow[];
    }
    return [];
}

function getWorkflowRouteSegment(workflow: Workflow): string {
    const trimmedName = workflow.name?.trim();
    return trimmedName && trimmedName.length > 0 ? trimmedName : workflow.puid;
}

export default function WorkflowsPage() {
    const { routeUsername, routeProjectName, projectId, isOwner } =
        useProject();
    const navigate = useNavigate();

    const [createOpen, setCreateOpen] = useState(false);
    const [editOpen, setEditOpen] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [selectedWorkflow, setSelectedWorkflow] = useState<Workflow | null>(
        null,
    );

    const workflowsQuery = useWorkflowsQuery(projectId, { enabled: isOwner });

    const workflows = useMemo(
        () => coerceWorkflows(workflowsQuery.data),
        [workflowsQuery.data],
    );

    const sortedWorkflows = useMemo(() => {
        return [...workflows].sort((a, b) => {
            const left = a.name?.trim() || a.puid;
            const right = b.name?.trim() || b.puid;
            return left.localeCompare(right, undefined, {
                sensitivity: "base",
            });
        });
    }, [workflows]);

    const {
        searchText,
        setSearchText,
        filteredItems: filteredWorkflows,
    } = useSearch(sortedWorkflows, {
        toText: (workflow) =>
            `${workflow.name ?? ""} ${workflow.description ?? ""} ${workflow.puid}`,
    });

    return (
        <ProjectPageLayout>
            <div className="flex min-h-full flex-col gap-4">
                <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
                    <div className="min-w-0">
                        <h1 className="truncate text-2xl font-semibold text-slate-100">
                            Workflows
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

                    <div className="flex flex-wrap items-center gap-2">
                        {isOwner && (
                            <button
                                type="button"
                                className="inline-flex items-center gap-2 self-start rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                onClick={() => setCreateOpen(true)}
                            >
                                Create Workflow
                            </button>
                        )}
                    </div>
                </div>

                <ProjectStatusGate>
                    {!isOwner ? (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                            Workflows can only be viewed by the project owner.
                        </div>
                    ) : (
                        <>
                            <SearchBar
                                value={searchText}
                                onChange={setSearchText}
                                disabled={!projectId}
                            />

                            {workflowsQuery.isLoading && projectId && (
                                <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3 text-sm text-slate-400">
                                    Loading workflows…
                                </div>
                            )}

                            {!workflowsQuery.isLoading &&
                                workflowsQuery.error && (
                                    <div className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                                        Failed to load workflows.
                                    </div>
                                )}

                            {!workflowsQuery.isLoading &&
                                !workflowsQuery.error &&
                                projectId &&
                                filteredWorkflows.length === 0 && (
                                    <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-sm text-slate-300">
                                        {searchText.trim()
                                            ? "No workflows match your search."
                                            : "No workflows yet."}
                                    </div>
                                )}

                            {filteredWorkflows.length > 0 && (
                                <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 2xl:grid-cols-3">
                                    {filteredWorkflows.map((workflow) => {
                                        const workflowLabel =
                                            workflow.name?.trim() ||
                                            workflow.puid;
                                        const workflowHref = `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/workflows/${encodeURIComponent(getWorkflowRouteSegment(workflow))}`;

                                        return (
                                            <div
                                                key={workflow.puid}
                                                className="group relative rounded-xl border border-slate-800 bg-slate-900/40 p-4 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60 focus-within:ring-2 focus-within:ring-purple-500/40"
                                            >
                                                <Link
                                                    to={workflowHref}
                                                    className="block focus:outline-none"
                                                >
                                                    <div className="flex items-start justify-between gap-3">
                                                        <div className="min-w-0">
                                                            <div className="truncate text-base font-semibold text-slate-100 group-hover:text-white">
                                                                {workflowLabel}
                                                            </div>
                                                            {workflow.description ? (
                                                                <div className="mt-1 line-clamp-3 text-sm text-slate-300">
                                                                    {
                                                                        workflow.description
                                                                    }
                                                                </div>
                                                            ) : (
                                                                <div className="mt-1 text-sm text-slate-500">
                                                                    No
                                                                    description
                                                                </div>
                                                            )}
                                                            <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-slate-400">
                                                                <span>
                                                                    Updated{" "}
                                                                    {formatTimestamp(
                                                                        workflow.updatedAt,
                                                                    )}
                                                                </span>
                                                                <span>
                                                                    Created{" "}
                                                                    {formatTimestamp(
                                                                        workflow.createdAt,
                                                                    )}
                                                                </span>
                                                            </div>
                                                        </div>
                                                        <div className="rounded-md border border-slate-700 bg-slate-900/70 px-2 py-1 text-xs text-slate-400">
                                                            Open
                                                        </div>
                                                    </div>
                                                </Link>

                                                <div className="mt-4 flex items-center justify-end gap-2 opacity-0 transition-opacity group-hover:opacity-100">
                                                    <button
                                                        type="button"
                                                        className="inline-flex size-8 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 text-slate-400 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                        title="Edit Workflow"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            setSelectedWorkflow(
                                                                workflow,
                                                            );
                                                            setEditOpen(true);
                                                        }}
                                                    >
                                                        <IconPencil className="size-4" />
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className="inline-flex size-8 items-center justify-center rounded-lg border border-slate-700 bg-slate-900/60 text-slate-400 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-red-950/30 hover:text-red-300 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                                                        title="Delete Workflow"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            setSelectedWorkflow(
                                                                workflow,
                                                            );
                                                            setDeleteOpen(true);
                                                        }}
                                                    >
                                                        <IconTrash className="size-4" />
                                                    </button>
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>
                            )}
                        </>
                    )}
                </ProjectStatusGate>

                {isOwner && (
                    <div className="mt-auto border-t border-slate-800 pt-4">
                        <div className="flex flex-wrap items-center gap-3 text-sm text-slate-400">
                            <span>Expecting something else?</span>
                            {!workflowsQuery.isLoading && (
                                <button
                                    type="button"
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-60"
                                    onClick={() => {
                                        void workflowsQuery.refetch();
                                    }}
                                    disabled={workflowsQuery.isFetching}
                                >
                                    {workflowsQuery.isFetching
                                        ? "Refreshing..."
                                        : "Refresh"}
                                </button>
                            )}
                        </div>
                    </div>
                )}

                <WorkflowEditorDialog
                    mode="create"
                    open={createOpen}
                    onOpenChange={setCreateOpen}
                    projectId={projectId}
                    onCreated={(workflow) => {
                        void navigate({
                            to: `/${encodeURIComponent(routeUsername ?? "")}/${encodeURIComponent(routeProjectName ?? "")}/workflows/${encodeURIComponent(getWorkflowRouteSegment(workflow))}`,
                        });
                    }}
                />

                <WorkflowEditorDialog
                    mode="edit"
                    open={editOpen}
                    onOpenChange={(next) => {
                        setEditOpen(next);
                        if (!next) setSelectedWorkflow(null);
                    }}
                    projectId={projectId}
                    workflow={selectedWorkflow}
                />

                <DeleteWorkflow
                    open={deleteOpen}
                    onOpenChange={setDeleteOpen}
                    projectId={projectId}
                    workflow={selectedWorkflow}
                    onDeleted={() => setSelectedWorkflow(null)}
                />
            </div>
        </ProjectPageLayout>
    );
}
