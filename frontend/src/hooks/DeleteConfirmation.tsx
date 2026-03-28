"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

export type UseDeleteConfirmationOptions = {
    /**
     * Attribute used to identify the clickable delete confirmation element.
     * The hook will not cancel confirmation when clicks occur inside an element
     * matching `[confirmAttribute="true"]`.
     */
    confirmAttribute?: string;
    /**
     * When any of these values change, the pending confirmation is cleared.
     * Useful for route/project changes.
     */
    resetDeps?: readonly unknown[];
};

export type UseDeleteConfirmationResult<TId extends string> = {
    confirmingId: TId | null;
    isConfirming: (id: TId) => boolean;
    requestConfirmation: (id: TId) => void;
    reset: () => void;
    confirmOrRequest: (id: TId, onConfirm: () => void) => void;
    confirmSelector: string;
};

/**
 * Manages a two-click delete confirmation UX:
 * - first click highlights the target (pending confirmation)
 * - second click confirms and runs `onConfirm`
 * - clicking anywhere else cancels the pending confirmation
 */
export function useDeleteConfirmation<TId extends string = string>(
    options: UseDeleteConfirmationOptions = {},
): UseDeleteConfirmationResult<TId> {
    const { confirmAttribute = "data-delete-confirm", resetDeps = [] } =
        options;
    const [confirmingId, setConfirmingId] = useState<TId | null>(null);

    const confirmSelector = useMemo(
        () => `[${confirmAttribute}="true"]`,
        [confirmAttribute],
    );

    const reset = useCallback(() => {
        setConfirmingId(null);
    }, []);

    const isConfirming = useCallback(
        (id: TId) => confirmingId === id,
        [confirmingId],
    );

    const requestConfirmation = useCallback((id: TId) => {
        setConfirmingId(id);
    }, []);

    const confirmOrRequest = useCallback(
        (id: TId, onConfirm: () => void) => {
            if (confirmingId === id) {
                setConfirmingId(null);
                onConfirm();
                return;
            }

            setConfirmingId(id);
        },
        [confirmingId],
    );

    useEffect(() => {
        reset();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, resetDeps);

    useEffect(() => {
        if (!confirmingId) return;

        const handlePointerDown = (event: MouseEvent | TouchEvent) => {
            const target = event.target as HTMLElement | null;
            if (!target) return;

            const withinConfirmElement = target.closest(confirmSelector);
            if (withinConfirmElement) return;

            setConfirmingId(null);
        };

        document.addEventListener("mousedown", handlePointerDown);
        document.addEventListener("touchstart", handlePointerDown);
        return () => {
            document.removeEventListener("mousedown", handlePointerDown);
            document.removeEventListener("touchstart", handlePointerDown);
        };
    }, [confirmingId, confirmSelector]);

    return {
        confirmingId,
        isConfirming,
        requestConfirmation,
        reset,
        confirmOrRequest,
        confirmSelector,
    };
}
