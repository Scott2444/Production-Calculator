"use client";

import type { ComponentType, RefObject } from "react";
import {
    IconArrowDown,
    IconArrowUp,
    IconClock,
    IconPlus,
    IconSettings,
    IconTrash,
} from "@tabler/icons-react";
import ErrorDisplay from "@/components/ErrorDisplay";
import Popup from "@/components/Popup";
import { type RecipeExchange, type RecipeAttributeRate } from "@/types/recipes";

type RecipeEditorMode = "create" | "edit";

type ProductDropDownProps = {
    value: string;
    onSelect: (next: string) => void;
    disabled?: boolean;
};

type AttributeDropDownProps = {
    value: string;
    onSelect: (next: string) => void;
    disabled?: boolean;
};

export interface RecipeEditorDialogProps {
    mode: RecipeEditorMode;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    name: string;
    description: string;
    baseCraftingTime: string;
    inputs: RecipeExchange[];
    outputs: RecipeExchange[];
    attributes: RecipeAttributeRate[];
    onNameChange: (value: string) => void;
    onDescriptionChange: (value: string) => void;
    onBaseCraftingTimeChange: (value: string) => void;
    onAddInput: () => void;
    onInputPuidChange: (index: number, puid: string) => void;
    onInputQuantityChange: (index: number, quantity: number) => void;
    onRemoveInput: (index: number) => void;
    onAddOutput: () => void;
    onOutputPuidChange: (index: number, puid: string) => void;
    onOutputQuantityChange: (index: number, quantity: number) => void;
    onRemoveOutput: (index: number) => void;
    onAddAttribute: () => void;
    onAttributePuidChange: (index: number, puid: string) => void;
    onAttributeRateChange: (index: number, rate: number) => void;
    onRemoveAttribute: (index: number) => void;
    sortedProductsCount: number;
    sortedAttributesCount: number;
    error: string | null;
    onDismissError: () => void;
    onSubmit: () => void;
    onCancel: () => void;
    submitting: boolean;
    submitDisabled: boolean;
    initialFocusRef?: RefObject<HTMLInputElement | null>;
    ProductDropDown: ComponentType<ProductDropDownProps>;
    AttributeDropDown: ComponentType<AttributeDropDownProps>;
}

