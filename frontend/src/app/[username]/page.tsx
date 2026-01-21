"use client";

import CreateProject from "@/components/CreateProject";
import Link from "next/link";
import ProjectPageLayout from "@/components/ProjectPageLayout";
import { useParams } from "next/navigation";
import { useState } from "react";
import { useRouter } from "next/navigation";

export default function ProjectHomePage() {
    const params = useParams<{ username: string }>();
    const username = params?.username ?? "";
    const [createOpen, setCreateOpen] = useState(false);
    const router = useRouter();

    return (
        <ProjectPageLayout>
            <div className="flex-1 flex items-center justify-center">
                <div className="flex flex-col items-center gap-6">
                    <div className="text-2xl font-semibold text-slate-200">
                        No Project Selected
                    </div>
                    <div className="flex gap-4">
                        <button
                            type="button"
                            className="rounded-lg bg-purple-600/30 px-6 py-3 text-base font-medium text-purple-100 transition-colors cursor-pointer hover:bg-purple-600/40 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                            onClick={() => setCreateOpen(true)}
                        >
                            Create a Project
                        </button>
                        <Link
                            href="/explore"
                            className="rounded-lg bg-slate-900/60 px-6 py-3 text-base font-medium text-slate-200 border border-slate-700 transition-colors hover:bg-slate-800/60 hover:text-purple-300 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                        >
                            Search for Existing Projects
                        </Link>
                    </div>
                    <CreateProject
                        open={createOpen}
                        onOpenChange={setCreateOpen}
                        username={username}
                        onCreated={(project) => {
                            if (username) {
                                router.push(
                                    `/${encodeURIComponent(username)}/${encodeURIComponent(project.name)}/`,
                                );
                            }
                        }}
                    />
                </div>
            </div>
        </ProjectPageLayout>
    );
}
