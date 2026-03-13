"use client";

import { useMemo } from "react";
import {
    Outlet,
    RouterProvider,
    createRootRoute,
    createRoute,
    createRouter,
} from "@tanstack/react-router";
import { ProjectProvider } from "@/context/ProjectContext";
import UsernamePage from "@/features/UsernamePage";
import ProjectOverview from "@/features/ProjectOverview";
import RecipesPage from "@/features/RecipesPage";
import MachinesPage from "@/features/MachinesPage";
import ModifiersPage from "@/features/ModifiersPage";
import ProductsPage from "@/features/ProductsPage";
import HomeRoute from "@/routes/HomeRoute";
import LoginRoute from "@/routes/LoginRoute";
import SignUpRoute from "@/routes/SignUpRoute";
import VerifyRoute from "@/routes/VerifyRoute";
import ExploreRoute from "@/routes/ExploreRoute";
import DocsRoute from "@/routes/DocsRoute";
import SettingsRoute from "@/routes/SettingsRoute";

function RootRouteComponent() {
    return <Outlet />;
}

function ProjectRouteLayout() {
    return (
        <ProjectProvider>
            <Outlet />
        </ProjectProvider>
    );
}

const rootRoute = createRootRoute({
    component: RootRouteComponent,
});

const homeRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/",
    component: HomeRoute,
});

const loginRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "login",
    component: LoginRoute,
});

const signUpRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "signup",
    component: SignUpRoute,
});

const verifyRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "verify",
    component: VerifyRoute,
});

const exploreRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "explore",
    component: ExploreRoute,
});

const docsRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "docs",
    component: DocsRoute,
});

const settingsRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "settings",
    component: SettingsRoute,
});

const usernameRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "$username",
    component: UsernamePage,
});

const projectLayoutRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "$username/$projectName",
    component: ProjectRouteLayout,
});

const projectOverviewRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "/",
    component: ProjectOverview,
});

const recipesRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "recipes",
    component: RecipesPage,
});

const machinesRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "machines",
    component: MachinesPage,
});

const modifiersRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "modifiers",
    component: ModifiersPage,
});

const productsRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "products",
    component: ProductsPage,
});

const routeTree = rootRoute.addChildren([
    homeRoute,
    loginRoute,
    signUpRoute,
    verifyRoute,
    exploreRoute,
    docsRoute,
    settingsRoute,
    usernameRoute,
    projectLayoutRoute.addChildren([
        projectOverviewRoute,
        recipesRoute,
        machinesRoute,
        modifiersRoute,
        productsRoute,
    ]),
]);

let appRouter: ReturnType<typeof createRouter> | null = null;

function getRouter() {
    if (!appRouter) {
        appRouter = createRouter({ routeTree });
    }

    return appRouter;
}

declare module "@tanstack/react-router" {
    interface Register {
        router: ReturnType<typeof createRouter>;
    }
}

export default function ClientRouterApp() {
    const router = useMemo(() => getRouter(), []);

    return <RouterProvider router={router} />;
}
