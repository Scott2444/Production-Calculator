"use client";

import NavBar from "@/components/NavBar";
import DropDown from "@/components/DropDown";
import ProjectSidebar from "@/components/ProjectSidebar";
import { useParams } from "next/navigation";

export default function ProjectPage() {
    const params = useParams<{ username: string; project_name: string }>();

    const username = params?.username ?? "";
    const projectName = params?.project_name ?? "";

    return (
        <div className="min-h-screen flex flex-col">
            <NavBar />
            <div className="flex flex-1 min-h-0">
                <ProjectSidebar />
                <div className="flex-1 p-4">
                    <h1 className="text-xl font-semibold">
                        {projectName || "Project"}
                    </h1>
                    <div className="text-sm text-gray-600">
                        Owner: {username || "(unknown)"}
                    </div>

                    <div className="mt-4">
                        <DropDown label="Options" align="left">
                            <div className="p-4">Dropdown Content</div>
                        </DropDown>
                    </div>
                </div>
            </div>
        </div>
    );
}
