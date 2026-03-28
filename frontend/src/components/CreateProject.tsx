"use client";

import React, { useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import Popup from "@/components/Popup";
import { useAuth } from "@/context/AuthContext";
import { useProtectedApi } from "@/lib/api";
import { postNewProject, UpsertProjectPayload } from "@/lib/projects";

export interface CreateProjectResponse {
    puid: string;
    name: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface CreateProjectProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    username?: string;
    onCreated?: (project: CreateProjectResponse) => void;
}

export default function CreateProject({
    open,
    onOpenChange,
    username,
    onCreated,
}: CreateProjectProps): React.ReactElement {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { userId } = useAuth();
    const protectedApi = useProtectedApi();

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [isPublic, setIsPublic] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const nameRef = useRef<HTMLInputElement>(null);

    const createProjectMutation = useMutation({
        mutationFn: async (payload: UpsertProjectPayload) => {
            const response = await postNewProject(protectedApi, payload);
            return response as CreateProjectResponse;
        },
        onSuccess: async (project) => {
            setError(null);
            setName("");
            setDescription("");
            setIsPublic(false);
            onOpenChange(false);

            await queryClient.invalidateQueries({
                queryKey: ["projects", userId],
            });

            if (onCreated) {
                onCreated(project);
                return;
            }

            // Default behavior if no callback is supplied: navigate to the new project.
            if (username) {
                void navigate({
                    to: `/${encodeURIComponent(username)}/${encodeURIComponent(project.name)}/`,
                });
            }
        },
        onError: (err) => {
            setError(
                err instanceof Error
                    ? err.message
                    : "Failed to create project.",
            );
        },
    });

    return (
        <Popup
            open={open}
            onOpenChange={(next) => {
                onOpenChange(next);
                if (next) setError(null);
            }}
            title="Create project"
            description="Create a new project for your account."
            initialFocusRef={nameRef}
            footer={
                <div className="flex items-center justify-end gap-2">
                    <button
                        type="button"
                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        onClick={() => onOpenChange(false)}
                        disabled={createProjectMutation.isPending}
                    >
                        Cancel
                    </button>
                    <button
                        type="button"
                        className="rounded-lg bg-purple-600/30 px-4 py-2 text-sm font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:cursor-not-allowed disabled:opacity-60"
                        onClick={() => {
                            setError(null);
                            const trimmed = name.trim();
                            if (!trimmed) {
                                setError("Project name is required.");
                                return;
                            }
                            createProjectMutation.mutate({
                                name: trimmed,
                                description: description.trim()
                                    ? description.trim()
                                    : null,
                                isPublic,
                                aliasProjectPuid: null,
                            });
                        }}
                        disabled={createProjectMutation.isPending}
                    >
                        {createProjectMutation.isPending
                            ? "Creating…"
                            : "Create"}
                    </button>
                </div>
            }
        >
            <div className="flex flex-col gap-4">
                {error && (
                    <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-4 py-3 text-sm text-red-200">
                        {error}
                    </div>
                )}

                <div className="flex flex-col gap-2">
                    <label className="text-sm font-medium text-slate-200">
                        Name
                    </label>
                    <input
                        ref={nameRef}
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        placeholder="My project"
                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        disabled={createProjectMutation.isPending}
                    />
                </div>

                <div className="flex flex-col gap-2">
                    <label className="text-sm font-medium text-slate-200">
                        Description (Optional)
                    </label>
                    <textarea
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="A brief description about this project..."
                        rows={3}
                        className="resize-none rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        disabled={createProjectMutation.isPending}
                    />
                </div>

                <label className="flex items-center gap-3 rounded-lg border border-slate-800 cursor-pointer bg-slate-900/40 px-3 py-2 text-sm text-slate-200">
                    <input
                        type="checkbox"
                        checked={isPublic}
                        onChange={(e) => setIsPublic(e.target.checked)}
                        disabled={createProjectMutation.isPending}
                        className="h-4 w-4 accent-purple-500 cursor-pointer"
                    />
                    <div className="min-w-0">
                        <div className="font-medium">Public project</div>
                        <div className="text-xs text-slate-400">
                            Allow others to view this project.
                        </div>
                    </div>
                </label>
            </div>
        </Popup>
    );
}
