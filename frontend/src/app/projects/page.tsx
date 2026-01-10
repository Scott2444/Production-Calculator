"use client";

import { useEffect } from "react";

export default function Projects() {
    useEffect(() => {
        fetch("http://localhost:5076/users/zq9Ln4D92s", { credentials: "include" })
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
    return <div>Projects Page</div>;
}