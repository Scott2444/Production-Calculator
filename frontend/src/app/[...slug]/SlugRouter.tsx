"use client";

import { usePathname } from "next/navigation";
import { ProjectProvider } from "@/context/ProjectContext";
import UsernamePage from "@/features/UsernamePage";
import ProjectOverview from "@/features/ProjectOverview";
import RecipesPage from "@/features/RecipesPage";
import MachinesPage from "@/features/MachinesPage";
import ModifiersPage from "@/features/ModifiersPage";
import ProductsPage from "@/features/ProductsPage";

const PROJECT_SUBPAGES: Record<string, React.ComponentType> = {
    "": ProjectOverview,
    recipes: RecipesPage,
    machines: MachinesPage,
    modifiers: ModifiersPage,
    products: ProductsPage,
};

export default function SlugRouter() {
    const pathname = usePathname();
    const segments = pathname
        .split("/")
        .filter(Boolean)
        .map((s) => {
            try {
                return decodeURIComponent(s);
            } catch {
                return s;
            }
        });

    // /username — project picker page
    if (segments.length === 1) {
        return <UsernamePage />;
    }

    // /username/project[/subpage] — project pages
    const subpage = segments[2] ?? "";
    const SubpageComponent = PROJECT_SUBPAGES[subpage];

    if (SubpageComponent) {
        return (
            <ProjectProvider>
                <SubpageComponent />
            </ProjectProvider>
        );
    }

    // Unknown route
    return null;
}
