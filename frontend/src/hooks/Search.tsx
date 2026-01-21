"use client";

import { useMemo, useState } from "react";
import uFuzzy from "@leeoniya/ufuzzy";

export type UseSearchOptions<TItem> = {
    toText?: (item: TItem) => string;
    initialSearchText?: string;
};

export type UseSearchResult<TItem> = {
    searchText: string;
    setSearchText: (next: string) => void;
    filteredItems: TItem[];
};

export function useSearch<TItem>(
    items: TItem[],
    options: UseSearchOptions<TItem> = {},
): UseSearchResult<TItem> {
    const { toText, initialSearchText = "" } = options;
    const [searchText, setSearchText] = useState(initialSearchText);
    const uf = useMemo(() => new uFuzzy(), []);

    const filteredItems = useMemo(() => {
        const needle = searchText.trim();
        if (!needle) return items;

        const haystack = items.map((item) => {
            try {
                if (toText) return toText(item);
                if (typeof item === "string") return item;
                return JSON.stringify(item);
            } catch {
                return String(item);
            }
        });

        const idxs = uf.filter(haystack, needle);
        if (!idxs || idxs.length === 0) return [];
        return idxs.map((idx) => items[idx]);
    }, [items, searchText, toText, uf]);

    return {
        searchText,
        setSearchText,
        filteredItems,
    };
}
