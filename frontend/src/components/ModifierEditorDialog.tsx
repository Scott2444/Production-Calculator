"use client";

import type { ComponentType, RefObject } from "react";
import {
    IconGauge,
    IconPackage,
    IconPlus,
    IconSettings,
    IconTrash,
} from "@tabler/icons-react";
import DocsHelpLink from "@/components/DocsHelpLink";
import ErrorDisplay from "@/components/ErrorDisplay";
import Popup from "@/components/Popup";
import { type ModifierAttributeBonus } from "@/types/modifiers";

type ModifierEditorMode = "create" | "edit";

type AttributeDropDownProps = {
    value: string;
    onSelect: (next: string) => void;
    disabled?: boolean;
};

export interface ModifierEditorDialogProps {
    mode: ModifierEditorMode;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    name: string;
    description: string;
    flatBonus: string;
    percentBonus: string;
    multiplicativeBonus: string;
    inputPercent: string;
    outputPercent: string;
    attributes: ModifierAttributeBonus[];
    onNameChange: (value: string) => void;
    onDescriptionChange: (value: string) => void;
    onFlatBonusChange: (value: string) => void;
    onPercentBonusChange: (value: string) => void;
    onMultiplicativeBonusChange: (value: string) => void;
    onInputPercentChange: (value: string) => void;
    onOutputPercentChange: (value: string) => void;
    onAddAttribute: () => void;
    onAttributePuidChange: (index: number, puid: string) => void;
    onAttributeFlatBonusChange: (index: number, value: number) => void;
    onAttributePercentBonusChange: (index: number, value: number) => void;
    onAttributeMultiplicativeBonusChange: (
        index: number,
        value: number,
    ) => void;
    onRemoveAttribute: (index: number) => void;
    sortedAttributesCount: number;
    error: string | null;
    onDismissError: () => void;
    onSubmit: () => void;
    onCancel: () => void;
    submitting: boolean;
    submitDisabled: boolean;
    initialFocusRef?: RefObject<HTMLInputElement | null>;
    AttributeDropDown: ComponentType<AttributeDropDownProps>;
}

