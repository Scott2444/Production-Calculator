"use client";

import React, { useState, useRef, useEffect } from "react";
import { IconChevronDown } from "@tabler/icons-react";

interface DropDownProps {
  label: React.ReactNode;
  children: React.ReactNode | ((api: { close: () => void }) => React.ReactNode);
  align?: "left" | "right";
  disabled?: boolean;
  className?: string;
  buttonClassName?: string;
  menuClassName?: string;
  matchTriggerWidth?: boolean;
}

export default function DropDown({
  label,
  children,
  align = "right",
  disabled = false,
  className,
  buttonClassName,
  menuClassName,
  matchTriggerWidth = false,
}: DropDownProps): React.ReactElement {
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuPanelRef = useRef<HTMLDivElement>(null);

  const closeMenu = ({ returnFocus = false }: { returnFocus?: boolean } = {}) => {
    if (returnFocus) {
      triggerRef.current?.focus({ preventScroll: true });
    }
    setOpen(false);
  };

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        closeMenu();
      }
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

  const renderedChildren =
    typeof children === "function" ? children({ close: () => closeMenu({ returnFocus: true }) }) : children;

  const menuPositionClass = matchTriggerWidth
    ? "left-0 right-0"
    : align === "right"
      ? "right-0"
      : "left-0";

  return (
    <div className={`relative inline-block text-left ${className ?? ""}`} ref={menuRef}>
      <button
        ref={triggerRef}
        type="button"
        className={`flex w-full items-center justify-between gap-3 rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-3 text-left shadow-sm transition-all cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60 ${
          buttonClassName ?? ""
        }`}
        onClick={() => setOpen((prev) => !prev)}
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
      <div
        ref={menuPanelRef}
        className={`absolute z-50 mt-2 ${menuPositionClass} ${
          matchTriggerWidth ? "" : "min-w-40"
        } overflow-hidden rounded-xl border border-slate-700 bg-slate-950 shadow-lg transition-all duration-200 ease-in-out transform ${
          open ? "opacity-100 scale-100 pointer-events-auto" : "opacity-0 scale-95 pointer-events-none"
        } ${menuClassName ?? ""}`}
        role="menu"
        aria-orientation="vertical"
        aria-hidden={!open}
      >
        {renderedChildren}
      </div>
    </div>
  );
}
