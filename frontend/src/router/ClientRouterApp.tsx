"use client";
import {
    Outlet,
    RouterProvider,
    createRootRoute,
    createRoute,
    createRouter,
} from "@tanstack/react-router";
import { createBrowserHistory, createMemoryHistory } from "@tanstack/history";
import { ProjectProvider } from "@/context/ProjectContext";
import UsernamePage from "@/features/UsernamePage";
import ProjectOverview from "@/features/ProjectOverview";
import WorkflowsPage from "@/features/WorkflowsPage";
import WorkflowPage from "@/features/WorkflowPage";
import RecipesPage from "@/features/RecipesPage";
import MachinesPage from "@/features/MachinesPage";
import ModifiersPage from "@/features/ModifiersPage";
import ProductsPage from "@/features/ProductsPage";
import AttributesPage from "@/features/AttributesPage";
import HomeRoute from "@/routes/HomeRoute";
import LoginRoute from "@/routes/LoginRoute";
import SignUpRoute from "@/routes/SignUpRoute";
import ForgotPasswordRoute from "../routes/ForgotPasswordRoute";
import ChangePasswordRoute from "../routes/ChangePasswordRoute";
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

function rejectDocsUsername<T extends { username?: string }>(params: T): T {
    if (params.username?.toLowerCase() === "docs") {
        throw new Error("The docs namespace is reserved.");
    }

    return params;
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
    path: "/login",
    component: LoginRoute,
});

const signUpRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/signup",
    component: SignUpRoute,
});

const forgotPasswordRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/reset-password",
    component: ForgotPasswordRoute,
});

const changePasswordRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/change-password",
    component: ChangePasswordRoute,
});

const verifyRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/verify",
    component: VerifyRoute,
});

const exploreRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/explore",
    component: ExploreRoute,
});

const docsRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/docs",
    component: DocsRoute,
});

const docsCatchAllRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/docs/$",
    component: DocsRoute,
});

const settingsRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/settings",
    component: SettingsRoute,
});

const usernameRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/$username",
    component: UsernamePage,
    params: {
        parse: rejectDocsUsername,
    },
    skipRouteOnParseError: {
        params: true,
    },
});

const projectLayoutRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/$username/$projectName",
    component: ProjectRouteLayout,
    params: {
        parse: rejectDocsUsername,
    },
    skipRouteOnParseError: {
        params: true,
    },
});

const projectOverviewRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "/",
    component: ProjectOverview,
});

const workflowsRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "workflows",
    component: WorkflowsPage,
});

const workflowRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "workflows/$workflowName",
    component: WorkflowPage,
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

const attributesRoute = createRoute({
    getParentRoute: () => projectLayoutRoute,
    path: "attributes",
    component: AttributesPage,
});

const routeTree = rootRoute.addChildren([
    homeRoute,
    loginRoute,
    signUpRoute,
    forgotPasswordRoute,
    changePasswordRoute,
    verifyRoute,
    exploreRoute,
    docsRoute,
    docsCatchAllRoute,
    settingsRoute,
    usernameRoute,
    projectLayoutRoute.addChildren([
        projectOverviewRoute,
        workflowsRoute,
        workflowRoute,
        recipesRoute,
        machinesRoute,
        modifiersRoute,
        productsRoute,
        attributesRoute,
    ]),
]);

const history =
    typeof window === "undefined"
        ? createMemoryHistory({ initialEntries: ["/"] })
        : createBrowserHistory();

const router = createRouter({ routeTree, history });

declare module "@tanstack/react-router" {
    interface Register {
        router: typeof router;
    }
}

export default function ClientApp() {
    return <RouterProvider router={router} />;
}
