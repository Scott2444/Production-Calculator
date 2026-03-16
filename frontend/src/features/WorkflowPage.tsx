"use client";

import ProjectPageLayout from "@/components/ProjectPageLayout";
import ProjectStatusGate from "@/components/ProjectStatusGate";

export default function WorkflowPage() {
    return (
        <ProjectPageLayout>
            <ProjectStatusGate>{null}</ProjectStatusGate>
        </ProjectPageLayout>
    );
}
