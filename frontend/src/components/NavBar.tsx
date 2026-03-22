"use client";

import React, { useEffect, useRef, useState } from "react";
import Image from "next/image";
import { Link, useRouterState } from "@tanstack/react-router";
import { useAuth } from "@/context/AuthContext";
import { useProtectedApi } from "@/lib/api";
import { useQuery } from "@tanstack/react-query";
import { fetchUser } from "@/lib/user";
import {
    IconSettings,
    IconLogout,
    IconSquareRoundedCheck,
} from "@tabler/icons-react";
import { useLogout } from "@/lib/logout";

function getCurrentNavSection(pathname: string) {
    if (pathname === "/") return "Home";

    const firstSlug = pathname.split("/").filter(Boolean)[0]?.toLowerCase();
    if (firstSlug === "explore") return "Explore";
    if (firstSlug === "docs") return "Docs";

    if (
        firstSlug === "settings" ||
        firstSlug === "verify" ||
        firstSlug === "login"
    )
        return "";

    // Anything else at the top-level is treated as a username route.
    return "Projects";
}

type NavBarProps = {
    currentPage?: string;
};

export default function NavBar({
    currentPage,
}: NavBarProps): React.ReactElement {
    const pathname = useRouterState({
        select: (state) => state.location.pathname,
    });
    const derivedCurrentPage = currentPage ?? getCurrentNavSection(pathname);
    const { loggedIn, isHydrated } = useAuth();
    const accountLogoUrl = "/assets/Default_Avatar.svg";

    const { userId } = useAuth();
    const protectedApi = useProtectedApi();
    const { data: user } = useQuery({
        queryKey: ["user", userId],
        queryFn: () => fetchUser(userId!, protectedApi),
        staleTime: 5 * 60 * 1000, // 5 minutes
        enabled: Boolean(userId),
    });

    const navItems = [
        { name: "Home", href: "/" },
        { name: "Projects", href: `/${user?.username ?? ""}` },
        { name: "Explore", href: "/explore" },
        { name: "Docs", href: "/docs" },
    ];

    return (
        <nav className="flex items-center justify-between py-5 px-8 border-b-2 border-black bg-slate-900/90 sticky top-0 z-50">
            <div className="flex items-center gap-14 text-xl">
                <Link to="/">
                    <Image
                        src="/assets/Medium_Logo.svg"
                        alt="Logo"
                        width={128}
                        height={32}
                    />
                </Link>
                {/* Navigation buttons */}
                {navItems.map((item) => (
                    // Determine active state from the first slug in the route hierarchy.
                    <Link
                        key={item.name}
                        to={item.href}
                        className={`mr-4 no-underline transition-colors duration-200 ${
                            derivedCurrentPage === item.name
                                ? "text-slate-200 font-medium"
                                : "text-slate-300 font-medium hover:text-purple-400 hover:scale-105"
                        } `}
                    >
                        {item.name}
                    </Link>
                ))}
            </div>
            {/* Drop down menu */}
            <div className="w-28 flex justify-end">
                {!isHydrated ? (
                    <div
                        aria-hidden="true"
                        className="h-10 w-24 rounded-md bg-slate-700/60"
                    />
                ) : loggedIn ? (
                    <AccountDropdown
                        accountLogoUrl={accountLogoUrl}
                        user={user}
                    />
                ) : (
                    <Link
                        to="/login"
                        className="px-6 py-2 bg-purple-700 text-white rounded-md no-underline font-medium transition-colors duration-200 hover:bg-purple-600 hover:scale-105 shadow-md hover:shadow-lg"
                    >
                        Login
                    </Link>
                )}
            </div>
        </nav>
    );
}

type NavUser = {
    profilePictureUrl?: string;
    name?: string;
    username?: string;
    email?: string;
    isVerified?: boolean;
};

