"use client";

import Navbar from "@/components/NavBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { Link, useNavigate } from "@tanstack/react-router";
import Image from "next/image";
import React, { useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { getApiUrl } from "@/lib/apiUrl";

export default function LoginRoute() {
    const [usernameEntry, setUsernameEntry] = useState("");
    const [passwordEntry, setPasswordEntry] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    const { setLoggedIn, setUserId, setUsername } = useAuth();
    const navigate = useNavigate();

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        setError("");
        setLoading(true);
        try {
            const res = await fetch(getApiUrl("/auth/login"), {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
                body: JSON.stringify({
                    username: usernameEntry,
                    password: passwordEntry,
                }),
            });
            const data = await res.json();
            if (!res.ok) {
                setError(data.message || "Incorrect Username or Password");
                setLoading(false);
                return;
            }
            setLoggedIn(true);
            setUserId(data.puid);
            setUsername(data.username);
            void navigate({ to: "/" });
        } catch (err) {
            setError(
                "An unexpected error occurred: " +
                    (err instanceof Error ? err.message : String(err)),
            );
            setLoading(false);
        }
    }

    return (
        <>
            <Navbar />
            <div className="flex flex-col items-center justify-center min-h-[80vh] bg-slate-950/80">
                <div className="w-full max-w-md bg-slate-900/80 rounded-lg shadow-lg p-8">
                    <h2 className="text-3xl font-bold text-slate-200 mb-6 text-center">
                        Log In
                    </h2>
                    <form
                        className="flex flex-col gap-5"
                        onSubmit={handleSubmit}
                    >
                        <input
                            type="text"
                            placeholder="Username"
                            className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                            required
                            value={usernameEntry}
                            onChange={(e) => setUsernameEntry(e.target.value)}
                        />
                        <input
                            type="password"
                            placeholder="Password"
                            className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                            required
                            value={passwordEntry}
                            onChange={(e) => setPasswordEntry(e.target.value)}
                        />
                        <button
                            type="submit"
                            className="w-full py-3 bg-purple-700 text-white rounded-md font-semibold cursor-pointer hover:bg-purple-600 transition-colors"
                            disabled={loading}
                        >
                            {loading ? "Logging in..." : "Log In"}
                        </button>
                        <ErrorDisplay
                            errors={
                                error
                                    ? [
                                          {
                                              id: "login-error",
                                              message: error,
                                              onDismiss: () => setError(""),
                                          },
                                      ]
                                    : []
                            }
                        />
                    </form>
                    <div className="my-6 flex items-center">
                        <hr className="grow border-slate-700" />
                        <span className="mx-4 text-slate-400">or</span>
                        <hr className="grow border-slate-700" />
                    </div>
                    <div className="flex flex-col gap-3">
                        <button
                            className="w-full py-3 bg-white text-slate-900 rounded-md font-semibold border border-slate-300 flex items-center justify-center gap-2 cursor-not-allowed hover:bg-slate-100 transition-colors"
                            disabled
                        >
                            <Image
                                src="/google-logo.svg"
                                alt=""
                                height={20}
                                width={20}
                            />
                            Continue with Google
                        </button>
                        <Link
                            to="/signup"
                            className="w-full py-3 bg-slate-800 text-purple-400 rounded-md font-semibold text-center border border-slate-700 hover:bg-slate-700 transition-colors no-underline"
                        >
                            Don&apos;t have an account? Sign Up
                        </Link>
                    </div>
                </div>
            </div>
        </>
    );
}
