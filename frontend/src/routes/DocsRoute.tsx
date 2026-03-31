"use client";

import { useEffect, useMemo, useState } from "react";
import { useNavigate, useRouterState } from "@tanstack/react-router";
import {
    IconBook2,
    IconChevronDown,
    IconChevronRight,
    IconFileText,
    IconFolder,
    IconFolderOpen,
    IconListDetails,
} from "@tabler/icons-react";

import NavBar from "@/components/NavBar";
import SearchBar from "@/components/SearchBar";
import { useSearch } from "@/hooks/Search";
import {
    DOCS_BASE_PATH,
    DOCS_LEGACY_SLUG_MAP,
    DEFAULT_DOC_SLUG,
    DOCS_PAGE_MAP,
    DOCS_PAGES,
    DOCS_TREE,
    type DocsPage,
    type DocsTreeNode,
} from "@/routes/docs/docsRegistry";

function normalizeSlug(value: string): string {
    return value.replace(/^\/+|\/+$/g, "").replace(/\/{2,}/g, "/");
}

function normalizePathname(pathname: string): string {
    return pathname.replace(/\/+$/g, "") || "/";
}

function isDocsPathname(pathname: string): boolean {
    return (
        pathname === DOCS_BASE_PATH || pathname.startsWith(`${DOCS_BASE_PATH}/`)
    );
}

function readSlugFromPathname(pathname: string): string {
    const normalizedPath = normalizePathname(pathname);

    if (normalizedPath === DOCS_BASE_PATH) {
        return DEFAULT_DOC_SLUG;
    }

    if (!normalizedPath.startsWith(`${DOCS_BASE_PATH}/`)) {
        return DEFAULT_DOC_SLUG;
    }

    const encodedSlug = normalizedPath.slice(DOCS_BASE_PATH.length + 1);
    const slug = normalizeSlug(decodeURIComponent(encodedSlug));

    if (!slug || !DOCS_PAGE_MAP[slug]) {
        return DEFAULT_DOC_SLUG;
    }

    return slug;
}

function buildDocsPath(slug: string): string {
    const normalizedSlug = normalizeSlug(slug);

    if (!normalizedSlug || normalizedSlug === DEFAULT_DOC_SLUG) {
        return DOCS_BASE_PATH;
    }

    return `${DOCS_BASE_PATH}/${normalizedSlug}`;
}

function findAncestorFolderSlugs(
    nodes: DocsTreeNode[],
    targetSlug: string,
    ancestors: string[] = [],
): string[] | null {
    for (const node of nodes) {
        if (node.type === "page") {
            if (node.slug === targetSlug) {
                return ancestors;
            }
            continue;
        }

        if (node.slug === targetSlug) {
            return ancestors;
        }

        const nestedResult = findAncestorFolderSlugs(
            node.children,
            targetSlug,
            [...ancestors, node.slug],
        );

        if (nestedResult) {
            return nestedResult;
        }
    }

    return null;
}

type DocsTreeProps = {
    nodes: DocsTreeNode[];
    activeSlug: string;
    collapsedFolders: Set<string>;
    onSelect: (slug: string) => void;
    onToggleFolder: (slug: string) => void;
};

