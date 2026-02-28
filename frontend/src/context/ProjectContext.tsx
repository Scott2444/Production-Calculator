"use client";

import React, { createContext, useContext, useMemo, ReactNode } from "react";
import { useRouteParams } from "@/hooks/useRouteParams";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "./AuthContext";
import { fetchProject, resolveProject } from "@/lib/projects";
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
    projectQuery: ReturnType<typeof useQuery<Project>>;
}

const ProjectContext = createContext<ProjectContextType | undefined>(undefined);

export function ProjectProvider({ children }: { children: ReactNode }) {
    const { username: routeUsername, projectName: routeProjectName } =
        useRouteParams();

    const { username, loggedIn } = useAuth();
    const protectedApi = useProtectedApi();

    const projectResolveQuery = useQuery({
        queryKey: ["resolve-project", routeUsername, routeProjectName],
        queryFn: () =>
            resolveProject(routeUsername, routeProjectName, protectedApi),
        staleTime: 5 * 60 * 1000,
        enabled: Boolean(routeUsername && routeProjectName),
    });

    const projectId = projectResolveQuery.data?.projectPuid ?? "";

    const projectQuery = useQuery({
        queryKey: ["project", projectId],
        queryFn: () => fetchProject(projectId, protectedApi),
        staleTime: 5 * 60 * 1000,
        enabled: Boolean(projectId),
    });

    const currentProject = useMemo(() => {
        const project = projectQuery.data as Project | undefined;
        return project ?? null;
    }, [projectQuery.data]);

    const canEdit =
        routeUsername === username &&
        loggedIn &&
        !currentProject?.aliasProjectPuid;

    const value: ProjectContextType = {
        routeUsername,
        routeProjectName,
        currentProject,
        projectId,
        canEdit,
        projectQuery,
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
