"use client";

import { useEffect } from "react";
import { useProtectedApiFetch } from "@/lib/api";
import NavBar from '@/components/NavBar';
import { useAuth } from "@/context/AuthContext";

export default function Projects() {
    const { userId } = useAuth();
    const protectedApiFetch = useProtectedApiFetch();

    useEffect(() => {
        protectedApiFetch(`/api/users/${userId}/projects`)
        .then(res => {
            if (!res.ok) {
            throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.json();
        })
        .then(data => {
            console.log("User:", data);
        })
        .catch(err => {
            console.error("Failed to fetch user:", err);
        });
    }, [userId]);
    return (
        <>
            <NavBar />
            <div>Project Page</div>
        </>
    );
}