"use client";

import type { ComponentType } from "react";
import Image from "next/image";
import { Link } from "@tanstack/react-router";
import {
    IconArrowRight,
    IconBinaryTree2,
    IconBolt,
    IconChartDots3,
    IconCompass,
    IconFlag3,
    IconHammer,
    IconRouteSquare,
    IconTimeline,
} from "@tabler/icons-react";
import NavBar from "@/components/NavBar";
import roadmapData from "@/content/landing/roadmap.json";

type RoadmapStatus = "Planned" | "In Progress";

type ChangelogEntry = {
    version: string;
    date: string;
    title: string;
    items: string[];
};

type FuturePlan = {
    title: string;
    description: string;
    status: RoadmapStatus;
};

type LandingRoadmap = {
    lastUpdated: string;
    changelog: ChangelogEntry[];
    futurePlans: FuturePlan[];
};

type FeatureCard = {
    title: string;
    summary: string;
    bullets: string[];
    image: string;
    imageAlt: string;
    icon: ComponentType<{ size?: number; className?: string }>;
};

const landingRoadmap = roadmapData as LandingRoadmap;

const planningSteps = [
    {
        title: "Model your project components",
        description:
            "Define products, recipes, machines, modifiers, and optional attributes once.",
        icon: IconHammer,
    },
    {
        title: "Set production targets",
        description:
            "Declare desired output rates and let demand solving shape the ideal graph.",
        icon: IconFlag3,
    },
    {
        title: "Validate real implementation",
        description:
            "Enter current machine counts and compare achievable supply against demand.",
        icon: IconChartDots3,
    },
];

const useCases = [
    {
        title: "Bootstrap a new factory",
        description:
            "Plan the full chain for a target item and discover required machine counts before building.",
        icon: IconCompass,
    },
    {
        title: "Debug bottlenecks",
        description:
            "Use node-level demand and supply values to identify where throughput is getting constrained.",
        icon: IconTimeline,
    },
    {
        title: "Optimize lines",
        description:
            "Model alternate routes and modifiers to experiment with improvements before committing to a build.",
        icon: IconRouteSquare,
    },
];

const featureCards: FeatureCard[] = [
    {
        title: "Visual Workflow Canvas",
        summary:
            "Build and inspect process and product nodes in a graph that reflects your production chain.",
        bullets: [
            "Trace material flow from source to target output.",
            "Understand how demand reshapes graph structure.",
            "Keep planning context visible while iterating quickly.",
        ],
        image: "/assets/WorkflowExample.png",
        imageAlt: "Workflow graph with process and product nodes",
        icon: IconBinaryTree2,
    },
    {
        title: "Detailed Node Insights",
        summary:
            "Each process node captures machine counts, calculated rates, and user-defined attributes.",
        bullets: [
            "Compare current machine counts with solved requirements.",
            "Track attributes such as power in demand and supply contexts.",
            "See where modifiers are applied inside the workflow.",
        ],
        image: "/assets/ProcessNodeExample.png",
        imageAlt:
            "Expanded process node details with machines, rates, and attributes",
        icon: IconBolt,
    },
    {
        title: "Demand vs Supply Validation",
        summary:
            "Product nodes and edge labels reveal what your line should do versus what it can do today.",
        bullets: [
            "Catch deficits early before rebuilding segments in-game.",
            "Experiment with alternate routes and external supply sources.",
            "Use clear flow labels to verify balancing decisions.",
        ],
        image: "/assets/NodeExample.png",
        imageAlt:
            "Product node showing demand, supply in, and supply out values",
        icon: IconChartDots3,
    },
];

function statusBadgeClasses(status: RoadmapStatus): string {
    if (status === "In Progress") {
        return "border-cyan-300/60 bg-cyan-500/20 text-cyan-100";
    }

    return "border-emerald-300/60 bg-emerald-500/20 text-emerald-100";
}

