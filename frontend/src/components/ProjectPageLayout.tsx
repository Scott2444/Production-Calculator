import NavBar from "./NavBar";
import ProjectSidebar from "./ProjectSidebar";
import { ReactNode } from "react";

export default function ProjectPageLayout({
    children,
}: {
    children: ReactNode;
}) {
    return (
        <div className="min-h-screen flex flex-col">
            <NavBar />
            <div className="flex flex-1 min-h-0">
                <ProjectSidebar />
                <div className="flex-1 p-6 min-w-0 flex flex-col">
                    {children}
                </div>
            </div>
        </div>
    );
}
