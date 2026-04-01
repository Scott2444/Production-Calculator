"use client";

import Navbar from "@/components/NavBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { getApiUrl } from "@/lib/apiUrl";
import { Link } from "@tanstack/react-router";
import React, { useState } from "react";

function isValidEmail(email: string) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

export default function ForgotPasswordRoute() {
    const [email, setEmail] = useState("");
    const [error, setError] = useState("");
    const [status, setStatus] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        if (loading) return;

        setError("");
        setStatus("");

        const normalizedEmail = email.trim().toLowerCase();
        if (!isValidEmail(normalizedEmail)) {
            setError("Please enter a valid email address.");
            return;
        }

        setLoading(true);
        try {
            const res = await fetch(getApiUrl("/auth/request-password-reset"), {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email: normalizedEmail }),
            });

            if (!res.ok) {
                let message =
                    "Unable to request a password reset right now. Please try again.";
                try {
                    const data = await res.json();
                    message = data?.message || data?.error || message;
                } catch {
                    // Ignore parse issues and keep generic fallback message.
                }
                setError(message);
                return;
            }

            setStatus("We sent a password reset link to your email.");
        } catch {
            setError("An unexpected error occurred.");
        } finally {
            setLoading(false);
        }
    }

    return (
        <>
            <Navbar />
            <div className="flex flex-col items-center justify-center min-h-[80vh] bg-slate-950/80 px-4">
                <div className="w-full max-w-md bg-slate-900/80 rounded-lg shadow-lg p-8 border border-slate-800">
                    <h2 className="text-3xl font-bold text-slate-200 mb-2 text-center">
                        Forgot Password
                    </h2>
                    <p className="text-slate-400 text-center mb-6">
                        Enter your email and we&apos;ll send you a reset link.
                    </p>

                    <form
                        className="flex flex-col gap-5"
                        onSubmit={handleSubmit}
                    >
                        <input
                            type="email"
                            placeholder="Email"
                            className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                            required
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                        />
                        <button
                            type="submit"
                            className="w-full py-3 bg-purple-700 text-white rounded-md font-semibold cursor-pointer hover:bg-purple-600 transition-colors disabled:opacity-60"
                            disabled={loading}
                        >
                            {loading ? "Sending..." : "Send Reset Link"}
                        </button>

                        <ErrorDisplay
                            errors={
                                error
                                    ? [
                                          {
                                              id: "forgot-password-error",
                                              message: error,
                                              onDismiss: () => setError(""),
                                          },
                                      ]
                                    : []
                            }
                        />

                        {status && (
                            <div className="text-center rounded-md px-4 py-3 border text-emerald-200 border-emerald-900/40 bg-emerald-950/20">
                                {status}
                            </div>
                        )}
                    </form>

                    <div className="my-6 flex items-center">
                        <hr className="grow border-slate-700" />
                        <span className="mx-4 text-slate-400">or</span>
                        <hr className="grow border-slate-700" />
                    </div>

                    <div className="flex flex-col gap-3">
                        <Link
                            to="/login"
                            className="w-full py-3 bg-slate-800 text-purple-400 rounded-md font-semibold text-center border border-slate-700 hover:bg-slate-700 transition-colors no-underline"
                        >
                            Back to Log In
                        </Link>
                        <Link
                            to="/signup"
                            className="w-full py-3 bg-slate-800 text-purple-400 rounded-md font-semibold text-center border border-slate-700 hover:bg-slate-700 transition-colors no-underline"
                        >
                            Need an account? Sign Up
                        </Link>
                    </div>
                </div>
            </div>
        </>
    );
}
