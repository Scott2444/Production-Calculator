"use client";

import { IconHelpHexagon } from "@tabler/icons-react";

export interface DocsHelpLinkProps {
    slug?: string;
    sectionId?: string;
    title: string;
    className?: string;
    iconSize?: number;
}

function normalizeSlug(slug: string | undefined): string {
    if (!slug) return "";
    return slug.replace(/^\/+|\/+$/g, "");
}

export default function DocsHelpLink({
    slug,
    sectionId,
    title,
    className,
    iconSize = 14,
}: DocsHelpLinkProps) {
    const normalizedSlug = normalizeSlug(slug);
    const basePath = normalizedSlug ? `/docs/${normalizedSlug}` : "/docs";
    const hash = sectionId ? `#${encodeURIComponent(sectionId)}` : "";
    const href = `${basePath}${hash}`;

    return (
        <a
            href={href}
            target="_blank"
            rel="noopener noreferrer"
            title={title}
            aria-label={title}
            className={`inline-flex size-5 items-center justify-center text-slate-400 transition-colors hover:border-sky-500/60 hover:text-sky-300 ${className ?? ""}`.trim()}
        >
            <IconHelpHexagon size={iconSize} />
            <span className="sr-only">{title}</span>
        </a>
    );
}
