"use client";

import React, { useEffect, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Popup from "@/components/Popup";
import { useProtectedApi } from "@/lib/api";
import { deleteWorkflow, Workflow } from "@/lib/workflow";

export interface DeleteWorkflowProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    projectId: string;
    workflow: Workflow | null;
    onDeleted?: (workflowPuid: string) => void;
}

export default function DeleteWorkflow({
    open,
    onOpenChange,
    projectId,
    workflow,
    onDeleted,
}: DeleteWorkflowProps): React.ReactElement {
    const queryClient = useQueryClient();
    const protectedApi = useProtectedApi();

    const [confirmText, setConfirmText] = useState("");
    const [error, setError] = useState<string | null>(null);
    const confirmRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (open) {
            setConfirmText("");
            setError(null);
        }
    }, [open]);

    const deleteWorkflowMutation = useMutation({
        mutationFn: async () => {
            if (!workflow) throw new Error("No workflow selected.");
            await deleteWorkflow(projectId, workflow.puid, protectedApi);
            return workflow.puid;
        },
        onSuccess: async (puid) => {
            setError(null);
            onOpenChange(false);

            await queryClient.invalidateQueries({
                queryKey: ["workflows", projectId],
            });

            if (onDeleted) {
                onDeleted(puid);
            }
        },
        onError: (err) => {
            setError(
                err instanceof Error
                    ? err.message
                    : "Failed to delete workflow.",
            );
        },
    });

    const isPending = deleteWorkflowMutation.isPending;
    const isConfirmed = confirmText.trim().toUpperCase() === "DELETE";

    return (
        <Popup
            open={open}
            onOpenChange={(next) => {
                onOpenChange(next);
                if (next) {
                    setError(null);
                    setConfirmText("");
                }
            }}
            title="Delete workflow"
            description="This action cannot be undone."
            initialFocusRef={confirmRef}
            footer={
                <div className="flex items-center justify-end gap-2">
                    <button
                        type="button"
                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        onClick={() => onOpenChange(false)}
                        disabled={isPending}
                    >
                        Cancel
                    </button>
                    <button
                        type="button"
                        className="rounded-lg bg-red-600/30 px-4 py-2 text-sm font-medium text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:opacity-50"
                        onClick={() => {
                            if (!isConfirmed) return;
                            deleteWorkflowMutation.mutate();
                        }}
                        disabled={isPending || !isConfirmed}
                    >
                        {isPending ? "Deleting..." : "Delete Workflow"}
                    </button>
                </div>
            }
        >
            <div className="flex flex-col gap-4 py-2">
                {error && (
                    <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-3 py-2 text-sm text-red-200">
                        {error}
                    </div>
                )}
                <div className="text-sm text-slate-300">
                    Are you sure you want to delete{" "}
                    <span className="font-semibold text-slate-100 italic">
                        {workflow?.name || workflow?.puid}
                    </span>
                    ? All production nodes and data associated with this
                    workflow will be permanently removed.
                </div>
                <div className="flex flex-col gap-1.5">
                    <label
                        htmlFor="delete-workflow-confirm"
                        className="text-xs font-medium text-slate-400"
                    >
                        Type{" "}
                        <span className="text-red-400 font-bold">DELETE</span>{" "}
                        to confirm
                    </label>
                    <input
                        ref={confirmRef}
                        id="delete-workflow-confirm"
                        type="text"
                        className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 placeholder-slate-500 focus:border-red-500/60 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                        placeholder="DELETE"
                        value={confirmText}
                        onChange={(e) => setConfirmText(e.target.value)}
                        disabled={isPending}
                    />
                </div>
            </div>
        </Popup>
    );
}
