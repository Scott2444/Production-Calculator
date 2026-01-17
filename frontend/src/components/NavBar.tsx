"use client";

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';

export default function NavBar(): React.ReactElement {
    const pathname = usePathname();
    const { loggedIn } = useAuth();
    const [accountLogoUrl, setAccountLogoUrl] = useState<string>('/Default_Avatar.svg');

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
        { name: 'Home', href: '/' },
        { name: 'Projects', href: '/projects' },
        { name: 'Explore', href: '/explore' },
        { name: 'Docs', href: '/docs' },
    ];

    return (
        <nav className="flex items-center justify-between py-5 px-8 border-b-2 border-black bg-slate-900/80">
            <div className="flex items-center gap-14 text-xl">
                <Link href="/">
                    <img src="/Medium_Logo.svg" alt="Logo" className="h-8" />
                </Link>
                {/* Navigation buttons */}
                    {navItems.map((item) => (
                        <Link
                            key={item.name}
                            href={item.href}
                            className={`mr-4 no-underline transition-colors duration-200 ${
                                pathname === item.href
                                    ? 'text-slate-200 font-medium'
                                    : 'text-slate-200 font-light hover:text-purple-400 hover:scale-105'
                            } `}
                        >
                            {item.name}
                        </Link>
                    ))}
            </div>
            {/* Drop down menu */}
            <div>
                    {loggedIn ? (
                        <Link href="/settings" className="flex items-center no-underline">
                            <img
                                src={accountLogoUrl}
                                alt="Account"
                                className="w-9 h-9 rounded-full object-cover border border-gray-300 transition-transform duration-200 hover:scale-105 hover:border-purple-400"
                            />
                        </Link>
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