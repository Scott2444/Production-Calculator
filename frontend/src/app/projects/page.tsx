"use client";

import NavBar from '@/components/NavBar';
import DropDown from "@/components/DropDown";
import { fetchUser } from "@/lib/user";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/context/AuthContext";
import { useProtectedApiFetch } from "@/lib/api";

export default function Projects() {
    const { userId } = useAuth();
    const protectedApiFetch = useProtectedApiFetch();
    const { data: user, isLoading, error } = useQuery({
        queryKey: ['user'],
        queryFn: () => fetchUser(userId!, protectedApiFetch),
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
    return (
        <>
            <NavBar />
            <div>Project Page</div>
            <div>{isLoading ? "Loading..." : error ? "Error loading user" : JSON.stringify(user)}</div>
            <DropDown label="Options" align="left">
                <div className="p-4">Dropdown Content</div>
            </DropDown>
        </>
    );
}