export default function HomeRoute() {
    return (
        <div className="relative min-h-screen overflow-x-hidden bg-slate-950 text-slate-100">
            <div className="pointer-events-none absolute inset-0">
                <div className="absolute -top-30 left-1/2 h-136 w-136 -translate-x-1/2 rounded-full bg-cyan-500/16 blur-3xl" />
                <div className="absolute top-48 -left-24 h-72 w-72 rounded-full bg-blue-500/10 blur-3xl" />
                <div className="absolute bottom-12 right-0 h-96 w-96 rounded-full bg-indigo-500/10 blur-3xl" />
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(148,163,184,0.14)_1px,transparent_1px)] bg-size-[22px_22px] opacity-35" />
            </div>

            <NavBar />

            <main className="relative mx-auto flex w-full max-w-7xl flex-col gap-24 px-6 pb-24 pt-16 md:px-10">
                <section className="landing-reveal grid items-center gap-12 lg:grid-cols-[1.06fr_0.94fr]">
                    <div className="space-y-8">
                        <span className="inline-flex items-center gap-2 rounded-full border border-cyan-300/35 bg-cyan-500/12 px-4 py-2 text-xs font-semibold tracking-[0.11em] text-cyan-100 uppercase">
                            Logistics Planning For Automation Games
                        </span>

                        <div className="space-y-5">
                            <h1 className="text-4xl leading-tight font-semibold text-white sm:text-5xl lg:text-6xl">
                                Build better factories before you place a single
                                belt.
                            </h1>
                            <p className="max-w-2xl text-base leading-relaxed text-slate-200 sm:text-lg">
                                Production Calculator helps you model project
                                data, solve target output rates, and optimize
                                implementation constraints with a visual
                                workflow graph.
                            </p>
                        </div>

                        <div className="flex flex-wrap items-center gap-4">
                            <Link
                                to="/explore"
                                className="inline-flex items-center gap-2 rounded-xl border border-cyan-300/50 bg-cyan-500/20 px-5 py-3 text-sm font-semibold text-cyan-50 transition-colors hover:bg-cyan-500/30"
                            >
                                Explore Public Projects
                                <IconArrowRight size={16} />
                            </Link>
                            <Link
                                to="/docs/$"
                                params={{ _splat: "getting-started" }}
                                className="inline-flex items-center gap-2 rounded-xl border border-slate-500/70 bg-slate-900/70 px-5 py-3 text-sm font-semibold text-slate-100 transition-colors hover:border-slate-400 hover:bg-slate-800/80"
                            >
                                Read Getting Started
                            </Link>
                        </div>

                        <div className="grid gap-4 sm:grid-cols-3">
                            <div className="rounded-xl border border-slate-700/80 bg-slate-900/70 px-4 py-4">
                                <p className="text-xs font-semibold tracking-widest text-cyan-200 uppercase">
                                    Linear Solver
                                </p>
                                <p className="mt-1 text-sm text-slate-200">
                                    Demand and supply solved optimally.
                                </p>
                            </div>
                            <div className="rounded-xl border border-slate-700/80 bg-slate-900/70 px-4 py-4">
                                <p className="text-xs font-semibold tracking-widest text-cyan-200 uppercase">
                                    Persistent
                                </p>
                                <p className="mt-1 text-sm text-slate-200">
                                    Keep track of your factory.
                                </p>
                            </div>
                            <div className="rounded-xl border border-slate-700/80 bg-slate-900/70 px-4 py-4">
                                <p className="text-xs font-semibold tracking-widest text-cyan-200 uppercase">
                                    Editable Model
                                </p>
                                <p className="mt-1 text-sm text-slate-200">
                                    Products, recipes, machines, and modifiers.
                                </p>
                            </div>
                        </div>
                    </div>

                    <div className="landing-float rounded-2xl border border-slate-700/70 bg-slate-900/80 p-4 shadow-[0_0_60px_rgba(14,116,144,0.2)] backdrop-blur-sm">
                        <div className="mb-3 flex items-center justify-between px-2">
                            <p className="text-sm font-semibold text-slate-100">
                                Workflow Overview
                            </p>
                            <span className="rounded-full border border-cyan-300/30 bg-cyan-500/15 px-3 py-1 text-xs font-medium text-cyan-100">
                                Graph Context
                            </span>
                        </div>
                        <Image
                            src="/assets/WorkflowExample.png"
                            alt="Workflow view in Production Calculator"
                            width={1208}
                            height={534}
                            priority
                            className="w-full rounded-xl border border-slate-700/80 object-cover"
                        />
                    </div>
                </section>

                <section className="landing-reveal landing-reveal-delay-1 grid gap-8 lg:grid-cols-[1.2fr_0.8fr]">
                    <div className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-7 backdrop-blur-sm">
                        <p className="text-xs font-semibold tracking-[0.11em] text-cyan-200 uppercase">
                            Purpose
                        </p>
                        <h2 className="mt-3 text-3xl font-semibold text-white sm:text-4xl">
                            Plan first, then execute with confidence.
                        </h2>
                        <p className="mt-4 max-w-3xl text-slate-200">
                            Production Calculator is designed to turn spaghetti
                            production lines into a clear, editable model. You
                            define your project once, solve demand from targets,
                            and test whether your current implementation can
                            sustain those rates.
                        </p>

                        <div className="mt-7 space-y-4">
                            {planningSteps.map((step) => {
                                const StepIcon = step.icon;
                                return (
                                    <article
                                        key={step.title}
                                        className="rounded-xl border border-slate-700/70 bg-slate-950/65 p-4"
                                    >
                                        <div className="flex items-start gap-3">
                                            <span className="mt-0.5 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-cyan-400/40 bg-cyan-500/20 text-cyan-100">
                                                <StepIcon size={17} />
                                            </span>
                                            <div>
                                                <h3 className="text-sm font-semibold text-slate-100">
                                                    {step.title}
                                                </h3>
                                                <p className="mt-1 text-sm text-slate-300">
                                                    {step.description}
                                                </p>
                                            </div>
                                        </div>
                                    </article>
                                );
                            })}
                        </div>
                    </div>

                    <div className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-7 backdrop-blur-sm">
                        <p className="text-xs font-semibold tracking-[0.11em] text-cyan-200 uppercase">
                            Use Cases
                        </p>
                        <h2 className="mt-3 text-2xl font-semibold text-white sm:text-3xl">
                            Where it shines most
                        </h2>
                        <div className="mt-6 space-y-4">
                            {useCases.map((useCase) => {
                                const UseCaseIcon = useCase.icon;
                                return (
                                    <article
                                        key={useCase.title}
                                        className="rounded-xl border border-slate-700/70 bg-slate-950/65 p-4"
                                    >
                                        <div className="flex items-start gap-3">
                                            <span className="mt-0.5 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-indigo-400/40 bg-indigo-500/20 text-indigo-100">
                                                <UseCaseIcon size={17} />
                                            </span>
                                            <div>
                                                <h3 className="text-sm font-semibold text-slate-100">
                                                    {useCase.title}
                                                </h3>
                                                <p className="mt-1 text-sm text-slate-300">
                                                    {useCase.description}
                                                </p>
                                            </div>
                                        </div>
                                    </article>
                                );
                            })}
                        </div>
                    </div>
                </section>

                <section className="landing-reveal landing-reveal-delay-2 space-y-6">
                    <div className="max-w-3xl">
                        <p className="text-xs font-semibold tracking-[0.11em] text-cyan-200 uppercase">
                            Features
                        </p>
                        <h2 className="mt-3 text-3xl font-semibold text-white sm:text-4xl">
                            Everything you need to shape and audit production
                            lines
                        </h2>
                    </div>

                    <div className="space-y-6">
                        {featureCards.map((feature) => {
                            const FeatureIcon = feature.icon;
                            return (
                                <article
                                    key={feature.title}
                                    className="grid gap-5 rounded-2xl border border-slate-700/70 bg-slate-900/75 p-4 backdrop-blur-sm md:grid-cols-[1.08fr_0.92fr] md:p-6"
                                >
                                    <div className="overflow-hidden rounded-xl border border-slate-700/80">
                                        <Image
                                            src={feature.image}
                                            alt={feature.imageAlt}
                                            width={1208}
                                            height={534}
                                            className="h-full w-full object-cover"
                                        />
                                    </div>

                                    <div className="flex flex-col justify-center">
                                        <div className="inline-flex w-fit items-center gap-2 rounded-full border border-cyan-300/35 bg-cyan-500/12 px-3 py-1 text-xs font-semibold tracking-[0.07em] text-cyan-100 uppercase">
                                            <FeatureIcon size={14} />
                                            Feature
                                        </div>
                                        <h3 className="mt-3 text-2xl font-semibold text-white">
                                            {feature.title}
                                        </h3>
                                        <p className="mt-3 text-sm leading-relaxed text-slate-300 sm:text-base">
                                            {feature.summary}
                                        </p>
                                        <ul className="mt-4 space-y-2">
                                            {feature.bullets.map((bullet) => (
                                                <li
                                                    key={bullet}
                                                    className="flex items-start gap-2 text-sm text-slate-200"
                                                >
                                                    <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-cyan-300" />
                                                    <span>{bullet}</span>
                                                </li>
                                            ))}
                                        </ul>
                                    </div>
                                </article>
                            );
                        })}
                    </div>
                </section>

                <section className="landing-reveal landing-reveal-delay-3 grid gap-8 lg:grid-cols-2">
                    <div className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-7 backdrop-blur-sm">
                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <div>
                                <p className="text-xs font-semibold tracking-[0.11em] text-cyan-200 uppercase">
                                    Changelog
                                </p>
                                <h2 className="mt-2 text-3xl font-semibold text-white">
                                    Recent progress
                                </h2>
                            </div>
                            <p className="text-sm text-slate-300">
                                Updated {landingRoadmap.lastUpdated}
                            </p>
                        </div>

                        <div className="mt-6 space-y-4">
                            {landingRoadmap.changelog.map((entry) => (
                                <article
                                    key={entry.version}
                                    className="rounded-xl border border-slate-700/70 bg-slate-950/65 p-4"
                                >
                                    <div className="flex flex-wrap items-center justify-between gap-2">
                                        <span className="inline-flex rounded-md border border-cyan-300/35 bg-cyan-500/12 px-2.5 py-1 text-xs font-semibold text-cyan-100">
                                            {entry.version}
                                        </span>
                                        <span className="text-xs text-slate-400">
                                            {entry.date}
                                        </span>
                                    </div>
                                    <h3 className="mt-3 text-base font-semibold text-slate-100">
                                        {entry.title}
                                    </h3>
                                    <ul className="mt-3 space-y-2">
                                        {entry.items.map((item) => (
                                            <li
                                                key={item}
                                                className="flex items-start gap-2 text-sm text-slate-300"
                                            >
                                                <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-indigo-300" />
                                                <span>{item}</span>
                                            </li>
                                        ))}
                                    </ul>
                                </article>
                            ))}
                        </div>
                    </div>

                    <div className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-7 backdrop-blur-sm">
                        <p className="text-xs font-semibold tracking-[0.11em] text-cyan-200 uppercase">
                            Future Plans
                        </p>
                        <h2 className="mt-2 text-3xl font-semibold text-white">
                            What is next
                        </h2>

                        <div className="mt-6 space-y-4">
                            {landingRoadmap.futurePlans.map((plan) => (
                                <article
                                    key={plan.title}
                                    className="rounded-xl border border-slate-700/70 bg-slate-950/65 p-4"
                                >
                                    <div className="flex flex-wrap items-center justify-between gap-2">
                                        <h3 className="text-base font-semibold text-slate-100">
                                            {plan.title}
                                        </h3>
                                        <span
                                            className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${statusBadgeClasses(
                                                plan.status,
                                            )}`}
                                        >
                                            {plan.status}
                                        </span>
                                    </div>
                                    <p className="mt-2 text-sm text-slate-300">
                                        {plan.description}
                                    </p>
                                </article>
                            ))}
                        </div>
                    </div>
                </section>
            </main>

            <style jsx>{`
                .landing-reveal {
                    opacity: 0;
                    transform: translateY(24px);
                    animation: landing-fade-up 0.75s ease forwards;
                }

                .landing-reveal-delay-1 {
                    animation-delay: 0.12s;
                }

                .landing-reveal-delay-2 {
                    animation-delay: 0.24s;
                }

                .landing-reveal-delay-3 {
                    animation-delay: 0.36s;
                }

                .landing-float {
                    animation: landing-float 6s ease-in-out infinite;
                }

                @keyframes landing-fade-up {
                    to {
                        opacity: 1;
                        transform: translateY(0);
                    }
                }

                @keyframes landing-float {
                    0%,
                    100% {
                        transform: translateY(0);
                    }

                    50% {
                        transform: translateY(-8px);
                    }
                }
            `}</style>
        </div>
    );
}
