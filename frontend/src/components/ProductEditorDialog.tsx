"use client";

import type { RefObject } from "react";
import ErrorDisplay from "@/components/ErrorDisplay";
import Popup from "@/components/Popup";

type ProductEditorMode = "create" | "edit";

export interface ProductEditorDialogProps {
    mode: ProductEditorMode;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    name: string;
    description: string;
    onNameChange: (value: string) => void;
    onDescriptionChange: (value: string) => void;
    error: string | null;
    onDismissError: () => void;
    onSubmit: () => void;
    onCancel: () => void;
    submitting: boolean;
    submitDisabled: boolean;
    initialFocusRef?: RefObject<HTMLInputElement | null>;
}

export default function ProductEditorDialog({
    mode,
    open,
    onOpenChange,
    name,
    description,
    onNameChange,
    onDescriptionChange,
    error,
    onDismissError,
    onSubmit,
    onCancel,
    submitting,
    submitDisabled,
    initialFocusRef,
}: ProductEditorDialogProps) {
    const isCreate = mode === "create";

    return (
        <Popup
            open={open}
            onOpenChange={onOpenChange}
            title={isCreate ? "Add product" : "Edit product"}
            description={
                isCreate
                    ? "Create a new product in this project."
                    : "Update product details."
            }
            initialFocusRef={initialFocusRef}
            footer={
                <div className="flex items-center justify-end gap-2">
                    <button
                        type="button"
                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        onClick={onCancel}
                        disabled={submitting}
                    >
                        Cancel
                    </button>
                    <button
                        type="button"
                        className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                        onClick={onSubmit}
                        disabled={submitDisabled || submitting}
                    >
                        {submitting
                            ? isCreate
                                ? "Creating..."
                                : "Saving..."
                            : isCreate
                              ? "Create"
                              : "Save"}
                    </button>
                </div>
            }
        >
            <div className="flex flex-col gap-4">
                <ErrorDisplay
                    errors={
                        error
                            ? [
                                  {
                                      id: `${mode}-error`,
                                      message: error,
                                      onDismiss: onDismissError,
                                  },
                              ]
                            : []
                    }
                />

                <div className="flex flex-col gap-2">
                    <label className="text-sm font-medium text-slate-200">
                        Name
                    </label>
                    <input
                        ref={initialFocusRef}
                        value={name}
                        onChange={(e) => onNameChange(e.target.value)}
                        placeholder="Iron Ingot"
                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        disabled={submitting}
                    />
                </div>

                <div className="flex flex-col gap-2">
                    <label className="text-sm font-medium text-slate-200">
                        Description (Optional)
                    </label>
                    <textarea
                        value={description}
                        onChange={(e) => onDescriptionChange(e.target.value)}
                        placeholder="A brief description about this product..."
                        rows={3}
                        className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        disabled={submitting}
                    />
                </div>
            </div>
        </Popup>
    );
}
