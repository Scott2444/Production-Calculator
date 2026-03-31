import type { ComponentType } from "react";

import IntroductionDoc from "@/content/docs/introduction.mdx";
import GettingStartedDoc from "@/content/docs/getting-started.mdx";
import ProjectsDoc from "@/content/docs/projects/index.mdx";
import ProjectsAliasDoc from "@/content/docs/projects/alias.mdx";
import ComponentsDoc from "@/content/docs/components/index.mdx";
import ComponentsProductsDoc from "@/content/docs/components/products.mdx";
import ComponentsAttributesDoc from "@/content/docs/components/attributes.mdx";
import ComponentsRecipesDoc from "@/content/docs/components/recipes.mdx";
import ComponentsMachinesDoc from "@/content/docs/components/machines.mdx";
import ComponentsModifiersDoc from "@/content/docs/components/modifiers.mdx";
import WorkflowsDoc from "@/content/docs/workflows/index.mdx";
import WorkflowsTargetsDoc from "@/content/docs/workflows/targets.mdx";
import WorkflowsSupplyDemandDoc from "@/content/docs/workflows/supply-demand.mdx";
import WorkflowsProcessNodesDoc from "@/content/docs/workflows/process-nodes.mdx";
import WorkflowsProductNodesDoc from "@/content/docs/workflows/product-nodes.mdx";
import WorkflowsExternalProductsDoc from "@/content/docs/workflows/external-products.mdx";
import WorkflowsAttributesDoc from "@/content/docs/workflows/attributes.mdx";
import WorkflowsComponentChangesDoc from "@/content/docs/workflows/component-changes.mdx";
import CalculationDoc from "@/content/docs/calculation/index.mdx";
import FormulaDoc from "@/content/docs/calculation/formulas.mdx";
import SolverDoc from "@/content/docs/calculation/solver.mdx";

export type DocsComponent = ComponentType<Record<string, unknown>>;

export type DocsPage = {
    slug: string;
    title: string;
    summary: string;
    keywords: string[];
    section: string;
    component: DocsComponent;
};

export type DocsTreePageNode = {
    type: "page";
    title: string;
    slug: string;
};

export type DocsTreeFolderNode = {
    type: "folder";
    title: string;
    slug: string;
    children: DocsTreeNode[];
};

export type DocsTreeNode = DocsTreePageNode | DocsTreeFolderNode;

export const DOCS_BASE_PATH = "/docs";

export const DEFAULT_DOC_SLUG = "introduction";

export const DOCS_LEGACY_SLUG_MAP: Record<string, string> = {
    introduction: "introduction",
    "getting-started": "getting-started",
    projects: "projects",
    "projects-alias": "projects/alias",
    components: "projects/components",
    "components-products": "projects/components/products",
    "components-attributes": "projects/components/attributes",
    "components-recipes": "projects/components/recipes",
    "components-machines": "projects/components/machines",
    "components-modifiers": "projects/components/modifiers",
    workflows: "projects/workflows",
    "workflows-targets": "projects/workflows/targets",
    "workflows-supply-demand": "projects/workflows/supply-demand",
    "workflows-process-nodes": "projects/workflows/process-nodes",
    "workflows-product-nodes": "projects/workflows/product-nodes",
    "workflows-external-products": "projects/workflows/external-products",
    "workflows-attributes": "projects/workflows/attributes",
    "workflows-component-changes": "projects/workflows/component-changes",
    calcuation: "calculation",
};