function AccountDropdown({
    accountLogoUrl,
    user,
}: {
    accountLogoUrl: string;
    user?: NavUser;
}) {
    const [menuOpen, setMenuOpen] = useState(false);
    const triggerRef = useRef<HTMLButtonElement>(null);
    const menuRef = useRef<HTMLDivElement>(null);
    const logout = useLogout();

    // Close dropdown on outside click
    useEffect(() => {
        if (!menuOpen) return;

        const handlePointerDown = (e: PointerEvent) => {
            const target = e.target as Node | null;
            if (!target) return;
            const withinTrigger = triggerRef.current?.contains(target) ?? false;
            const withinMenu = menuRef.current?.contains(target) ?? false;
            if (!withinTrigger && !withinMenu) {
                setMenuOpen(false);
            }
        };

        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape") {
                setMenuOpen(false);
            }
        };

        document.addEventListener("pointerdown", handlePointerDown);
        document.addEventListener("keydown", handleKeyDown);
        return () => {
            document.removeEventListener("pointerdown", handlePointerDown);
            document.removeEventListener("keydown", handleKeyDown);
        };
    }, [menuOpen]);

    return (
        <>
            <button
                ref={triggerRef}
                type="button"
                aria-haspopup="true"
                aria-expanded={menuOpen}
                onClick={() => setMenuOpen((open) => !open)}
                className="flex items-center focus:outline-none"
            >
                <Image
                    src={accountLogoUrl}
                    alt="Account"
                    width={36}
                    height={36}
                    className="rounded-full object-cover border border-gray-300 transition-transform duration-200 hover:scale-105 hover:border-purple-400"
                />
            </button>
            {menuOpen && (
                <div
                    ref={menuRef}
                    className="absolute right-0 top-15 mt-2 mr-4 w-70 bg-slate-50 rounded-md shadow-lg border border-gray-200 z-50 animate-fade-in"
                >
                    {/* User info section */}
                    <div className="flex items-center gap-3 px-4 py-3 border-b border-gray-100">
                        <Image
                            src={user?.profilePictureUrl || accountLogoUrl}
                            alt="Profile"
                            width={40}
                            height={40}
                            className="rounded-full object-cover border border-gray-300"
                            unoptimized={Boolean(user?.profilePictureUrl)}
                        />
                        <div className="flex flex-col min-w-0">
                            <span className="font-medium text-gray-900 truncate">
                                {user?.name || user?.username || "User"}
                            </span>
                            <span className="text-sm text-gray-500 truncate">
                                {user?.email || ""}
                            </span>
                        </div>
                    </div>
                    {/* Verify, Settings, Logout */}
                    <div className="flex flex-col gap-1 px-2 py-2">
                        {user && !user.isVerified && (
                            <Link
                                to="/verify"
                                className="px-4 py-2 text-gray-800 no-underline transition-all duration-150 rounded-xl hover:bg-purple-100 hover:text-purple-700 flex items-center gap-2"
                                onClick={() => setMenuOpen(false)}
                            >
                                <span className="inline-block">
                                    <IconSquareRoundedCheck size={18} />
                                </span>
                                <span>Verify Email</span>
                            </Link>
                        )}
                        <Link
                            to="/settings"
                            className="px-4 py-2 text-gray-800 no-underline transition-all duration-150 rounded-xl hover:bg-purple-100 hover:text-purple-700 flex items-center gap-2"
                            onClick={() => setMenuOpen(false)}
                        >
                            <span className="inline-block">
                                <IconSettings size={18} />
                            </span>
                            <span>Settings</span>
                        </Link>
                        <button
                            className="w-full text-left px-4 py-2 text-gray-800 transition-all duration-150 rounded-xl cursor-pointer hover:bg-purple-100 hover:text-purple-700 border-t border-white flex items-center gap-2"
                            style={{ marginTop: "2px" }}
                            onClick={() => {
                                setMenuOpen(false);
                                logout();
                            }}
                        >
                            <span className="inline-block">
                                <IconLogout size={18} />
                            </span>
                            <span>Sign Out</span>
                        </button>
                    </div>
                </div>
            )}
        </>
    );
}
