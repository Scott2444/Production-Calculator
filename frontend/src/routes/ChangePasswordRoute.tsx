"use client";

import Navbar from "@/components/NavBar";
import ErrorDisplay from "@/components/ErrorDisplay";
import { getApiUrl } from "@/lib/apiUrl";
import { Link, useRouterState } from "@tanstack/react-router";
import React, { useMemo, useState } from "react";

export default function ChangePasswordRoute() {
    const search = useRouterState({
        select: (state) => state.location.search,
    });

    const token = useMemo(() => {
        const value = new URLSearchParams(search).get("token");
        return value?.trim() ?? "";
    }, [search]);

    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const [status, setStatus] = useState("");
    const [loading, setLoading] = useState(false);
    const [isComplete, setIsComplete] = useState(false);

    const hasValidToken = token.length > 0;

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        if (loading || !hasValidToken || isComplete) return;

        setError("");
        setStatus("");

        if (newPassword.length < 8 || newPassword.length > 32) {
            setError("Password must be 8-32 characters.");
            return;
        }
        if (newPassword !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

        setLoading(true);
        try {
            const res = await fetch(getApiUrl("/auth/reset-password"), {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
                body: JSON.stringify({ token, newPassword }),
            });

            if (!res.ok) {
                let message = "Unable to reset password.";
                try {
                    const data = await res.json();
                    message = data?.message || data?.error || message;
                } catch {
                    // Ignore parse issues and keep generic fallback message.
                }
                setError(message);
                return;
            }

            setIsComplete(true);
            setStatus("Password updated successfully. You can now log in.");
            setNewPassword("");
            setConfirmPassword("");
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
                        Change Password
                    </h2>
                    <p className="text-slate-400 text-center mb-6">
                        Create a new password for your account.
                    </p>

                    {!hasValidToken && (
                        <div className="mb-5 rounded-md px-4 py-3 border border-amber-900/50 bg-amber-950/20 text-amber-200 text-sm">
                            This reset link is invalid. Request a new password
                            reset email.
                        </div>
                    )}

                    <form
                        className="flex flex-col gap-5"
                        onSubmit={handleSubmit}
                    >
                        <input
                            type="password"
                            placeholder="New Password (8-32 characters)"
                            className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                            required
                            value={newPassword}
                            onChange={(e) => setNewPassword(e.target.value)}
                            disabled={!hasValidToken || isComplete}
                        />
                        <input
                            type="password"
                            placeholder="Confirm New Password"
                            className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                            required
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            disabled={!hasValidToken || isComplete}
                        />
                        <button
                            type="submit"
                            className="w-full py-3 bg-purple-700 text-white rounded-md font-semibold cursor-pointer hover:bg-purple-600 transition-colors disabled:opacity-60"
                            disabled={!hasValidToken || loading || isComplete}
                        >
                            {isComplete
                                ? "Password Updated"
                                : loading
                                  ? "Updating..."
                                  : "Update Password"}
                        </button>

                        <ErrorDisplay
                            errors={
                                error
                                    ? [
                                          {
                                              id: "change-password-error",
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
                        {isComplete ? (
                            <Link
                                to="/login"
                                className="w-full py-3 bg-purple-700 text-white rounded-md font-semibold text-center hover:bg-purple-600 transition-colors no-underline"
                            >
                                Continue to Log In
                            </Link>
                        ) : (
                            <Link
                                to="/reset-password"
                                className="w-full py-3 bg-slate-800 text-purple-400 rounded-md font-semibold text-center border border-slate-700 hover:bg-slate-700 transition-colors no-underline"
                            >
                                Request a New Reset Link
                            </Link>
                        )}
                    </div>
                </div>
            </div>
        </>
    );
}
