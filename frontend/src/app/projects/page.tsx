"use client";

import { useEffect } from "react";
import { useProtectedApiFetch } from "@/lib/api";
import NavBar from '@/components/NavBar';

export default function Projects() {
    const protectedApiFetch = useProtectedApiFetch();

    useEffect(() => {
        protectedApiFetch("/api/users/zq9Ln4D92s")
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
    }, []);
    return (
        <>
            <NavBar />
            <div>Project Page</div>
        </>
    );
}