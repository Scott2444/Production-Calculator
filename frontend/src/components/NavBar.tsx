"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
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

type NavBarProps = {
    currentPage?: "Home" | "Projects" | "Explore" | "Docs";
};

function getCurrentNavSection(
    pathname: string,
): NonNullable<NavBarProps["currentPage"]> {
    if (pathname === "/") return "Home";

    const firstSlug = pathname.split("/").filter(Boolean)[0]?.toLowerCase();
    if (firstSlug === "explore") return "Explore";
    if (firstSlug === "docs") return "Docs";

    // Anything else at the top-level is treated as a username route.
    return "Projects";
}

export default function NavBar({
    currentPage,
}: NavBarProps): React.ReactElement {
    const pathname = usePathname();
    const derivedCurrentPage = currentPage ?? getCurrentNavSection(pathname);
    const { loggedIn } = useAuth();
    const [accountLogoUrl, setAccountLogoUrl] = useState<string>(
        "/Default_Avatar.svg",
    );
    const { userId } = useAuth();
    const protectedApi = useProtectedApi();
    const {
        data: user,
        isLoading,
        error,
    } = useQuery({
        queryKey: ["user", userId],
        queryFn: () => fetchUser(userId!, protectedApi),
        staleTime: 5 * 60 * 1000, // 5 minutes
        enabled: Boolean(userId),
    });

    useEffect(() => {
        // Example: fetch account logo from API or context
        // Replace with your actual logic
        const fetchLogo = async () => {
            // Simulate async fetch
            // setAccountLogoUrl(await getLogoUrl());
            // For now, keep default
        };
        fetchLogo();
    }, []);

    const navItems = [
        { name: "Home", href: "/" },
        { name: "Projects", href: `/${user?.username ?? ""}` },
        { name: "Explore", href: "/explore" },
        { name: "Docs", href: "/docs" },
    ];

    return (
        <nav className="flex items-center justify-between py-5 px-8 border-b-2 border-black bg-slate-900/80">
            <div className="flex items-center gap-14 text-xl">
                <Link href="/">
                    <img src="/Medium_Logo.svg" alt="Logo" className="h-8" />
                </Link>
                {/* Navigation buttons */}
                {navItems.map((item) => (
                    // Determine active state from the first slug in the route hierarchy.
                    <Link
                        key={item.name}
                        href={item.href}
                        className={`mr-4 no-underline transition-colors duration-200 ${
                            derivedCurrentPage === item.name
                                ? "text-slate-200 font-medium"
                                : "text-slate-200 font-light hover:text-purple-400 hover:scale-105"
                        } `}
                    >
                        {item.name}
                    </Link>
                ))}
            </div>
            {/* Drop down menu */}
            <div>
                {loggedIn ? (
                    <AccountDropdown
                        accountLogoUrl={accountLogoUrl}
                        user={user}
                    />
                ) : (
                    <Link
                        href="/login"
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
    const logout = useLogout();

    // Close dropdown on outside click
    useEffect(() => {
        if (!menuOpen) return;
        const handleClick = (e: MouseEvent) => {
            const target = e.target as HTMLElement;
            if (
                !target.closest("#account-menu-btn") &&
                !target.closest("#account-dropdown")
            ) {
                setMenuOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClick);
        return () => document.removeEventListener("mousedown", handleClick);
    }, [menuOpen]);

    return (
        <>
            <button
                id="account-menu-btn"
                type="button"
                aria-haspopup="true"
                aria-expanded={menuOpen}
                onClick={() => setMenuOpen((open) => !open)}
                className="flex items-center focus:outline-none"
            >
                <img
                    src={accountLogoUrl}
                    alt="Account"
                    className="w-9 h-9 rounded-full object-cover border border-gray-300 transition-transform duration-200 hover:scale-105 hover:border-purple-400"
                />
            </button>
            {menuOpen && (
                <div
                    id="account-dropdown"
                    className="absolute right-0 mt-2 mr-4 w-70 bg-slate-50 rounded-md shadow-lg border border-gray-200 z-50 animate-fade-in"
                >
                    {/* User info section */}
                    <div className="flex items-center gap-3 px-4 py-3 border-b border-gray-100">
                        <img
                            src={user?.profilePictureUrl || accountLogoUrl}
                            alt="Profile"
                            className="w-10 h-10 rounded-full object-cover border border-gray-300"
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
                                href="/verify"
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
                            href="/settings"
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
