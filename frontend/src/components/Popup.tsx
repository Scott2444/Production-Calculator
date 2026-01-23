"use client";

import React, { useEffect, useId, useMemo, useRef } from "react";
import { createPortal } from "react-dom";
import { IconX } from "@tabler/icons-react";

type PopupSize = "sm" | "md" | "lg";

export interface PopupProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    title?: React.ReactNode;
    description?: React.ReactNode;
    children: React.ReactNode;
    footer?: React.ReactNode;
    size?: PopupSize;
    closeOnBackdrop?: boolean;
    closeOnEscape?: boolean;
    showCloseButton?: boolean;
    initialFocusRef?: React.RefObject<HTMLElement | null>;
    className?: string;
    overlayClassName?: string;
    panelClassName?: string;
}

function getFocusableElements(container: HTMLElement): HTMLElement[] {
    const selector = [
        "a[href]",
        "button:not([disabled])",
        "textarea:not([disabled])",
        "input:not([disabled])",
        "select:not([disabled])",
        "[tabindex]:not([tabindex='-1'])",
    ].join(",");

    return Array.from(container.querySelectorAll<HTMLElement>(selector)).filter(
        (el) => {
            if (el.hasAttribute("disabled")) return false;
            if (el.getAttribute("aria-hidden") === "true") return false;
            return el.offsetParent !== null;
        },
    );
}

export default function Popup({
    open,
    onOpenChange,
    title,
    description,
    children,
    footer,
    size = "md",
    closeOnBackdrop = true,
    closeOnEscape = true,
    showCloseButton = true,
    initialFocusRef,
    className,
    overlayClassName,
    panelClassName,
}: PopupProps): React.ReactElement | null {
    const titleId = useId();
    const descriptionId = useId();
    const panelRef = useRef<HTMLDivElement>(null);
    const lastActiveElementRef = useRef<HTMLElement | null>(null);

    const sizeClass = useMemo(() => {
        switch (size) {
            case "sm":
                return "max-w-sm";
            case "lg":
                return "max-w-3xl";
            case "md":
            default:
                return "max-w-xl";
        }
    }, [size]);

    const close = () => onOpenChange(false);

    useEffect(() => {
        if (!open) return;

        lastActiveElementRef.current =
            document.activeElement as HTMLElement | null;
        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";

        const focusTarget = initialFocusRef?.current;
        if (focusTarget) {
            focusTarget.focus({ preventScroll: true });
        } else {
            // Wait for portal content to mount, then focus.
            queueMicrotask(() => {
                const panel = panelRef.current;
                if (!panel) return;
                const focusables = getFocusableElements(panel);
                (focusables[0] ?? panel).focus({ preventScroll: true });
            });
        }

        return () => {
            document.body.style.overflow = previousOverflow;
            lastActiveElementRef.current?.focus?.({ preventScroll: true });
            lastActiveElementRef.current = null;
        };
    }, [open, initialFocusRef]);

    if (!open) return null;
    if (typeof document === "undefined") return null;

    return createPortal(
        <div
            className={`fixed inset-0 z-50 flex items-center justify-center p-4 ${className ?? ""}`}
            role="presentation"
        >
            <div
                className={`absolute inset-0 bg-black/60 backdrop-blur-[1px] ${overlayClassName ?? ""}`}
                onMouseDown={() => {
                    if (closeOnBackdrop) close();
                }}
                aria-hidden="true"
            />

            <div
                ref={panelRef}
                role="dialog"
                aria-modal="true"
                aria-labelledby={title ? titleId : undefined}
                aria-describedby={description ? descriptionId : undefined}
                tabIndex={-1}
                className={`relative flex w-full max-h-[calc(100dvh-2rem)] ${sizeClass} flex-col overflow-hidden rounded-xl border border-slate-700 bg-slate-950 text-slate-200 shadow-lg ${
                    panelClassName ?? ""
                }`}
                onMouseDown={(e) => {
                    // Prevent backdrop handler from firing when clicking inside.
                    e.stopPropagation();
                }}
                onKeyDown={(e) => {
                    if (e.key === "Escape" && closeOnEscape) {
                        e.stopPropagation();
                        close();
                        return;
                    }

                    if (e.key !== "Tab") return;
                    const panel = panelRef.current;
                    if (!panel) return;

                    const focusables = getFocusableElements(panel);
                    if (focusables.length === 0) {
                        e.preventDefault();
                        panel.focus({ preventScroll: true });
                        return;
                    }

                    const first = focusables[0];
                    const last = focusables[focusables.length - 1];
                    const active = document.activeElement as HTMLElement | null;

                    if (e.shiftKey) {
                        if (
                            !active ||
                            active === first ||
                            !panel.contains(active)
                        ) {
                            e.preventDefault();
                            last.focus();
                        }
                    } else {
                        if (active === last) {
                            e.preventDefault();
                            first.focus();
                        }
                    }
                }}
            >
                {(title || showCloseButton) && (
                    <div className="flex shrink-0 items-start justify-between gap-4 border-b border-slate-800 px-4 py-3">
                        <div className="min-w-0">
                            {title && (
                                <div
                                    id={titleId}
                                    className="truncate text-base font-semibold text-slate-100"
                                >
                                    {title}
                                </div>
                            )}
                            {description && (
                                <div
                                    id={descriptionId}
                                    className="mt-0.5 text-sm text-slate-400"
                                >
                                    {description}
                                </div>
                            )}
                        </div>

                        {showCloseButton && (
                            <button
                                type="button"
                                onClick={close}
                                className="shrink-0 rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-slate-300 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 hover:text-slate-100 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                                aria-label="Close"
                            >
                                <IconX size={16} />
                            </button>
                        )}
                    </div>
                )}

                <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4">
                    {children}
                </div>

                {footer && (
                    <div className="shrink-0 border-t border-slate-800 px-4 py-3">
                        {footer}
                    </div>
                )}
            </div>
        </div>,
        document.body,
    );
}
