"use client";

import React, { createContext, useContext, useMemo, ReactNode } from "react";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "./AuthContext";
import { fetchProjects } from "@/lib/projects";
import { useProtectedApi } from "@/lib/api";

interface Project {
    puid: string;
    name: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
    createdAt: string;
    updatedAt: string;
}

interface ProjectContextType {
    routeUsername: string;
    routeProjectName: string;
    currentProject: Project | null;
    projectId: string;
    canEdit: boolean;
    projectsQuery: ReturnType<typeof useQuery>;
}

const ProjectContext = createContext<ProjectContextType | undefined>(undefined);

export function ProjectProvider({ children }: { children: ReactNode }) {
    const params = useParams<{ username: string; project_name: string }>();
    const routeUsername = params?.username ?? "";
    const routeProjectName = params?.project_name
        ? decodeURIComponent(params.project_name)
        : "";

    const { userId, username, loggedIn } = useAuth();
    const protectedApi = useProtectedApi();

    const projectsQuery = useQuery({
        queryKey: ["projects", userId],
        queryFn: () => fetchProjects(userId!, protectedApi),
        staleTime: 5 * 60 * 1000,
        enabled: Boolean(userId),
    });

    const currentProject = useMemo(() => {
        const projects = projectsQuery.data as Project[] | undefined;
        if (!projects || !routeProjectName) return null;
        return projects.find((p) => p.name === routeProjectName) ?? null;
    }, [projectsQuery.data, routeProjectName]);

    const projectId = currentProject?.puid ?? "";
    const canEdit = routeUsername === username && loggedIn;

    const value: ProjectContextType = {
        routeUsername,
        routeProjectName,
        currentProject,
        projectId,
        canEdit,
        projectsQuery,
    };

    return (
        <ProjectContext.Provider value={value}>
            {children}
        </ProjectContext.Provider>
    );
}

export function useProject() {
    const context = useContext(ProjectContext);
    if (!context) {
        throw new Error("useProject must be used within a ProjectProvider");
    }
    return context;
}
