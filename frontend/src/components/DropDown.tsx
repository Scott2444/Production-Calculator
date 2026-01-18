"use client";

import React, { useState, useRef, useEffect } from "react";

interface DropDownProps {
  label: React.ReactNode;
  children: React.ReactNode;
  align?: "left" | "right";
}

export default function DropDown({ label, children, align = "right" }: DropDownProps): React.ReactElement {
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpen(false);
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

  return (
    <div className="relative inline-block text-left" ref={menuRef}>
      <button
        type="button"
        className="flex items-center gap-2 px-4 py-2 bg-slate-800 text-slate-200 rounded-md shadow-md hover:bg-slate-700 transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-purple-500"
        onClick={() => setOpen((prev) => !prev)}
        aria-haspopup="true"
        aria-expanded={open}
      >
        {label}
        <svg
          className={`w-4 h-4 transition-transform duration-200 ${open ? "rotate-180" : "rotate-0"}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      <div
        className={`absolute z-50 mt-2 min-w-[10rem] ${
          align === "right" ? "right-0" : "left-0"
        } bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg overflow-hidden transition-all duration-200 ease-in-out transform ${
          open
            ? "opacity-100 scale-100 pointer-events-auto"
            : "opacity-0 scale-95 pointer-events-none"
        }`}
        role="menu"
        aria-orientation="vertical"
        aria-hidden={!open}
      >
        {children}
      </div>
    </div>
  );
}
