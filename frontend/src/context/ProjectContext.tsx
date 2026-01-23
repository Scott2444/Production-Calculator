"use client";

import React, {
    createContext,
    useContext,
    useMemo,
    ReactNode,
    useEffect,
} from "react";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "./AuthContext";
import { fetchProject, resolveProject } from "@/lib/projects";
import { useProtectedApi } from "@/lib/api";

function safeDecodeURIComponent(value: string): string {
    try {
        return decodeURIComponent(value);
    } catch {
        return value;
    }
}

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
    const params = useParams<{ username: string; project_name: string }>();
    const routeUsername = params?.username
        ? safeDecodeURIComponent(params.username)
        : "";
    const routeProjectName = params?.project_name
        ? safeDecodeURIComponent(params.project_name)
        : "";

    const { userId, username, loggedIn } = useAuth();
    const protectedApi = useProtectedApi();
    const [projectId, setProjectId] = React.useState<string>("");

    useEffect(() => {
        const getProjectPuid = async () => {
            if (routeUsername && routeProjectName) {
                try {
                    const res = await resolveProject(
                        routeUsername,
                        routeProjectName,
                        protectedApi,
                    );
                    setProjectId(res.projectPuid);
                } catch (error) {
                    console.error("Failed to resolve project:", error);
                }
            }
        };
        getProjectPuid();
    }, [routeUsername, routeProjectName]);

    const projectQuery = useQuery({
        queryKey: ["project", projectId],
        queryFn: () => fetchProject(projectId, protectedApi),
        staleTime: 5 * 60 * 1000,
        enabled: Boolean(projectId),
    });

    const currentProject = useMemo(() => {
        const project = projectQuery.data as Project | undefined;
        return project ?? null;
    }, [projectQuery.data, routeProjectName]);

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
