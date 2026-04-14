"use client";

import type { ComponentType, RefObject } from "react";
import {
    IconClipboardList,
    IconGauge,
    IconPlus,
    IconSettings,
    IconTrash,
} from "@tabler/icons-react";
import DocsHelpLink from "@/components/DocsHelpLink";
import ErrorDisplay from "@/components/ErrorDisplay";
import Popup from "@/components/Popup";
import { type MachineAttributeRate } from "@/types/machines";

type MachineEditorMode = "create" | "edit";

type AttributeDropDownProps = {
    value: string;
    onSelect: (next: string) => void;
    disabled?: boolean;
};

type RecipeMultiSelectProps = {
    value: string[];
    onChange: (next: string[]) => void;
    disabled?: boolean;
};

export interface MachineEditorDialogProps {
    mode: MachineEditorMode;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    name: string;
    description: string;
    baseSpeed: string;
    recipePuids: string[];
    attributes: MachineAttributeRate[];
    onNameChange: (value: string) => void;
    onDescriptionChange: (value: string) => void;
    onBaseSpeedChange: (value: string) => void;
    onRecipePuidsChange: (value: string[]) => void;
    onAddAttribute: () => void;
    onAttributePuidChange: (index: number, puid: string) => void;
    onAttributeRateChange: (index: number, rate: number) => void;
    onRemoveAttribute: (index: number) => void;
    onRemoveRecipe: (puid: string) => void;
    getRecipeLabel: (puid: string) => string;
    sortedRecipesCount: number;
    sortedAttributesCount: number;
    error: string | null;
    onDismissError: () => void;
    onSubmit: () => void;
    onCancel: () => void;
    submitting: boolean;
    submitDisabled: boolean;
    initialFocusRef?: RefObject<HTMLInputElement | null>;
    AttributeDropDown: ComponentType<AttributeDropDownProps>;
    RecipeMultiSelect: ComponentType<RecipeMultiSelectProps>;
}

function uniqueTrimmedPuids(values: string[]): string[] {
    const out: string[] = [];
    const seen = new Set<string>();
    for (const raw of values) {
        const v = raw?.trim?.() ?? raw;
        if (!v) continue;
        if (seen.has(v)) continue;
        seen.add(v);
        out.push(v);
    }
    return out;
}

export default function MachineEditorDialog({
    mode,
    open,
    onOpenChange,
    name,
    description,
    baseSpeed,
    recipePuids,
    attributes,
    onNameChange,
    onDescriptionChange,
    onBaseSpeedChange,
    onRecipePuidsChange,
    onAddAttribute,
    onAttributePuidChange,
    onAttributeRateChange,
    onRemoveAttribute,
    onRemoveRecipe,
    getRecipeLabel,
    sortedRecipesCount,
    sortedAttributesCount,
    error,
    onDismissError,
    onSubmit,
    onCancel,
    submitting,
    submitDisabled,
    initialFocusRef,
    AttributeDropDown,
    RecipeMultiSelect,
}: MachineEditorDialogProps) {
    const isCreate = mode === "create";
    const selectedRecipes = uniqueTrimmedPuids(recipePuids);

    return (
        <Popup
            open={open}
            onOpenChange={onOpenChange}
            title={isCreate ? "Add machine" : "Edit machine"}
            description={
                isCreate
                    ? "Create a new machine in this project."
                    : "Update machine details."
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
                    <div className="flex items-center gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Name
                        </label>
                        <div className="flex justify-end">
                            <DocsHelpLink
                                slug="projects/components/machines"
                                sectionId="key-fields"
                                title="Open machines docs in a new tab"
                            />
                        </div>
                    </div>
                    <input
                        ref={initialFocusRef}
                        value={name}
                        onChange={(e) => onNameChange(e.target.value)}
                        placeholder="Assembler"
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
                        placeholder="A brief description about this machine..."
                        rows={3}
                        className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        disabled={submitting}
                    />
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-purple-400">
                        <IconGauge size={16} />
                        Machine Speed
                        <DocsHelpLink
                            slug="calculation/formulas"
                            sectionId="speed-formulas"
                            title="Open machine speed formulas docs in a new tab"
                        />
                    </div>
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Base speed
                        </label>
                        <input
                            value={baseSpeed}
                            onChange={(e) => onBaseSpeedChange(e.target.value)}
                            inputMode="decimal"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={submitting}
                        />
                    </div>
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-emerald-400">
                        <IconClipboardList size={16} />
                        Compatible Recipes
                        <DocsHelpLink
                            slug="projects/components/machines"
                            sectionId="key-fields"
                            title="Open compatible recipes docs in a new tab"
                        />
                    </div>

                    {sortedRecipesCount === 0 ? (
                        <div className="mt-2 text-sm text-slate-500">
                            No recipes available in this project.
                        </div>
                    ) : null}

                    <div className="flex flex-col gap-2">
                        <RecipeMultiSelect
                            value={recipePuids}
                            onChange={onRecipePuidsChange}
                            disabled={submitting}
                        />

                        {selectedRecipes.length === 0 ? (
                            <div className="text-sm text-slate-500">
                                No recipes selected
                            </div>
                        ) : (
                            <div className="flex flex-col gap-2">
                                {selectedRecipes.map((puid) => (
                                    <div
                                        key={puid}
                                        className="flex items-center justify-between gap-3 rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-2"
                                    >
                                        <div className="min-w-0 truncate text-sm text-slate-200">
                                            {getRecipeLabel(puid)}
                                        </div>
                                        <button
                                            type="button"
                                            className="shrink-0 rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                            onClick={() => onRemoveRecipe(puid)}
                                            disabled={submitting}
                                            title="Remove"
                                            aria-label="Remove recipe"
                                        >
                                            <IconTrash size={18} />
                                        </button>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-blue-400">
                            <IconSettings size={16} />
                            User Defined Attributes
                            <DocsHelpLink
                                slug="calculation/formulas"
                                sectionId="attribute-formulas"
                                title="Open machine attribute formula docs in a new tab"
                            />
                        </div>
                        <button
                            type="button"
                            className="inline-flex items-center gap-2 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={onAddAttribute}
                            disabled={submitting || sortedAttributesCount === 0}
                            title={
                                sortedAttributesCount === 0
                                    ? "Create attributes first"
                                    : "Add attribute"
                            }
                        >
                            <IconPlus size={16} />
                            Add
                        </button>
                    </div>

                    {sortedAttributesCount === 0 ? (
                        <div className="mt-2 text-sm text-slate-500">
                            No attributes available in this project.
                        </div>
                    ) : null}

                    <div className="mt-3 flex flex-col gap-2">
                        {attributes.length === 0 && (
                            <div className="text-sm text-slate-500">
                                No attributes
                            </div>
                        )}
                        {attributes.map((row, idx) => (
                            <div
                                key={`${mode}-attr-${idx}`}
                                className="flex items-center gap-2"
                            >
                                <AttributeDropDown
                                    value={row.puid}
                                    disabled={submitting}
                                    onSelect={(next) =>
                                        onAttributePuidChange(idx, next)
                                    }
                                />
                                <input
                                    value={String(row.rate)}
                                    onChange={(e) =>
                                        onAttributeRateChange(
                                            idx,
                                            Number(e.target.value),
                                        )
                                    }
                                    inputMode="decimal"
                                    className="w-32 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                    placeholder="Rate"
                                />
                                <button
                                    type="button"
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-red-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                    onClick={() => onRemoveAttribute(idx)}
                                    disabled={submitting}
                                    title="Remove"
                                    aria-label="Remove attribute"
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
