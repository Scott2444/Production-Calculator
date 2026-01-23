import type { ReactNode, RefObject } from "react";
import Popup from "./Popup";

export interface ItemCardProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    title?: ReactNode;
    description?: ReactNode;
    initialFocusRef?: RefObject<HTMLElement | null>;
    children: ReactNode;

    submitLabel: string;
    submittingLabel?: string;
    submitting?: boolean;
    submitDisabled?: boolean;
    onSubmit: () => void;

    cancelLabel?: string;
    cancelDisabled?: boolean;
    onCancel?: () => void;
}

export default function ItemCard({
    open,
    onOpenChange,
    title,
    description,
    initialFocusRef,
    children,
    submitLabel,
    submittingLabel,
    submitting,
    submitDisabled,
    onSubmit,
    cancelLabel = "Cancel",
    cancelDisabled,
    onCancel,
}: ItemCardProps) {
    const isSubmitting = Boolean(submitting);
    const handleCancel = () => {
        if (onCancel) onCancel();
        else onOpenChange(false);
    };

    return (
        <Popup
            open={open}
            onOpenChange={onOpenChange}
            title={title}
            description={description}
            initialFocusRef={initialFocusRef}
            footer={
                <div className="flex items-center justify-end gap-2">
                    <button
                        type="button"
                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        onClick={handleCancel}
                        disabled={Boolean(cancelDisabled) || isSubmitting}
                    >
                        {cancelLabel}
                    </button>
                    <button
                        type="button"
                        className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                        onClick={onSubmit}
                        disabled={Boolean(submitDisabled) || isSubmitting}
                    >
                        {isSubmitting
                            ? (submittingLabel ?? submitLabel)
                            : submitLabel}
                    </button>
                </div>
            }
        >
            {children}
        </Popup>
    );
}