export default function ModifierEditorDialog({
    mode,
    open,
    onOpenChange,
    name,
    description,
    flatBonus,
    percentBonus,
    multiplicativeBonus,
    inputPercent,
    outputPercent,
    attributes,
    onNameChange,
    onDescriptionChange,
    onFlatBonusChange,
    onPercentBonusChange,
    onMultiplicativeBonusChange,
    onInputPercentChange,
    onOutputPercentChange,
    onAddAttribute,
    onAttributePuidChange,
    onAttributeFlatBonusChange,
    onAttributePercentBonusChange,
    onAttributeMultiplicativeBonusChange,
    onRemoveAttribute,
    sortedAttributesCount,
    error,
    onDismissError,
    onSubmit,
    onCancel,
    submitting,
    submitDisabled,
    initialFocusRef,
    AttributeDropDown,
}: ModifierEditorDialogProps) {
    const isCreate = mode === "create";

    return (
        <Popup
            open={open}
            onOpenChange={onOpenChange}
            title={isCreate ? "Add modifier" : "Edit modifier"}
            description={
                isCreate
                    ? "Create a new modifier in this project."
                    : "Update modifier details."
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
            <div className="flex min-w-0 flex-col gap-4">
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
                                slug="projects/components/modifiers"
                                sectionId="built-in-modifier-fields"
                                title="Open modifiers docs in a new tab"
                            />
                        </div>
                    </div>
                    <input
                        ref={initialFocusRef}
                        value={name}
                        onChange={(e) => onNameChange(e.target.value)}
                        placeholder="Productivity module"
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
                        rows={3}
                        placeholder="A brief description about this modifier..."
                        className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        disabled={submitting}
                    />
                </div>

                <div className="flex flex-col gap-6">
                    <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                        <div className="mb-4 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-purple-400">
                            <IconGauge size={16} />
                            Speed Modifiers
                            <DocsHelpLink
                                slug="projects/components/modifiers"
                                sectionId="speed"
                                title="Open speed modifiers docs in a new tab"
                            />
                        </div>
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                            <div className="flex flex-col gap-2">
                                <div className="flex items-center gap-1.5">
                                    <label className="text-sm font-medium text-slate-200">
                                        Flat bonus
                                    </label>
                                    <DocsHelpLink
                                        slug="calculation/formulas"
                                        sectionId="speed-formulas"
                                        title="Open flat bonus formula docs in a new tab"
                                    />
                                </div>
                                <input
                                    type="number"
                                    step="any"
                                    value={flatBonus}
                                    onChange={(e) =>
                                        onFlatBonusChange(e.target.value)
                                    }
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                />
                            </div>

                            <div className="flex flex-col gap-2">
                                <div className="flex items-center gap-1.5">
                                    <label className="text-sm font-medium text-slate-200">
                                        Additive bonus
                                    </label>
                                    <DocsHelpLink
                                        slug="calculation/formulas"
                                        sectionId="additive-stacking"
                                        title="Open additive stacking docs in a new tab"
                                    />
                                </div>
                                <input
                                    type="number"
                                    step="any"
                                    value={percentBonus}
                                    onChange={(e) =>
                                        onPercentBonusChange(e.target.value)
                                    }
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                />
                            </div>

                            <div className="flex flex-col gap-2">
                                <div className="flex items-center gap-1.5">
                                    <label className="text-sm font-medium text-slate-200">
                                        Multiplicative bonus
                                    </label>
                                    <DocsHelpLink
                                        slug="calculation/formulas"
                                        sectionId="multiplicative-stacking"
                                        title="Open multiplicative stacking docs in a new tab"
                                    />
                                </div>
                                <input
                                    type="number"
                                    step="any"
                                    value={multiplicativeBonus}
                                    onChange={(e) =>
                                        onMultiplicativeBonusChange(
                                            e.target.value,
                                        )
                                    }
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                />
                            </div>
                        </div>
                    </div>

                    <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                        <div className="mb-4 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-emerald-400">
                            <IconPackage size={16} />
                            Yield Modifiers
                            <DocsHelpLink
                                slug="projects/components/modifiers"
                                sectionId="yield"
                                title="Open yield modifiers docs in a new tab"
                            />
                        </div>
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                            <div className="flex flex-col gap-2">
                                <div className="flex items-center gap-1.5">
                                    <label className="text-sm font-medium text-slate-200">
                                        Input bonus
                                    </label>
                                    <DocsHelpLink
                                        slug="calculation/formulas"
                                        sectionId="yield-formulas"
                                        title="Open input yield formula docs in a new tab"
                                    />
                                </div>
                                <input
                                    type="number"
                                    step="any"
                                    value={inputPercent}
                                    onChange={(e) =>
                                        onInputPercentChange(e.target.value)
                                    }
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                />
                            </div>

                            <div className="flex flex-col gap-2">
                                <div className="flex items-center gap-1.5">
                                    <label className="text-sm font-medium text-slate-200">
                                        Output bonus
                                    </label>
                                    <DocsHelpLink
                                        slug="calculation/formulas"
                                        sectionId="yield-formulas"
                                        title="Open output yield formula docs in a new tab"
                                    />
                                </div>
                                <input
                                    type="number"
                                    step="any"
                                    value={outputPercent}
                                    onChange={(e) =>
                                        onOutputPercentChange(e.target.value)
                                    }
                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                    disabled={submitting}
                                />
                            </div>
                        </div>
                    </div>
                </div>

                <div className="rounded-xl border border-slate-800 bg-slate-900/30 p-4">
                    <div className="mb-4 flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-blue-400">
                            <IconSettings size={16} />
                            Attribute Bonuses
                            <DocsHelpLink
                                slug="projects/components/modifiers"
                                sectionId="attribute-contributions"
                                title="Open modifier attribute contribution docs in a new tab"
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

                    <div className="mt-3 flex min-w-0 flex-col gap-2">
                        {attributes.length === 0 && (
                            <div className="text-sm text-slate-500">
                                No attributes
                            </div>
                        )}

                        {attributes.map((row, idx) => (
                            <div
                                key={`${mode}-attr-${idx}`}
                                className="flex flex-col gap-1"
                            >
                                <label className="text-sm font-small text-slate-200">
                                    Attribute
                                </label>
                                <div className="flex flex-row gap-1">
                                    <div className="flex min-w-0 flex-1 flex-col">
                                        <AttributeDropDown
                                            value={row.puid}
                                            disabled={submitting}
                                            onSelect={(next) =>
                                                onAttributePuidChange(idx, next)
                                            }
                                        />

                                        <div className="mt-1 flex w-full flex-row gap-2 overflow-hidden">
                                            <div className="flex min-w-0 flex-1 flex-col gap-1">
                                                <label className="text-sm font-small text-slate-200">
                                                    Flat bonus
                                                </label>
                                                <input
                                                    value={String(
                                                        row.flatBonus,
                                                    )}
                                                    onChange={(e) =>
                                                        onAttributeFlatBonusChange(
                                                            idx,
                                                            Number(
                                                                e.target.value,
                                                            ),
                                                        )
                                                    }
                                                    inputMode="decimal"
                                                    placeholder="Flat"
                                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                    disabled={submitting}
                                                />
                                            </div>
                                            <div className="flex min-w-0 flex-1 flex-col gap-1">
                                                <label className="text-sm font-small text-slate-200">
                                                    Additive bonus
                                                </label>
                                                <input
                                                    value={String(
                                                        row.percentBonus,
                                                    )}
                                                    onChange={(e) =>
                                                        onAttributePercentBonusChange(
                                                            idx,
                                                            Number(
                                                                e.target.value,
                                                            ),
                                                        )
                                                    }
                                                    inputMode="decimal"
                                                    placeholder="Additive"
                                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                    disabled={submitting}
                                                />
                                            </div>
                                            <div className="flex min-w-0 flex-1 flex-col gap-1">
                                                <label className="text-sm font-small text-slate-200">
                                                    Multiplicative bonus
                                                </label>
                                                <input
                                                    value={String(
                                                        row.multiplicativeBonus,
                                                    )}
                                                    onChange={(e) =>
                                                        onAttributeMultiplicativeBonusChange(
                                                            idx,
                                                            Number(
                                                                e.target.value,
                                                            ),
                                                        )
                                                    }
                                                    inputMode="decimal"
                                                    placeholder="Multiplicative"
                                                    className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                                    disabled={submitting}
                                                />
                                            </div>
                                        </div>
                                    </div>

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
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </Popup>
    );
}
