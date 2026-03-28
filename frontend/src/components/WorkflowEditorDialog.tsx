"use client";

import { useEffect, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Popup from "@/components/Popup";
import { useProtectedApi } from "@/lib/api";
import {
    type NewWorkflowPayload,
    postNewWorkflow,
    updateWorkflow,
    type Workflow,
} from "@/lib/workflow";

type WorkflowEditorMode = "create" | "edit";

export interface WorkflowEditorDialogProps {
    mode: WorkflowEditorMode;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    projectId: string;
    workflow?: Workflow | null;
    onCreated?: (workflow: Workflow) => void;
    onUpdated?: (workflow: Workflow) => void;
}

export default function WorkflowEditorDialog({
    mode,
    open,
    onOpenChange,
    projectId,
    workflow = null,
    onCreated,
    onUpdated,
}: WorkflowEditorDialogProps) {
    const queryClient = useQueryClient();
    const protectedApi = useProtectedApi();

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [error, setError] = useState<string | null>(null);
    const nameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (!open) return;
        setError(null);
        if (mode === "edit") {
            setName(workflow?.name ?? "");
            setDescription(workflow?.description ?? "");
        } else {
            setName("");
            setDescription("");
        }
    }, [open, mode, workflow]);

    const mutation = useMutation({
        mutationFn: async (payload: NewWorkflowPayload) => {
            if (!projectId) throw new Error("No project selected.");

            if (mode === "create") {
                const response = await postNewWorkflow(
                    projectId,
                    protectedApi,
                    payload,
                );
                return response as Workflow;
            }

            if (!workflow) throw new Error("No workflow selected.");
            const response = await updateWorkflow(
                projectId,
                workflow.puid,
                protectedApi,
                payload,
            );
            return response as Workflow;
        },
        onSuccess: async (savedWorkflow) => {
            setError(null);
            onOpenChange(false);

            await queryClient.invalidateQueries({
                queryKey: ["workflows", projectId],
            });

            if (mode === "create") {
                onCreated?.(savedWorkflow);
            } else {
                onUpdated?.(savedWorkflow);
            }
        },
        onError: (err) => {
            setError(
                err instanceof Error
                    ? err.message
                    : mode === "create"
                      ? "Failed to create workflow."
                      : "Failed to update workflow.",
            );
        },
    });

    const isPending = mutation.isPending;

    return (
        <Popup
            open={open}
            onOpenChange={(next) => {
                onOpenChange(next);
                if (next) setError(null);
            }}
            title={mode === "create" ? "Create workflow" : "Edit workflow"}
            description={
                mode === "create"
                    ? "Create a new workflow for your project."
                    : "Update the details of your workflow."
            }
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

                            mutation.mutate({
                                name: name.trim(),
                                description: description.trim() || null,
                            });
                        }}
                        disabled={isPending || (mode === "edit" && !workflow)}
                    >
                        {isPending
                            ? mode === "create"
                                ? "Creating..."
                                : "Updating..."
                            : mode === "create"
                              ? "Create Workflow"
                              : "Update Workflow"}
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
                        htmlFor={`${mode}-workflow-name`}
                        className="text-xs font-medium text-slate-400"
                    >
                        Name
                    </label>
                    <input
                        ref={nameRef}
                        id={`${mode}-workflow-name`}
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
                        htmlFor={`${mode}-workflow-description`}
                        className="text-xs font-medium text-slate-400"
                    >
                        Description (Optional)
                    </label>
                    <textarea
                        id={`${mode}-workflow-description`}
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
