"use client";

import React, {
    useCallback,
    useMemo,
    useState,
    useRef,
    useEffect,
} from "react";
import { createPortal } from "react-dom";
import { IconChevronDown } from "@tabler/icons-react";

interface DropDownProps {
    label: React.ReactNode;
    children:
        | React.ReactNode
        | ((api: { close: () => void }) => React.ReactNode);
    align?: "left" | "right";
    placement?: "auto" | "top" | "bottom";
    disabled?: boolean;
    className?: string;
    buttonClassName?: string;
    menuClassName?: string;
    matchTriggerWidth?: boolean;
    maxMenuHeightPx?: number;
}

export default function DropDown({
    label,
    children,
    align = "right",
    placement = "auto",
    disabled = false,
    className,
    buttonClassName,
    menuClassName,
    matchTriggerWidth = false,
    maxMenuHeightPx = 320,
}: DropDownProps): React.ReactElement {
    const [open, setOpen] = useState(false);
    const [renderMenu, setRenderMenu] = useState(false);
    const menuRef = useRef<HTMLDivElement>(null);
    const triggerRef = useRef<HTMLButtonElement>(null);
    const menuPanelRef = useRef<HTMLDivElement>(null);
    const [resolvedPlacement, setResolvedPlacement] = useState<
        "top" | "bottom"
    >("bottom");
    const [menuStyle, setMenuStyle] = useState<React.CSSProperties>({
        position: "fixed",
        zIndex: 1000,
        top: 0,
        left: 0,
        maxHeight: maxMenuHeightPx,
        overflowY: "auto",
        overscrollBehavior: "contain",
    });

    const computeMenuStyle = useCallback((): {
        nextPlacement: "top" | "bottom";
        style: React.CSSProperties;
    } | null => {
        const trigger = triggerRef.current;
        if (!trigger || typeof window === "undefined") return null;

        const rect = trigger.getBoundingClientRect();
        const viewportHeight = window.innerHeight;
        const viewportWidth = window.innerWidth;
        const viewportPadding = 8;
        const gap = 8;

        const spaceBelow = viewportHeight - rect.bottom - viewportPadding;
        const spaceAbove = rect.top - viewportPadding;

        const wantBottom =
            placement === "bottom"
                ? true
                : placement === "top"
                  ? false
                  : spaceBelow >= 220 || spaceBelow >= spaceAbove;

        const nextPlacement: "top" | "bottom" = wantBottom ? "bottom" : "top";

        const available =
            (nextPlacement === "bottom" ? spaceBelow : spaceAbove) - gap;
        const maxHeight = Math.max(120, Math.min(maxMenuHeightPx, available));

        const base: React.CSSProperties = {
            position: "fixed",
            zIndex: 1000,
            maxHeight,
            overflowY: "auto",
            overscrollBehavior: "contain",
        };

        const horizontal: React.CSSProperties = matchTriggerWidth
            ? {
                  width: rect.width,
                  ...(align === "right"
                      ? { right: Math.max(0, viewportWidth - rect.right) }
                      : { left: Math.max(0, rect.left) }),
              }
            : align === "right"
              ? { right: Math.max(0, viewportWidth - rect.right) }
              : { left: Math.max(0, rect.left) };

        const vertical: React.CSSProperties =
            nextPlacement === "bottom"
                ? { top: rect.bottom + gap }
                : { bottom: viewportHeight - rect.top + gap };

        return {
            nextPlacement,
            style: { ...base, ...horizontal, ...vertical },
        };
    }, [align, matchTriggerWidth, maxMenuHeightPx, placement]);

    const closeMenu = ({
        returnFocus = false,
    }: { returnFocus?: boolean } = {}) => {
        if (returnFocus) {
            triggerRef.current?.focus({ preventScroll: true });
        }
        setOpen(false);
    };

    useEffect(() => {
        function handleClickOutside(event: MouseEvent) {
            const target = event.target as Node;
            const withinTrigger = triggerRef.current?.contains(target) ?? false;
            const withinMenu = menuPanelRef.current?.contains(target) ?? false;
            if (!withinTrigger && !withinMenu) closeMenu();
        }
        if (open) {
            document.addEventListener("mousedown", handleClickOutside);
        } else {
            document.removeEventListener("mousedown", handleClickOutside);
        }
        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
        };
    }, [open]);

    useEffect(() => {
        if (!menuPanelRef.current) return;
        (menuPanelRef.current as unknown as { inert: boolean }).inert = !open;
    }, [open]);

    useEffect(() => {
        if (open) {
            setRenderMenu(true);
            return;
        }

        const t = window.setTimeout(() => setRenderMenu(false), 220);
        return () => window.clearTimeout(t);
    }, [open]);

    useEffect(() => {
        if (!open) return;

        const updatePosition = () => {
            const computed = computeMenuStyle();
            if (!computed) return;
            setResolvedPlacement(computed.nextPlacement);
            setMenuStyle(computed.style);
        };

        updatePosition();
        window.addEventListener("resize", updatePosition);
        window.addEventListener("scroll", updatePosition, true);
        return () => {
            window.removeEventListener("resize", updatePosition);
            window.removeEventListener("scroll", updatePosition, true);
        };
    }, [open, computeMenuStyle]);

    const renderedChildren =
        typeof children === "function"
            ? children({ close: () => closeMenu({ returnFocus: true }) })
            : children;

    const originClass = useMemo(() => {
        if (resolvedPlacement === "top") {
            return align === "right"
                ? "origin-bottom-right"
                : "origin-bottom-left";
        }
        return align === "right" ? "origin-top-right" : "origin-top-left";
    }, [align, resolvedPlacement]);

    return (
        <div
            className={`relative inline-block text-left ${className ?? ""}`}
            ref={menuRef}
        >
            <button
                ref={triggerRef}
                type="button"
                className={`flex w-full items-center justify-between gap-3 rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-3 text-left shadow-sm transition-all cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60 ${
                    buttonClassName ?? ""
                }`}
                onClick={() => {
                    setOpen((prev) => {
                        const next = !prev;
                        if (next) {
                            const computed = computeMenuStyle();
                            if (computed) {
                                setResolvedPlacement(computed.nextPlacement);
                                setMenuStyle(computed.style);
                            }
                        }
                        return next;
                    });
                }}
                disabled={disabled}
                aria-haspopup="true"
                aria-expanded={open}
                aria-disabled={disabled}
            >
                {label}
                <IconChevronDown
                    className={`h-4 w-4 shrink-0 text-slate-400 transition-transform duration-200 ${
                        open ? "rotate-180" : "rotate-0"
                    }`}
                />
            </button>

            {renderMenu &&
                typeof document !== "undefined" &&
                createPortal(
                    <div
                        ref={menuPanelRef}
                        style={menuStyle}
                        className={`rounded-xl border border-slate-700 bg-slate-950 shadow-lg transition-all duration-200 ease-in-out transform ${originClass} ${
                            open
                                ? "opacity-100 scale-100 pointer-events-auto"
                                : "opacity-0 scale-95 pointer-events-none"
                        } ${matchTriggerWidth ? "" : "min-w-40"} ${
                            menuClassName ?? ""
                        }`}
                        role="menu"
                        aria-orientation="vertical"
                        aria-hidden={!open}
                    >
                        {renderedChildren}
                    </div>,
                    document.body,
                )}
        </div>
    );
}
