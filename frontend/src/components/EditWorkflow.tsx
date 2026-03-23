"use client";

import React, { useEffect, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Popup from "@/components/Popup";
import { useProtectedApi } from "@/lib/api";
import { updateWorkflow, NewWorkflowPayload, Workflow } from "@/lib/workflow";

export interface EditWorkflowProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    projectId: string;
    workflow: Workflow | null;
    onUpdated?: (workflow: Workflow) => void;
}

export default function EditWorkflow({
    open,
    onOpenChange,
    projectId,
    workflow,
    onUpdated,
}: EditWorkflowProps): React.ReactElement {
    const queryClient = useQueryClient();
    const protectedApi = useProtectedApi();

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [error, setError] = useState<string | null>(null);
    const nameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (open && workflow) {
            setName(workflow.name ?? "");
            setDescription(workflow.description ?? "");
            setError(null);
        }
    }, [open, workflow]);

    const editWorkflowMutation = useMutation({
        mutationFn: async (payload: NewWorkflowPayload) => {
            if (!workflow) throw new Error("No workflow selected.");
            const response = await updateWorkflow(
                projectId,
                workflow.puid,
                protectedApi,
                payload,
            );
            return response as Workflow;
        },
        onSuccess: async (updatedWorkflow) => {
            setError(null);
            onOpenChange(false);

            await queryClient.invalidateQueries({
                queryKey: ["workflows", projectId],
            });

            if (onUpdated) {
                onUpdated(updatedWorkflow);
            }
        },
        onError: (err) => {
            setError(
                err instanceof Error
                    ? err.message
                    : "Failed to update workflow.",
            );
        },
    });

    const isPending = editWorkflowMutation.isPending;

    return (
        <Popup
            open={open}
            onOpenChange={(next) => {
                onOpenChange(next);
                if (next && workflow) {
                    setError(null);
                    setName(workflow.name ?? "");
                    setDescription(workflow.description ?? "");
                }
            }}
            title="Edit workflow"
            description="Update the details of your workflow."
            initialFocusRef={nameRef}
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
                        className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-50"
                        onClick={() => {
                            if (!name.trim()) {
                                setError("Name is required.");
                                return;
                            }
                            editWorkflowMutation.mutate({
                                name: name.trim(),
                                description: description.trim() || null,
                            });
                        }}
                        disabled={isPending}
                    >
                        {isPending ? "Updating..." : "Update Workflow"}
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
                <div className="flex flex-col gap-1.5">
                    <label
                        htmlFor="edit-workflow-name"
                        className="text-xs font-medium text-slate-400"
                    >
                        Name
                    </label>
                    <input
                        ref={nameRef}
                        id="edit-workflow-name"
                        type="text"
                        className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 placeholder-slate-500 focus:border-purple-500/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        placeholder="My Workflow"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        disabled={isPending}
                    />
                </div>
                <div className="flex flex-col gap-1.5">
                    <label
                        htmlFor="edit-workflow-description"
                        className="text-xs font-medium text-slate-400"
                    >
                        Description (Optional)
                    </label>
                    <textarea
                        id="edit-workflow-description"
                        rows={3}
                        className="w-full resize-none rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 placeholder-slate-500 focus:border-purple-500/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        placeholder="Explain what this workflow is for..."
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        disabled={isPending}
                    />
                </div>
            </div>
        </Popup>
    );
}