export const DOCS_PAGES: DocsPage[] = [
    {
        slug: "introduction",
        title: "Introduction",
        summary:
            "Overview of Production Calculator docs and how to navigate them.",
        keywords: ["overview", "docs", "navigation", "production calculator"],
        section: "General",
        component: IntroductionDoc,
    },
    {
        slug: "getting-started",
        title: "Getting Started",
        summary:
            "Quick path to create a project, add components, and build workflows.",
        keywords: ["setup", "first project", "first workflow", "quick start"],
        section: "General",
        component: GettingStartedDoc,
    },
    {
        slug: "projects",
        title: "Projects",
        summary: "Top-level project container and visibility model.",
        keywords: ["project", "public", "private", "sharing"],
        section: "Projects",
        component: ProjectsDoc,
    },
    {
        slug: "projects/alias",
        title: "Alias",
        summary:
            "How alias projects map to canonical projects and publication rules.",
        keywords: ["alias", "canonical", "redirect", "public"],
        section: "Projects",
        component: ProjectsAliasDoc,
    },
    {
        slug: "projects/components",
        title: "Components",
        summary:
            "Overview of products, attributes, recipes, machines, and modifiers.",
        keywords: ["components", "entities", "project model"],
        section: "Projects / Components",
        component: ComponentsDoc,
    },
    {
        slug: "projects/components/products",
        title: "Products",
        summary:
            "Products used as recipe inputs, outputs, and workflow targets.",
        keywords: ["products", "items", "materials", "targets"],
        section: "Projects / Components",
        component: ComponentsProductsDoc,
    },
    {
        slug: "projects/components/attributes",
        title: "Attributes",
        summary: "Custom metrics and where they attach in project data.",
        keywords: ["attributes", "power", "pollution", "cost", "unit"],
        section: "Projects / Components",
        component: ComponentsAttributesDoc,
    },
    {
        slug: "projects/components/recipes",
        title: "Recipes",
        summary:
            "Transformation definitions with IO, timing, and attribute rates.",
        keywords: ["recipes", "inputs", "outputs", "crafting time"],
        section: "Projects / Components",
        component: ComponentsRecipesDoc,
    },
    {
        slug: "projects/components/machines",
        title: "Machines",
        summary: "Machine capabilities and speed context for recipe execution.",
        keywords: ["machines", "base speed", "capacity"],
        section: "Projects / Components",
        component: ComponentsMachinesDoc,
    },
    {
        slug: "projects/components/modifiers",
        title: "Modifiers",
        summary: "Runtime bonuses that affect speed, yield, and attributes.",
        keywords: ["modifiers", "flat", "percent", "multiplicative", "yield"],
        section: "Projects / Components",
        component: ComponentsModifiersDoc,
    },
    {
        slug: "projects/workflows",
        title: "Workflows",
        summary: "Workflow model with targets, nodes, and flow edges.",
        keywords: ["workflow", "chart", "solver", "graph"],
        section: "Projects / Workflows",
        component: WorkflowsDoc,
    },
    {
        slug: "projects/workflows/targets",
        title: "Targets",
        summary: "How desired product rates drive demand solving.",
        keywords: ["targets", "demand", "rate"],
        section: "Projects / Workflows",
        component: WorkflowsTargetsDoc,
    },
    {
        slug: "projects/workflows/supply-demand",
        title: "Supply and Demand",
        summary:
            "Difference between ideal demand and actual implementation supply.",
        keywords: ["supply", "demand", "deficit", "throughput"],
        section: "Projects / Workflows",
        component: WorkflowsSupplyDemandDoc,
    },
    {
        slug: "projects/workflows/process-nodes",
        title: "Process Nodes",
        summary: "Recipe execution nodes with machine and modifier context.",
        keywords: ["process nodes", "recipe", "machine", "modifiers"],
        section: "Projects / Workflows",
        component: WorkflowsProcessNodesDoc,
    },
    {
        slug: "projects/workflows/product-nodes",
        title: "Product Nodes",
        summary: "Material flow nodes and rate state between processes.",
        keywords: ["product nodes", "flow", "inflow", "outflow"],
        section: "Projects / Workflows",
        component: WorkflowsProductNodesDoc,
    },
    {
        slug: "projects/workflows/external-products",
        title: "External Products",
        summary: "Products supplied from outside the current workflow graph.",
        keywords: ["external", "import", "off-graph", "supply"],
        section: "Projects / Workflows",
        component: WorkflowsExternalProductsDoc,
    },
    {
        slug: "projects/workflows/attributes",
        title: "Attributes",
        summary:
            "Node-level attribute calculation for demand and supply views.",
        keywords: ["workflow attributes", "aggregation", "formula"],
        section: "Projects / Workflows",
        component: WorkflowsAttributesDoc,
    },
    {
        slug: "projects/workflows/component-changes",
        title: "Component Changes",
        summary:
            "How project changes can invalidate or reshape existing workflows.",
        keywords: [
            "component changes",
            "invalidation",
            "warnings",
            "recalculation",
            "version",
        ],
        section: "Projects / Workflows",
        component: WorkflowsComponentChangesDoc,
    },
    {
        slug: "calculation",
        title: "Calculation",
        summary: "How the solver approaches finding optimal solutions.",
        keywords: ["calculation", "solver", "optimization", "algorithm"],
        section: "General",
        component: CalculationDoc,
    },
    {
        slug: "calculation/formulas",
        title: "Formulas",
        summary:
            "Details about the mathematical formulas used in calculations.",
        keywords: ["formulas", "mathematics", "equations"],
        section: "General",
        component: FormulaDoc,
    },
    {
        slug: "calculation/solver",
        title: "Solver",
        summary: "Information about the solver and its operation.",
        keywords: ["solver", "optimization", "algorithm"],
        section: "General",
        component: SolverDoc,
    },
];