function DocsTree({
    nodes,
    activeSlug,
    collapsedFolders,
    onSelect,
    onToggleFolder,
}: DocsTreeProps) {
    return (
        <div className="space-y-1">
            {nodes.map((node) => {
                if (node.type === "page") {
                    const isActive = activeSlug === node.slug;
                    return (
                        <button
                            key={node.slug}
                            type="button"
                            onClick={() => onSelect(node.slug)}
                            className={`flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm transition-colors cursor-pointer ${
                                isActive
                                    ? "bg-slate-700/70 text-slate-100"
                                    : "text-slate-300 hover:bg-slate-800/70 hover:text-slate-100"
                            }`}
                        >
                            <IconFileText size={15} className="shrink-0" />
                            <span>{node.title}</span>
                        </button>
                    );
                }

                const isPageActive = activeSlug === node.slug;
                const isCollapsed = collapsedFolders.has(node.slug);
                const ChevronIcon = isCollapsed
                    ? IconChevronRight
                    : IconChevronDown;

                const folderStateClass = isPageActive
                    ? "bg-slate-700/70 text-slate-100"
                    : "text-slate-200 hover:bg-slate-800/60";

                return (
                    <div key={node.slug} className="rounded-lg">
                        <div
                            className={`flex items-center gap-1 rounded-lg ${folderStateClass}`}
                        >
                            <button
                                type="button"
                                onClick={() => onSelect(node.slug)}
                                className="flex flex-1 cursor-pointer items-center gap-2 rounded-lg px-3 py-2 text-left text-sm font-semibold"
                            >
                                {isCollapsed ? (
                                    <IconFolder
                                        size={15}
                                        className="shrink-0 text-slate-300"
                                    />
                                ) : (
                                    <IconFolderOpen
                                        size={15}
                                        className="shrink-0 text-slate-300"
                                    />
                                )}
                                <span>{node.title}</span>
                            </button>

                            <button
                                type="button"
                                onClick={() => onToggleFolder(node.slug)}
                                aria-label={
                                    isCollapsed
                                        ? `Expand ${node.title}`
                                        : `Collapse ${node.title}`
                                }
                                className="mr-1 rounded-md p-1 text-slate-300 hover:bg-slate-700/60 hover:text-slate-100"
                            >
                                <ChevronIcon size={14} />
                            </button>
                        </div>

                        {!isCollapsed && (
                            <div className="ml-4 border-l border-slate-700/70 pl-2">
                                <DocsTree
                                    nodes={node.children}
                                    activeSlug={activeSlug}
                                    collapsedFolders={collapsedFolders}
                                    onSelect={onSelect}
                                    onToggleFolder={onToggleFolder}
                                />
                            </div>
                        )}
                    </div>
                );
            })}
        </div>
    );
}

type SearchResultsProps = {
    pages: DocsPage[];
    activeSlug: string;
    onSelect: (slug: string) => void;
};

function SearchResults({ pages, activeSlug, onSelect }: SearchResultsProps) {
    if (pages.length === 0) {
        return (
            <div className="rounded-xl border border-slate-700 bg-slate-900/40 px-3 py-4 text-sm text-slate-300">
                No docs matched your search.
            </div>
        );
    }

    return (
        <div className="space-y-2">
            {pages.map((page) => {
                const isActive = activeSlug === page.slug;
                return (
                    <button
                        key={page.slug}
                        type="button"
                        onClick={() => onSelect(page.slug)}
                        className={`w-full rounded-xl border px-3 py-3 text-left transition-colors ${
                            isActive
                                ? "border-slate-500 bg-slate-700/70"
                                : "border-slate-700 bg-slate-900/40 hover:border-slate-600 hover:bg-slate-800/70"
                        }`}
                    >
                        <div className="flex items-center justify-between gap-3">
                            <div className="truncate text-sm font-semibold text-slate-100">
                                {page.title}
                            </div>
                            <div className="shrink-0 text-xs text-slate-400">
                                {page.section}
                            </div>
                        </div>
                        <p className="mt-2 line-clamp-2 text-xs text-slate-300">
                            {page.summary}
                        </p>
                    </button>
                );
            })}
        </div>
    );
}

