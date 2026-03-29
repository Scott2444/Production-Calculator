"use client";

import ErrorDisplay from "@/components/ErrorDisplay";
import NavBar from "@/components/NavBar";
import Popup from "@/components/Popup";
import SearchBar from "@/components/SearchBar";
import { useAuth } from "@/context/AuthContext";
import { useProtectedApi } from "@/lib/api";
import { getApiUrl } from "@/lib/apiUrl";
import {
    postNewProject,
    searchPublicProjects,
    type PublicProjectSearchResult,
    type UpsertProjectPayload,
} from "@/lib/projects";
import { formatTimestamp } from "@/lib/timestamp";
import { fetchUser } from "@/lib/user";
import { Project } from "@/types/projects";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useRouterState } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";

const SEARCH_PAGE_SIZE = 20;

interface AliasCreateResponse {
    puid: string;
    name: string;
}

interface ExploreSearchState {
    query: string;
    page: number;
}

interface ExploreSearchQueryResult {
    search: PublicProjectSearchResult;
    elapsedMs: number;
}

type PaginationToken = number | "ellipsis";

function errorMessage(error: unknown, fallback: string): string {
    if (error instanceof Error && error.message.trim()) {
        return error.message;
    }
    return fallback;
}

function parseExploreSearch(searchStr: string): ExploreSearchState {
    const normalized = searchStr.startsWith("?")
        ? searchStr.slice(1)
        : searchStr;
    const params = new URLSearchParams(normalized);

    const query = params.get("q")?.trim() ?? "";
    const rawPage = Number(params.get("page") ?? "1");
    const parsedPage = Number.isFinite(rawPage) ? Math.floor(rawPage) : 1;
    const page = parsedPage > 0 ? parsedPage : 1;

    return { query, page };
}

function buildExploreUrl(query: string, page: number): string {
    const trimmedQuery = query.trim();
    if (!trimmedQuery) return "/explore";

    const params = new URLSearchParams({ q: trimmedQuery });
    if (page > 1) {
        params.set("page", String(page));
    }

    return `/explore?${params.toString()}`;
}

function formatCompactCount(value: number): string {
    if (value >= 1_000_000_000_000) {
        return `${(value / 1_000_000_000_000).toFixed(1).replace(/\.0$/, "")}T`;
    }
    if (value >= 1_000_000_000) {
        return `${(value / 1_000_000_000).toFixed(1).replace(/\.0$/, "")}B`;
    }
    if (value >= 1_000_000) {
        return `${(value / 1_000_000).toFixed(1).replace(/\.0$/, "")}M`;
    }
    if (value >= 1_000) {
        return `${(value / 1_000).toFixed(1).replace(/\.0$/, "")}K`;
    }
    return value.toString();
}

function buildPaginationTokens(
    currentPage: number,
    totalPages: number,
): PaginationToken[] {
    if (totalPages <= 0) return [];

    if (totalPages <= 10) {
        return Array.from({ length: totalPages }, (_, index) => index + 1);
    }

    const current = Math.min(Math.max(currentPage, 1), totalPages);

    if (current <= 5) {
        return [
            ...Array.from({ length: 9 }, (_, index) => index + 1),
            "ellipsis",
            totalPages,
        ];
    }

    if (current >= totalPages - 4) {
        const start = Math.max(2, totalPages - 8);
        return [
            1,
            "ellipsis",
            ...Array.from(
                { length: totalPages - start + 1 },
                (_, index) => start + index,
            ),
        ];
    }

    return [
        1,
        "ellipsis",
        current - 3,
        current - 2,
        current - 1,
        current,
        current + 1,
        current + 2,
        current + 3,
        "ellipsis",
        totalPages,
    ];
}

