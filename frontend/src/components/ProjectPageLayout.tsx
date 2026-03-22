import NavBar from "./NavBar";
import ProjectSidebar from "./ProjectSidebar";
import { ReactNode } from "react";

export default function ProjectPageLayout({
    children,
    padding = true,
}: {
    children: ReactNode;
    padding?: boolean;
}) {
    return (
        <div className="flex flex-col h-screen overflow-hidden">
            <NavBar />
            <div className="flex flex-1 min-h-0 overflow-hidden">
                <ProjectSidebar />
                <div
                    className={`flex-1 ${padding ? "p-6" : ""} overflow-y-auto`}
                >
                    {children}
                </div>
            </div>
        </div>
    );
}
