"use client";

import React, {
    useCallback,
    useMemo,
    useState,
    useRef,
    useEffect,
} from "react";
import { createPortal } from "react-dom";
import { IconCheck, IconChevronDown, IconSearch } from "@tabler/icons-react";

export interface DropDownOption {
    value: string;
    label: React.ReactNode;
    searchText?: string;
    disabled?: boolean;
    endAdornment?: React.ReactNode;
}

interface DropDownProps {
    label: React.ReactNode;
    children?:
        | React.ReactNode
        | ((api: { close: () => void }) => React.ReactNode);
    mode?: "single" | "multi";
    options?: DropDownOption[];
    value?: string;
    values?: string[];
    onSelect?: (next: string) => void;
    onChangeValues?: (next: string[]) => void;
    enableSearch?: boolean;
    searchPlaceholder?: string;
    searchAriaLabel?: string;
    emptyFilteredText?: string;
    emptyOptionsText?: string;
    doneLabel?: string;
    checkIconSize?: number;
    optionClassName?: string;
    optionTextClassName?: string;
    searchInputClassName?: string;
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
    mode = "single",
    options,
    value,
    values,
    onSelect,
    onChangeValues,
    enableSearch = true,
    searchPlaceholder = "Search",
    searchAriaLabel = "Search options",
    emptyFilteredText = "No items match your search.",
    emptyOptionsText = "No items yet.",
    doneLabel = "Done",
    checkIconSize = 16,
    optionClassName,
    optionTextClassName,
    searchInputClassName,
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
    const [searchText, setSearchText] = useState("");
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
        setSearchText("");
        if (returnFocus) {
            triggerRef.current?.focus({ preventScroll: true });
        }
        setOpen(false);
    };

    const hasOptionMode = Array.isArray(options);

    const selectedSet = useMemo(() => {
        if (mode === "multi") {
            return new Set(values ?? []);
        }
        return new Set(value ? [value] : []);
    }, [mode, value, values]);

    const normalizedSearch = searchText.trim().toLowerCase();

    const filteredOptions = useMemo(() => {
        if (!hasOptionMode) return [];
        if (!normalizedSearch) return options;

        return options.filter((option) => {
            const optionSearchText = (
                option.searchText ??
                (typeof option.label === "string" ? option.label : "")
            )
                .toString()
                .toLowerCase();
            return optionSearchText.includes(normalizedSearch);
        });
    }, [hasOptionMode, normalizedSearch, options]);

    const handleOptionClick = useCallback(
        (nextValue: string) => {
            if (mode === "multi") {
                const next = new Set(values ?? []);
                if (next.has(nextValue)) next.delete(nextValue);
                else next.add(nextValue);
                onChangeValues?.(Array.from(next));
                return;
            }

            onSelect?.(nextValue);
            closeMenu({ returnFocus: true });
        },
        [mode, onChangeValues, onSelect, values],
    );

    useEffect(() => {
        if (!open) return;

        function handlePointerDown(event: PointerEvent) {
            const target = event.target as Node | null;
            if (!target) return;
            const withinTrigger = triggerRef.current?.contains(target) ?? false;
            const withinMenu = menuPanelRef.current?.contains(target) ?? false;
            if (!withinTrigger && !withinMenu) {
                closeMenu();
            }
        }

        function handleKeyDown(event: KeyboardEvent) {
            if (event.key === "Escape") {
                closeMenu({ returnFocus: true });
            }
        }

        document.addEventListener("pointerdown", handlePointerDown);
        document.addEventListener("keydown", handleKeyDown);
        return () => {
            document.removeEventListener("pointerdown", handlePointerDown);
            document.removeEventListener("keydown", handleKeyDown);
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

    const renderedOptions = hasOptionMode ? (
        <div className="p-2">
            <div className="flex flex-col gap-1">
                {enableSearch && (
                    <div className="sticky top-0 z-10 -mx-2 -mt-2 border-b border-slate-800 bg-slate-950/95 px-2 pb-2 pt-2 backdrop-blur supports-backdrop-filter:bg-slate-950/80">
                        <div className="flex items-center gap-2 rounded-lg bg-slate-950/80 p-1">
                            <div className="text-slate-400">
                                <IconSearch size={16} />
                            </div>
                            <input
                                value={searchText}
                                onChange={(e) => setSearchText(e.target.value)}
                                placeholder={searchPlaceholder}
                                className={`w-full rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40 ${
                                    searchInputClassName ?? ""
                                }`}
                                disabled={disabled}
                                aria-label={searchAriaLabel}
                            />
                        </div>
                    </div>
                )}

                {filteredOptions.map((option) => {
                    const selected = selectedSet.has(option.value);
                    return (
                        <button
                            key={option.value}
                            type="button"
                            disabled={option.disabled}
                            className={`group flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors cursor-pointer hover:bg-slate-800/70 disabled:cursor-not-allowed disabled:opacity-50 ${
                                selected
                                    ? "bg-purple-600/15 text-slate-100"
                                    : "text-slate-200"
                            } ${optionClassName ?? ""}`}
                            onClick={() => handleOptionClick(option.value)}
                        >
                            <span
                                className={`min-w-0 truncate ${
                                    optionTextClassName ?? ""
                                }`}
                            >
                                {option.label}
                            </span>
                            <span
                                className={`shrink-0 ${
                                    selected
                                        ? "text-purple-300"
                                        : "text-slate-500 opacity-0 group-hover:opacity-100"
                                }`}
                                aria-hidden="true"
                            >
                                {option.endAdornment ?? (
                                    <IconCheck size={checkIconSize} />
                                )}
                            </span>
                        </button>
                    );
                })}

                {options.length > 0 && filteredOptions.length === 0 && (
                    <div className="px-3 py-2 text-sm text-slate-400">
                        {emptyFilteredText}
                    </div>
                )}

                {options.length === 0 && (
                    <div className="px-3 py-2 text-sm text-slate-400">
                        {emptyOptionsText}
                    </div>
                )}

                {mode === "multi" && (
                    <div className="mt-1 flex items-center justify-end border-t border-slate-800 pt-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => closeMenu({ returnFocus: true })}
                        >
                            {doneLabel}
                        </button>
                    </div>
                )}
            </div>
        </div>
    ) : (
        renderedChildren
    );

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
                        } ${matchTriggerWidth ? "" : "min-w-40"} overflow-hidden ${
                            menuClassName ?? ""
                        }`}
                        role="menu"
                        aria-orientation="vertical"
                        aria-hidden={!open}
                    >
                        {renderedOptions}
                    </div>,
                    document.body,
                )}
        </div>
    );
}
