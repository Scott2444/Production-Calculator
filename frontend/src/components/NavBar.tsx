import NavBarProps from '@/types/NavBar';
import React from 'react';
import Link from 'next/link';

export default function NavBar({ loggedIn, accountLogoUrl, currentPage }: NavBarProps): React.ReactElement {
   return (
        <nav className="flex items-center justify-between py-5 px-8 border-b-2 border-black bg-slate-900/80">
            <div className="flex items-center gap-14 text-xl">
                <Link href="/">
                    <img src="/Medium_Logo.svg" alt="Logo" className="h-8" />
                </Link>
                {/* Navigation buttons */}
                <Link
                    href="/home"
                    className={`mr-4 no-underline ${currentPage === 'home' ? 'text-slate-200 font-medium' : 'text-slate-200 font-light '}`}
                >
                    Home
                </Link>
                <Link
                    href="/projects"
                    className={`mr-4 no-underline ${currentPage === 'projects' ? 'text-slate-200 font-medium' : 'text-slate-200 font-light '}`}
                >
                    Projects
                </Link>
                <Link
                    href="/explore"
                    className={`mr-4 no-underline ${currentPage === 'explore' ? 'text-slate-200 font-medium' : 'text-slate-200 font-light '}`}
                >
                    Explore
                </Link>
                <Link
                    href="/settings"
                    className={`mr-4 no-underline ${currentPage === 'settings' ? 'text-slate-200 font-medium' : 'text-slate-200 font-light '}`}
                >
                    Settings
                </Link>
            </div>
            {/* Drop down menu */}
            <div>
                {loggedIn ? (
                    <Link href="/settings" className="flex items-center no-underline">
                        <img
                            src={accountLogoUrl || '/Default_Avatar.svg'}
                            alt="Account"
                            className="w-9 h-9 rounded-full object-cover border border-gray-300"
                        />
                    </Link>
                ) : (
                    <Link href="/login" className="px-6 py-2 bg-purple-700 text-white rounded-md no-underline font-medium hover:bg-purple-600 transition-colors">
                        Login
                    </Link>
                )}
            </div>
        </nav>
    );
}