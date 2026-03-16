export function formatTimestamp(value: string): string {
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return "Unknown";
    return parsed.toLocaleString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
}
