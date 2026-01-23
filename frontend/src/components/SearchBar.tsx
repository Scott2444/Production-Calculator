import { IconSearch } from "@tabler/icons-react";

export interface SearchBarProps {
    value: string;
    onChange: (newValue: string) => void;
    disabled: boolean;
}

export default function SearchBar({
    value,
    onChange,
    disabled,
}: SearchBarProps): React.ReactElement {
    return (
        <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-4">
            <div className="flex items-center gap-3">
                <div className="text-slate-400">
                    <IconSearch size={18} />
                </div>
                <input
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    placeholder="Search"
                    className="w-full rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-2 text-slate-200 placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-purple-500/40"
                    disabled={disabled}
                    aria-label="Search"
                />
            </div>
        </div>
    );
}