export const DOCS_PAGE_MAP: Record<string, DocsPage> = Object.fromEntries(
    DOCS_PAGES.map((page) => [page.slug, page]),
);

export const DOCS_TREE: DocsTreeNode[] = [
    {
        type: "page",
        title: "Introduction",
        slug: "introduction",
    },
    {
        type: "page",
        title: "Getting Started",
        slug: "getting-started",
    },
    {
        type: "folder",
        title: "Projects",
        slug: "projects",
        children: [
            {
                type: "page",
                title: "Alias",
                slug: "projects/alias",
            },
            {
                type: "folder",
                title: "Components",
                slug: "projects/components",
                children: [
                    {
                        type: "page",
                        title: "Products",
                        slug: "projects/components/products",
                    },
                    {
                        type: "page",
                        title: "Attributes",
                        slug: "projects/components/attributes",
                    },
                    {
                        type: "page",
                        title: "Recipes",
                        slug: "projects/components/recipes",
                    },
                    {
                        type: "page",
                        title: "Machines",
                        slug: "projects/components/machines",
                    },
                    {
                        type: "page",
                        title: "Modifiers",
                        slug: "projects/components/modifiers",
                    },
                ],
            },
            {
                type: "folder",
                title: "Workflows",
                slug: "projects/workflows",
                children: [
                    {
                        type: "page",
                        title: "Targets",
                        slug: "projects/workflows/targets",
                    },
                    {
                        type: "page",
                        title: "Supply and Demand",
                        slug: "projects/workflows/supply-demand",
                    },
                    {
                        type: "page",
                        title: "Process Nodes",
                        slug: "projects/workflows/process-nodes",
                    },
                    {
                        type: "page",
                        title: "Product Nodes",
                        slug: "projects/workflows/product-nodes",
                    },
                    {
                        type: "page",
                        title: "External Products",
                        slug: "projects/workflows/external-products",
                    },
                    {
                        type: "page",
                        title: "Attributes",
                        slug: "projects/workflows/attributes",
                    },
                    {
                        type: "page",
                        title: "Component Changes",
                        slug: "projects/workflows/component-changes",
                    },
                ],
            },
        ],
    },
    {
        type: "folder",
        title: "Calculation",
        slug: "calculation",
        children: [
            {
                type: "page",
                title: "Formulas",
                slug: "calculation/formulas",
            },
            {
                type: "page",
                title: "Solver",
                slug: "calculation/solver",
            },
        ],
    },
];
