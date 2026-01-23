"use client";

import React, { useEffect, useMemo, useState } from "react";
import { IconX } from "@tabler/icons-react";

export type ErrorDisplayItem =
    | string
    | {
          id?: string;
          message: React.ReactNode;
          onDismiss?: () => void;
      }
    | null
    | undefined
    | false;

export interface ErrorDisplayProps {
    errors?: ErrorDisplayItem[];
    className?: string;
}

type NormalizedError = {
    id: string;
    message: React.ReactNode;
    onDismiss?: () => void;
};

function normalizeErrors(
    errors: ErrorDisplayItem[] | undefined,
): NormalizedError[] {
    if (!errors || errors.length === 0) return [];

    const normalized: NormalizedError[] = [];
    for (let i = 0; i < errors.length; i++) {
        const item = errors[i];
        if (!item) continue;

        if (typeof item === "string") {
            const trimmed = item.trim();
            if (!trimmed) continue;
            normalized.push({ id: `${trimmed}::${i}`, message: trimmed });
            continue;
        }

        const id = item.id?.trim() ? item.id.trim() : `error::${i}`;
        normalized.push({
            id,
            message: item.message,
            onDismiss: item.onDismiss,
        });
    }

    return normalized;
}

export default function ErrorDisplay({ errors, className }: ErrorDisplayProps) {
    const items = useMemo(() => normalizeErrors(errors), [errors]);
    const [dismissedIds, setDismissedIds] = useState<Set<string>>(
        () => new Set(),
    );

    useEffect(() => {
        if (dismissedIds.size === 0) return;
        const activeIds = new Set(items.map((i) => i.id));
        let changed = false;
        const next = new Set<string>();
        for (const id of dismissedIds) {
            if (activeIds.has(id)) {
                next.add(id);
            } else {
                changed = true;
            }
        }
        if (changed) setDismissedIds(next);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [items]);

    const visible = items.filter((i) => !dismissedIds.has(i.id));
    if (visible.length === 0) return null;

    return (
        <div className={"flex flex-col gap-2 " + (className ?? "")}>
            {visible.map((err) => (
                <div
                    key={err.id}
                    className="rounded-xl border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200"
                >
                    <div className="flex items-center justify-between gap-3">
                        <div className="min-w-0 align-middle">
                            {err.message}
                        </div>
                        <button
                            type="button"
                            className="rounded-md p-1 text-red-200/90 transition-colors cursor-pointer hover:bg-red-900/20 hover:text-red-100 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                            onClick={() => {
                                setDismissedIds((prev) => {
                                    const next = new Set(prev);
                                    next.add(err.id);
                                    return next;
                                });
                                err.onDismiss?.();
                            }}
                            aria-label="Dismiss error"
                            title="Dismiss"
                        >
                            <IconX aria-hidden size={20} />
                        </button>
                    </div>
                </div>
            ))}
        </div>
    );
}