export default function DocsRoute() {
    const navigate = useNavigate();
    const pathname = useRouterState({
        select: (state) => state.location.pathname,
    });

    const activeSlug = useMemo(
        () => readSlugFromPathname(pathname),
        [pathname],
    );
    const [collapsedFolders, setCollapsedFolders] = useState<Set<string>>(
        () => new Set(),
    );

    const { searchText, setSearchText, filteredItems } = useSearch(DOCS_PAGES, {
        toText: (page) =>
            [page.title, page.summary, page.section, ...page.keywords].join(
                " ",
            ),
    });

    useEffect(() => {
        if (typeof window === "undefined") {
            return;
        }

        const params = new URLSearchParams(window.location.search);
        const legacySlug = params.get("doc");

        if (!legacySlug) {
            return;
        }

        const normalizedLegacySlug = normalizeSlug(legacySlug);
        const mappedSlug =
            DOCS_LEGACY_SLUG_MAP[normalizedLegacySlug] ?? normalizedLegacySlug;
        const nextSlug = DOCS_PAGE_MAP[mappedSlug]
            ? mappedSlug
            : DEFAULT_DOC_SLUG;

        navigate({
            to: buildDocsPath(nextSlug),
            replace: true,
        });
    }, [navigate]);

    useEffect(() => {
        const normalizedPathname = normalizePathname(pathname);

        if (!isDocsPathname(normalizedPathname)) {
            return;
        }

        const canonicalPath = buildDocsPath(activeSlug);

        if (normalizedPathname === canonicalPath) {
            return;
        }

        navigate({
            to: canonicalPath,
            replace: true,
        });
    }, [activeSlug, navigate, pathname]);

    useEffect(() => {
        const ancestorFolders = findAncestorFolderSlugs(DOCS_TREE, activeSlug);

        if (!ancestorFolders || ancestorFolders.length === 0) {
            return;
        }

        setCollapsedFolders((previous) => {
            const next = new Set(previous);
            let changed = false;

            for (const folderSlug of ancestorFolders) {
                if (next.delete(folderSlug)) {
                    changed = true;
                }
            }

            return changed ? next : previous;
        });
    }, [activeSlug]);

    const toggleFolder = (slug: string) => {
        setCollapsedFolders((previous) => {
            const next = new Set(previous);

            if (next.has(slug)) {
                next.delete(slug);
            } else {
                next.add(slug);
            }

            return next;
        });
    };

    const hasSearch = searchText.trim().length > 0;

    const currentPage = useMemo(() => {
        return DOCS_PAGE_MAP[activeSlug] ?? DOCS_PAGE_MAP[DEFAULT_DOC_SLUG];
    }, [activeSlug]);

    const SearchComponent = currentPage.component;

    const selectDoc = (slug: string) => {
        if (!DOCS_PAGE_MAP[slug]) return;

        navigate({ to: buildDocsPath(slug) });
    };

    return (
        <>
            <NavBar currentPage="Docs" />
            <div className="mx-auto flex w-full max-w-425 gap-6 px-4 py-6 md:px-8">
                <aside className="sticky top-24 hidden h-[calc(100vh-8rem)] w-96 shrink-0 overflow-y-auto [scrollbar-gutter:stable] rounded-2xl border border-slate-700 bg-slate-900/70 p-4 lg:block">
                    <div className="mb-4 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-slate-300">
                        <IconBook2 size={16} />
                        <span>Docs</span>
                    </div>
                    <SearchBar
                        value={searchText}
                        onChange={setSearchText}
                        disabled={false}
                    />
                    <div className="mt-4">
                        {hasSearch ? (
                            <SearchResults
                                pages={filteredItems}
                                activeSlug={activeSlug}
                                onSelect={selectDoc}
                            />
                        ) : (
                            <DocsTree
                                nodes={DOCS_TREE}
                                activeSlug={activeSlug}
                                collapsedFolders={collapsedFolders}
                                onSelect={selectDoc}
                                onToggleFolder={toggleFolder}
                            />
                        )}
                    </div>
                </aside>

                <main className="min-w-0 flex-1 rounded-2xl border border-slate-700 bg-slate-900/55 p-4 md:p-8">
                    <div className="mb-5 rounded-xl border border-slate-700 bg-slate-900/40 p-4 lg:hidden">
                        <SearchBar
                            value={searchText}
                            onChange={setSearchText}
                            disabled={false}
                        />
                        <div className="mt-3 rounded-lg border border-slate-700 bg-slate-950/40 px-3 py-2 text-xs text-slate-300">
                            <div className="flex items-center gap-2 text-slate-400">
                                <IconListDetails size={14} />
                                <span>Current: {currentPage.title}</span>
                            </div>
                        </div>
                        <div className="mt-3 max-h-80 overflow-y-auto [scrollbar-gutter:stable]">
                            {hasSearch ? (
                                <SearchResults
                                    pages={filteredItems}
                                    activeSlug={activeSlug}
                                    onSelect={selectDoc}
                                />
                            ) : (
                                <DocsTree
                                    nodes={DOCS_TREE}
                                    activeSlug={activeSlug}
                                    collapsedFolders={collapsedFolders}
                                    onSelect={selectDoc}
                                    onToggleFolder={toggleFolder}
                                />
                            )}
                        </div>
                    </div>

                    <div className="mb-6 flex flex-wrap items-center gap-3 rounded-xl border border-slate-700 bg-slate-900/40 px-4 py-3 text-sm text-slate-300">
                        <div className="flex items-center gap-2 font-medium text-slate-200">
                            <IconFolder size={16} />
                            <span>{currentPage.section}</span>
                        </div>
                        <span className="text-slate-500">/</span>
                        <div className="truncate">{currentPage.title}</div>
                    </div>

                    <article className="min-h-[60vh]">
                        <SearchComponent />
                    </article>
                </main>
            </div>
        </>
    );
}