export default function RecipeEditorDialog({
    mode,
    open,
    onOpenChange,
    name,
    description,
    baseCraftingTime,
    inputs,
    outputs,
    attributes,
    onNameChange,
    onDescriptionChange,
    onBaseCraftingTimeChange,
    onAddInput,
    onInputPuidChange,
    onInputQuantityChange,
    onRemoveInput,
    onAddOutput,
    onOutputPuidChange,
    onOutputQuantityChange,
    onRemoveOutput,
    onAddAttribute,
    onAttributePuidChange,
    onAttributeRateChange,
    onRemoveAttribute,
    sortedProductsCount,
    sortedAttributesCount,
    error,
    onDismissError,
    onSubmit,
    onCancel,
    submitting,
    submitDisabled,
    initialFocusRef,
    ProductDropDown,
    AttributeDropDown,
}: RecipeEditorDialogProps) {
    const isCreate = mode === "create";

    return (
        <Popup
            open={open}
            onOpenChange={onOpenChange}
            title={isCreate ? "Add recipe" : "Edit recipe"}
            description={
                isCreate
                    ? "Create a new recipe in this project."
                    : "Update recipe details."
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
                                      id: `${mode}-recipe-error`,
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
                        placeholder="A brief description..."
                        rows={3}
                        className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        disabled={submitting}
                    />
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-amber-400">
                        <IconClock size={16} />
                        Crafting Time
                    </div>
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Base time (seconds)
                        </label>
                        <input
                            value={baseCraftingTime}
                            onChange={(e) =>
                                onBaseCraftingTimeChange(e.target.value)
                            }
                            inputMode="decimal"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={submitting}
                        />
                    </div>
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-emerald-400">
                            <IconArrowDown size={16} />
                            Inputs
                        </div>
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={onAddInput}
                            disabled={submitting || sortedProductsCount === 0}
                        >
                            <IconPlus size={16} />
                            Add
                        </button>
                    </div>
                    <div className="flex flex-col gap-2">
                        {inputs.length === 0 && (
                            <div className="text-sm text-slate-500">
                                No inputs
                            </div>
                        )}
                        {inputs.map((row, idx) => (
                            <div
                                key={`input-${idx}`}
                                className="flex items-center gap-2"
                            >
                                <div className="flex-1">
                                    <ProductDropDown
                                        value={row.puid}
                                        disabled={submitting}
                                        onSelect={(next) =>
                                            onInputPuidChange(idx, next)
                                        }
                                    />
                                </div>
                                <input
                                    value={row.quantity}
                                    onChange={(e) =>
                                        onInputQuantityChange(
                                            idx,
                                            Number(e.target.value),
                                        )
                                    }
                                    inputMode="decimal"
                                    className="w-24 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                    placeholder="Qty"
                                />
                                <button
                                    type="button"
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                                    onClick={() => onRemoveInput(idx)}
                                    disabled={submitting}
                                    title="Remove"
                                >
                                    <IconTrash size={18} />
                                </button>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-blue-400">
                            <IconArrowUp size={16} />
                            Outputs
                        </div>
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={onAddOutput}
                            disabled={submitting || sortedProductsCount === 0}
                        >
                            <IconPlus size={16} />
                            Add
                        </button>
                    </div>
                    <div className="flex flex-col gap-2">
                        {outputs.length === 0 && (
                            <div className="text-sm text-slate-500">
                                No outputs
                            </div>
                        )}
                        {outputs.map((row, idx) => (
                            <div
                                key={`output-${idx}`}
                                className="flex items-center gap-2"
                            >
                                <div className="flex-1">
                                    <ProductDropDown
                                        value={row.puid}
                                        disabled={submitting}
                                        onSelect={(next) =>
                                            onOutputPuidChange(idx, next)
                                        }
                                    />
                                </div>
                                <input
                                    value={row.quantity}
                                    onChange={(e) =>
                                        onOutputQuantityChange(
                                            idx,
                                            Number(e.target.value),
                                        )
                                    }
                                    inputMode="decimal"
                                    className="w-24 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                    placeholder="Qty"
                                />
                                <button
                                    type="button"
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                                    onClick={() => onRemoveOutput(idx)}
                                    disabled={submitting}
                                    title="Remove"
                                >
                                    <IconTrash size={18} />
                                </button>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-purple-400">
                            <IconSettings size={16} />
                            Recipe Attributes
                        </div>
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={onAddAttribute}
                            disabled={submitting || sortedAttributesCount === 0}
                        >
                            <IconPlus size={16} />
                            Add
                        </button>
                    </div>
                    <div className="flex flex-col gap-2">
                        {attributes.length === 0 && (
                            <div className="text-sm text-slate-500">
                                No attributes
                            </div>
                        )}
                        {attributes.map((row, idx) => (
                            <div
                                key={`attr-${idx}`}
                                className="flex items-center gap-2"
                            >
                                <div className="flex-1">
                                    <AttributeDropDown
                                        value={row.puid}
                                        disabled={submitting}
                                        onSelect={(next) =>
                                            onAttributePuidChange(idx, next)
                                        }
                                    />
                                </div>
                                <input
                                    value={row.rate}
                                    onChange={(e) =>
                                        onAttributeRateChange(
                                            idx,
                                            Number(e.target.value),
                                        )
                                    }
                                    inputMode="decimal"
                                    className="w-24 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                    placeholder="Rate"
                                />
                                <button
                                    type="button"
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                                    onClick={() => onRemoveAttribute(idx)}
                                    disabled={submitting}
                                    title="Remove"
                                >
                                    <IconTrash size={18} />
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </Popup>
    );
}