export default function ExploreRoute() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const protectedApi = useProtectedApi();
    const { loggedIn, isHydrated, userId, username: authUsername } = useAuth();

    const locationSearchStr = useRouterState({
        select: (state) => state.location.searchStr,
    });
    const { query: submittedQuery, page } = useMemo(
        () => parseExploreSearch(locationSearchStr),
        [locationSearchStr],
    );

    const [searchText, setSearchText] = useState("");

    const [aliasOpen, setAliasOpen] = useState(false);
    const [aliasTarget, setAliasTarget] = useState<Project | null>(null);
    const [aliasName, setAliasName] = useState("");
    const [aliasDescription, setAliasDescription] = useState("");
    const [aliasIsPublic, setAliasIsPublic] = useState(false);
    const [aliasError, setAliasError] = useState<string | null>(null);
    const aliasNameRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        setSearchText(submittedQuery);
    }, [submittedQuery]);

    const publicApi = useCallback(
        (input: RequestInfo, init?: RequestInit): Promise<Response> => {
            const resolvedInput =
                typeof input === "string" ? getApiUrl(input) : input;
            return fetch(resolvedInput, {
                credentials: "include",
                ...init,
            });
        },
        [],
    );

    const viewerQuery = useQuery({
        queryKey: ["user", userId],
        queryFn: () => fetchUser(userId!, protectedApi),
        enabled: loggedIn && Boolean(userId),
        staleTime: 5 * 60 * 1000,
    });

    const viewerUsername = useMemo(() => {
        if (authUsername?.trim()) return authUsername;
        const maybeUser = viewerQuery.data as { username?: string } | undefined;
        return maybeUser?.username;
    }, [authUsername, viewerQuery.data]);

    const projectsSearchQuery = useQuery<ExploreSearchQueryResult>({
        queryKey: ["explore-projects", submittedQuery, page, SEARCH_PAGE_SIZE],
        queryFn: async () => {
            const startedAt = performance.now();
            const search = await searchPublicProjects(
                submittedQuery,
                page,
                SEARCH_PAGE_SIZE,
                publicApi,
            );
            const elapsedMs = Math.max(
                1,
                Math.round(performance.now() - startedAt),
            );
            return {
                search,
                elapsedMs,
            };
        },
        enabled: submittedQuery.length > 0,
        staleTime: 60 * 1000,
        retry: 1,
    });

    const createAliasMutation = useMutation({
        mutationFn: async (payload: UpsertProjectPayload) => {
            const response = await postNewProject(protectedApi, payload);
            return response as AliasCreateResponse;
        },
        onSuccess: async (createdProject) => {
            setAliasError(null);
            setAliasOpen(false);
            setAliasTarget(null);
            setAliasName("");
            setAliasDescription("");
            setAliasIsPublic(false);

            await queryClient.invalidateQueries({
                queryKey: ["projects", userId],
            });

            if (viewerUsername) {
                void navigate({
                    to: `/${encodeURIComponent(viewerUsername)}/${encodeURIComponent(createdProject.name)}/`,
                });
            }
        },
        onError: (error) => {
            setAliasError(
                errorMessage(error, "Failed to create alias project."),
            );
        },
    });

    const searchResult = projectsSearchQuery.data?.search;
    const projects = searchResult?.projects ?? [];
    const totalPages = searchResult?.totalPages ?? 0;
    const totalCount = searchResult?.totalCount ?? 0;
    const elapsedMs = projectsSearchQuery.data?.elapsedMs ?? 0;
    const activePage =
        totalPages > 0 ? Math.min(Math.max(page, 1), totalPages) : page;

    const paginationTokens = useMemo(
        () => buildPaginationTokens(activePage, totalPages),
        [activePage, totalPages],
    );

    const searchErrors = [
        projectsSearchQuery.error
            ? {
                  id: "explore-search-error",
                  message: errorMessage(
                      projectsSearchQuery.error,
                      "Failed to search projects.",
                  ),
              }
            : null,
    ];

    const canSearch = submittedQuery.length > 0;

    const applyExploreSearch = useCallback(
        (nextQuery: string, nextPage: number) => {
            const nextUrl = buildExploreUrl(nextQuery, nextPage);
            const currentUrl = locationSearchStr
                ? `/explore${locationSearchStr}`
                : "/explore";

            if (nextUrl === currentUrl) {
                return;
            }

            void navigate({ to: nextUrl });
        },
        [locationSearchStr, navigate],
    );

    function openAliasDialog(project: Project) {
        setAliasTarget(project);
        setAliasName(`${project.name} Alias`);
        setAliasDescription("");
        setAliasIsPublic(false);
        setAliasError(null);
        setAliasOpen(true);
    }

    function submitAlias() {
        if (!loggedIn) {
            setAliasError("Please sign in to create an alias project.");
            return;
        }

        const target = aliasTarget;
        if (!target) {
            setAliasError("No source project selected.");
            return;
        }

        const trimmedName = aliasName.trim();
        if (!trimmedName) {
            setAliasError("Project name is required.");
            return;
        }

        createAliasMutation.mutate({
            name: trimmedName,
            description: aliasDescription.trim()
                ? aliasDescription.trim()
                : null,
            isPublic: aliasIsPublic,
            aliasProjectPuid: target.puid,
        });
    }

    return (
        <div className="min-h-screen flex flex-col">
            <NavBar />
            <div className="flex-1 p-6 min-h-0 min-w-0 flex flex-col">
                <div className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-6">
                    <div className="flex flex-col gap-2">
                        <h1 className="text-2xl font-semibold text-slate-100">
                            Explore Projects
                        </h1>
                        <p className="text-sm text-slate-400">
                            Search public projects ranked by relevance and
                            popularity, then create your own alias project from
                            any result.
                        </p>
                    </div>

                    <SearchBar
                        value={searchText}
                        onChange={setSearchText}
                        disabled={false}
                        onKeyDown={(event) => {
                            if (event.key !== "Enter") return;
                            event.preventDefault();
                            applyExploreSearch(searchText, 1);
                        }}
                    />

                    <ErrorDisplay errors={searchErrors} />

                    {!canSearch && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                            Enter a search term to explore public projects.
                        </div>
                    )}

                    {canSearch && projectsSearchQuery.isLoading && (
                        <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                            Searching projects...
                        </div>
                    )}

                    {canSearch &&
                        !projectsSearchQuery.isLoading &&
                        !projectsSearchQuery.error && (
                            <>
                                <div className="flex flex-col gap-2 px-4 sm:flex-row sm:items-baseline">
                                    <span className="text-md text-slate-300 font-semibold">
                                        {formatCompactCount(totalCount)} results
                                    </span>
                                    <span className="text-xs text-slate-300 font-extralight">
                                        ({elapsedMs} ms)
                                    </span>
                                </div>

                                {projects.length === 0 && (
                                    <div className="rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-8 text-sm text-slate-300">
                                        No public projects matched your search.
                                    </div>
                                )}

                                {projects.length > 0 && (
                                    <div className="overflow-hidden rounded-xl border border-slate-800 bg-slate-900/40">
                                        {projects.map((project, index) => {
                                            const projectLabel =
                                                project.name?.trim() ||
                                                project.puid;
                                            const projectDescription =
                                                project.description?.trim() ||
                                                "No description provided.";

                                            return (
                                                <div
                                                    key={project.puid}
                                                    className={`flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-start sm:justify-between ${
                                                        index <
                                                        projects.length - 1
                                                            ? "border-b border-slate-800"
                                                            : ""
                                                    }`}
                                                >
                                                    <div className="min-w-0">
                                                        <Link
                                                            to="/$username/$projectName"
                                                            params={{
                                                                username:
                                                                    project.ownerUsername,
                                                                projectName:
                                                                    projectLabel,
                                                            }}
                                                            className="text-base font-semibold text-purple-300 transition-colors hover:text-purple-200"
                                                        >
                                                            {projectLabel}
                                                        </Link>
                                                        <div className="mt-1 text-xs text-slate-400">
                                                            Owner:{" "}
                                                            {
                                                                project.ownerUsername
                                                            }
                                                        </div>
                                                        <div className="mt-1 text-sm text-slate-300 prose prose-invert max-w-none line-clamp-1 *:m-0 *:inline">
                                                            <ReactMarkdown>
                                                                {
                                                                    projectDescription
                                                                }
                                                            </ReactMarkdown>
                                                        </div>
                                                        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-slate-400">
                                                            <span>
                                                                Aliases:{" "}
                                                                {formatCompactCount(
                                                                    project.aliasCount,
                                                                )}
                                                            </span>
                                                            <span>
                                                                Updated{" "}
                                                                {formatTimestamp(
                                                                    project.updatedAt,
                                                                )}
                                                            </span>
                                                        </div>
                                                    </div>

                                                    <div className="flex items-center gap-2 sm:pt-1">
                                                        <Link
                                                            to="/$username/$projectName"
                                                            params={{
                                                                username:
                                                                    project.ownerUsername,
                                                                projectName:
                                                                    projectLabel,
                                                            }}
                                                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60"
                                                        >
                                                            Open Project
                                                        </Link>

                                                        {loggedIn ? (
                                                            <button
                                                                type="button"
                                                                className="rounded-lg bg-purple-600/30 px-3 py-1.5 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                                                                onClick={() =>
                                                                    openAliasDialog(
                                                                        project,
                                                                    )
                                                                }
                                                                disabled={
                                                                    !isHydrated ||
                                                                    createAliasMutation.isPending
                                                                }
                                                            >
                                                                Create Alias
                                                            </button>
                                                        ) : (
                                                            <Link
                                                                to="/login"
                                                                className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors hover:border-purple-500/60 hover:bg-slate-800/60"
                                                            >
                                                                Log in to alias
                                                            </Link>
                                                        )}
                                                    </div>
                                                </div>
                                            );
                                        })}
                                    </div>
                                )}

                                {totalPages > 1 && (
                                    <div className="flex flex-wrap items-center justify-end gap-1 text-sm text-slate-300">
                                        <button
                                            type="button"
                                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 disabled:border-slate-700 disabled:bg-slate-900/60 disabled:opacity-60 disabled:cursor-default"
                                            onClick={() =>
                                                applyExploreSearch(
                                                    submittedQuery,
                                                    Math.max(1, activePage - 1),
                                                )
                                            }
                                            disabled={
                                                activePage <= 1 ||
                                                projectsSearchQuery.isFetching
                                            }
                                        >
                                            {"< Previous"}
                                        </button>

                                        {paginationTokens.map(
                                            (token, index) => {
                                                if (token === "ellipsis") {
                                                    return (
                                                        <span
                                                            key={`ellipsis-${index}`}
                                                            className="px-1 text-slate-500"
                                                        >
                                                            . . .
                                                        </span>
                                                    );
                                                }

                                                const isActive =
                                                    token === activePage;
                                                return (
                                                    <button
                                                        key={`page-${token}`}
                                                        type="button"
                                                        className={`rounded-md px-2 py-1 text-sm transition-colors cursor-pointer focus:outline-none focus:ring-2 focus:ring-purple-500/40 ${
                                                            isActive
                                                                ? "bg-slate-100 text-slate-900 font-semibold"
                                                                : "text-slate-300 hover:bg-slate-800/60 hover:text-slate-100"
                                                        }`}
                                                        onClick={() =>
                                                            applyExploreSearch(
                                                                submittedQuery,
                                                                token,
                                                            )
                                                        }
                                                        disabled={
                                                            isActive ||
                                                            projectsSearchQuery.isFetching
                                                        }
                                                    >
                                                        {token}
                                                    </button>
                                                );
                                            },
                                        )}

                                        <button
                                            type="button"
                                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-1.5 text-sm text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 disabled:border-slate-700 disabled:bg-slate-900/60 disabled:opacity-60 disabled:cursor-default"
                                            onClick={() =>
                                                applyExploreSearch(
                                                    submittedQuery,
                                                    Math.min(
                                                        totalPages,
                                                        activePage + 1,
                                                    ),
                                                )
                                            }
                                            disabled={
                                                activePage >= totalPages ||
                                                projectsSearchQuery.isFetching
                                            }
                                        >
                                            {"Next >"}
                                        </button>
                                    </div>
                                )}
                            </>
                        )}
                </div>
            </div>

            <Popup
                open={aliasOpen}
                onOpenChange={(next) => {
                    setAliasOpen(next);
                    if (!next) {
                        setAliasError(null);
                        setAliasTarget(null);
                        setAliasName("");
                        setAliasDescription("");
                        setAliasIsPublic(false);
                    }
                }}
                title="Create alias project"
                description={
                    aliasTarget
                        ? `Create your own project that aliases ${aliasTarget.ownerUsername}/${aliasTarget.name}.`
                        : "Create an alias project from the selected source project."
                }
                initialFocusRef={aliasNameRef}
                footer={
                    <div className="flex items-center justify-end gap-2">
                        <button
                            type="button"
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setAliasOpen(false)}
                            disabled={createAliasMutation.isPending}
                        >
                            Cancel
                        </button>
                        <button
                            type="button"
                            className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                            onClick={submitAlias}
                            disabled={createAliasMutation.isPending}
                        >
                            {createAliasMutation.isPending
                                ? "Creating..."
                                : "Create Alias"}
                        </button>
                    </div>
                }
            >
                <div className="flex flex-col gap-4">
                    <ErrorDisplay
                        errors={
                            aliasError
                                ? [
                                      {
                                          id: "create-alias-error",
                                          message: aliasError,
                                          onDismiss: () => setAliasError(null),
                                      },
                                  ]
                                : []
                        }
                    />

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Alias Name
                        </label>
                        <input
                            ref={aliasNameRef}
                            value={aliasName}
                            onChange={(e) => setAliasName(e.target.value)}
                            className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createAliasMutation.isPending}
                        />
                    </div>

                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-medium text-slate-200">
                            Description (Optional)
                        </label>
                        <textarea
                            value={aliasDescription}
                            onChange={(e) =>
                                setAliasDescription(e.target.value)
                            }
                            rows={3}
                            placeholder="A brief description about this alias project..."
                            className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            disabled={createAliasMutation.isPending}
                        />
                    </div>

                    <label className="flex items-center gap-3 rounded-lg border border-slate-800 cursor-pointer bg-slate-900/40 px-3 py-2 text-sm text-slate-200">
                        <input
                            type="checkbox"
                            checked={aliasIsPublic}
                            onChange={(e) => setAliasIsPublic(e.target.checked)}
                            disabled={createAliasMutation.isPending}
                            className="h-4 w-4 accent-purple-500 cursor-pointer"
                        />
                        <div className="min-w-0">
                            <div className="font-medium">Public project</div>
                            <div className="text-xs text-slate-400">
                                Allow others to view this alias project.
                            </div>
                        </div>
                    </label>
                </div>
            </Popup>
        </div>
    );
}
