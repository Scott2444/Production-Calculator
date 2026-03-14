const apiBaseUrl =
    process.env.NEXT_PUBLIC_API_BASE_URL?.trim().replace(/\/+$/, "") ?? "";

export function getApiUrl(path: string): string {
    if (/^https?:\/\//i.test(path)) {
        return path;
    }

    const normalizedPath = path.startsWith("/") ? path : `/${path}`;

    if (!apiBaseUrl) {
        return normalizedPath;
    }

    return `${apiBaseUrl}${normalizedPath}`;
}
