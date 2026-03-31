import type { ComponentPropsWithoutRef } from "react";
import type { MDXComponents } from "mdx/types";

function cn(...values: Array<string | undefined>) {
    return values.filter(Boolean).join(" ");
}

export function useMDXComponents(components: MDXComponents): MDXComponents {
    return {
        h1: ({ className, ...props }: ComponentPropsWithoutRef<"h1">) => (
            <h1
                className={cn(
                    "text-3xl font-semibold tracking-tight text-slate-100",
                    className,
                )}
                {...props}
            />
        ),
        h2: ({ className, ...props }: ComponentPropsWithoutRef<"h2">) => (
            <h2
                className={cn(
                    "mt-9 text-2xl font-semibold tracking-tight text-slate-100",
                    className,
                )}
                {...props}
            />
        ),
        h3: ({ className, ...props }: ComponentPropsWithoutRef<"h3">) => (
            <h3
                className={cn(
                    "mt-7 text-xl font-semibold text-slate-100",
                    className,
                )}
                {...props}
            />
        ),
        p: ({ className, ...props }: ComponentPropsWithoutRef<"p">) => (
            <p
                className={cn("mt-4 leading-7 text-slate-200", className)}
                {...props}
            />
        ),
        ul: ({ className, ...props }: ComponentPropsWithoutRef<"ul">) => (
            <ul
                className={cn(
                    "mt-4 list-disc space-y-2 pl-6 text-slate-200",
                    className,
                )}
                {...props}
            />
        ),
        ol: ({ className, ...props }: ComponentPropsWithoutRef<"ol">) => (
            <ol
                className={cn(
                    "mt-4 list-decimal space-y-2 pl-6 text-slate-200",
                    className,
                )}
                {...props}
            />
        ),
        li: ({ className, ...props }: ComponentPropsWithoutRef<"li">) => (
            <li className={cn("leading-7", className)} {...props} />
        ),
        a: ({ className, ...props }: ComponentPropsWithoutRef<"a">) => (
            <a
                className={cn(
                    "font-medium text-sky-300 underline decoration-sky-500/50 underline-offset-4 hover:text-sky-200",
                    className,
                )}
                {...props}
            />
        ),
        blockquote: ({
            className,
            ...props
        }: ComponentPropsWithoutRef<"blockquote">) => (
            <blockquote
                className={cn(
                    "mt-6 border-l-4 border-slate-600 pl-4 italic text-slate-300",
                    className,
                )}
                {...props}
            />
        ),
        hr: ({ className, ...props }: ComponentPropsWithoutRef<"hr">) => (
            <hr
                className={cn("my-10 border-slate-700", className)}
                {...props}
            />
        ),
        code: ({ className, ...props }: ComponentPropsWithoutRef<"code">) => (
            <code
                className={cn(
                    "rounded bg-slate-800 px-1.5 py-0.5 text-sm text-slate-100",
                    className,
                )}
                {...props}
            />
        ),
        pre: ({ className, ...props }: ComponentPropsWithoutRef<"pre">) => (
            <pre
                className={cn(
                    "mt-4 overflow-x-auto rounded-xl border border-slate-700 bg-slate-950 p-4 text-slate-100",
                    className,
                )}
                {...props}
            />
        ),
        ...components,
    };
}
