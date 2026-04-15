"use client";

import Navbar from "@/components/NavBar";
import { Link, useNavigate } from "@tanstack/react-router";
import Image from "next/image";
import React, { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useAuth } from "@/context/AuthContext";
import { getApiUrl } from "@/lib/apiUrl";

export default function SignUpRoute() {
    const [step, setStep] = useState(1);
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);
    const { setLoggedIn, setUserId } = useAuth();

    const navigate = useNavigate();

    async function handleNext(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        setError("");
        if (!/^[a-zA-Z0-9_-]{6,20}$/.test(username)) {
            setError(
                "Username must be 6-20 characters, letters, numbers, _ or -",
            );
            return;
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            setError("Please enter a valid email address.");
            return;
        }
        setLoading(true);
        let isValid = false;
        try {
            const res = await fetch(getApiUrl("/users/validate"), {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
                body: JSON.stringify({ username, email }),
            });
            isValid = res.ok;
            if (!isValid) {
                const data = await res.json();
                setError(data.error || "User or email already in use");
                setLoading(false);
                return;
            }
        } catch {
            setError("An unexpected error occurred");
            setLoading(false);
        }
        setLoading(false);
        if (!isValid) return;
        setStep(2);
    }

    function handleBack() {
        setError("");
        setStep(1);
    }

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        setError("");

        if (password.length < 8 || password.length > 32) {
            setError("Password must be 8-32 characters.");
            return;
        }
        if (password !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

        setLoading(true);
        try {
            const registerRes = await fetch(getApiUrl("/users/register"), {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
                body: JSON.stringify({ username, email, password }),
            });
            const data = await registerRes.json();
            if (!registerRes.ok) {
                setError(data.error || "Signup failed");
                setLoading(false);
                return;
            }
            await login();
            void navigate({ to: "/verify" });
        } catch {
            setError("An unexpected error occurred");
            setLoading(false);
        }
    }

    async function login() {
        setError("");
        try {
            const res = await fetch(getApiUrl("/auth/login"), {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "include",
                body: JSON.stringify({ username, password }),
            });
            const data = await res.json();
            if (!res.ok) {
                return;
            }
            setLoggedIn(true);
            setUserId(data.puid);
        } catch {
            setError("An unexpected error occurred");
        }
    }

    return (
        <>
            <Navbar />
            <div className="flex flex-col items-center justify-center min-h-[80vh] bg-slate-950/80">
                <AnimatePresence mode="wait">
                    {step === 1 && (
                        <motion.div
                            key="step1"
                            initial={{ x: 0, opacity: 0 }}
                            animate={{ x: 0, opacity: 1 }}
                            exit={{ x: -300, opacity: 0 }}
                            transition={{ duration: 0.4 }}
                            className="w-full max-w-md"
                        >
                            <div className="bg-slate-900/80 rounded-lg shadow-lg p-8">
                                <h2 className="text-3xl font-bold text-slate-200 mb-6 text-center">
                                    Sign Up
                                </h2>
                                <form
                                    className="flex flex-col gap-5"
                                    onSubmit={handleNext}
                                >
                                    <input
                                        type="text"
                                        placeholder="Username"
                                        className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                                        required
                                        value={username}
                                        onChange={(e) =>
                                            setUsername(e.target.value)
                                        }
                                    />
                                    <input
                                        type="email"
                                        placeholder="Email"
                                        className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                                        required
                                        value={email}
                                        onChange={(e) =>
                                            setEmail(e.target.value)
                                        }
                                    />
                                    <button
                                        type="submit"
                                        className="w-full py-3 bg-purple-700 text-white rounded-md font-semibold hover:bg-purple-600 transition-colors cursor-pointer"
                                        disabled={loading}
                                    >
                                        {loading ? "Validating..." : "Next"}
                                    </button>
                                </form>
                                {error && (
                                    <div className="text-red-500 text-center mt-2 w-full left-0">
                                        {error}
                                    </div>
                                )}
                                <div className="my-6 flex items-center">
                                    <hr className="grow border-slate-700" />
                                    <span className="mx-4 text-slate-400">
                                        or
                                    </span>
                                    <hr className="grow border-slate-700" />
                                </div>
                                <div className="flex flex-col gap-3">
                                    {/* <button
                                        className="w-full py-3 bg-white text-slate-900 rounded-md font-semibold border border-slate-300 flex items-center justify-center gap-2 hover:bg-slate-100 transition-colors"
                                        disabled
                                    >
                                        <Image
                                            src="/google-logo.svg"
                                            alt="Google"
                                            height={20}
                                            width={20}
                                        />
                                        Continue with Google
                                    </button> */}
                                    <Link
                                        to="/login"
                                        className="w-full py-3 bg-slate-800 text-purple-400 rounded-md font-semibold text-center border border-slate-700 hover:bg-slate-700 transition-colors no-underline"
                                    >
                                        Already have an account? Log In
                                    </Link>
                                </div>
                            </div>
                        </motion.div>
                    )}
                    {step === 2 && (
                        <motion.div
                            key="step2"
                            initial={{ x: 300, opacity: 0 }}
                            animate={{ x: 0, opacity: 1 }}
                            exit={{ x: -300, opacity: 0 }}
                            transition={{ duration: 0.4 }}
                            className="w-full max-w-md"
                        >
                            <div className="bg-slate-900/80 rounded-lg shadow-lg p-8">
                                <h2 className="text-3xl font-bold text-slate-200 mb-6 text-center">
                                    Sign Up
                                </h2>
                                <form
                                    className="flex flex-col gap-5"
                                    onSubmit={handleSubmit}
                                >
                                    <input
                                        type="password"
                                        placeholder="Password (8-32 characters)"
                                        className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                                        required
                                        value={password}
                                        onChange={(e) =>
                                            setPassword(e.target.value)
                                        }
                                    />
                                    <input
                                        type="password"
                                        placeholder="Confirm Password"
                                        className="px-4 py-3 rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                                        required
                                        value={confirmPassword}
                                        onChange={(e) =>
                                            setConfirmPassword(e.target.value)
                                        }
                                    />
                                    <div className="flex flex-row gap-3">
                                        <button
                                            type="button"
                                            className="w-1/2 py-3 bg-slate-700 text-white rounded-md font-semibold hover:bg-slate-600 transition-colors cursor-pointer"
                                            onClick={handleBack}
                                            disabled={loading}
                                        >
                                            Back
                                        </button>
                                        <button
                                            type="submit"
                                            className="w-1/2 py-3 bg-purple-700 text-white rounded-md font-semibold hover:bg-purple-600 transition-colors cursor-pointer"
                                            disabled={loading}
                                        >
                                            {loading
                                                ? "Signing up..."
                                                : "Sign Up"}
                                        </button>
                                    </div>
                                </form>
                                {error && (
                                    <div className="text-red-500 text-center mt-2 w-full left-0">
                                        {error}
                                    </div>
                                )}
                                <div className="my-6 flex items-center">
                                    <hr className="grow border-slate-700" />
                                    <span className="mx-4 text-slate-400">
                                        or
                                    </span>
                                    <hr className="grow border-slate-700" />
                                </div>
                                <div className="flex flex-col gap-3">
                                    {/* <button
                                        className="w-full py-3 bg-white text-slate-900 rounded-md font-semibold border border-slate-300 flex items-center justify-center gap-2 hover:bg-slate-100 transition-colors"
                                        disabled
                                    >
                                        <Image
                                            src="/google-logo.svg"
                                            alt="Google"
                                            height={20}
                                            width={20}
                                        />
                                        Continue with Google
                                    </button> */}
                                    <Link
                                        to="/login"
                                        className="w-full py-3 bg-slate-800 text-purple-400 rounded-md font-semibold text-center border border-slate-700 hover:bg-slate-700 transition-colors no-underline"
                                    >
                                        Already have an account? Log In
                                    </Link>
                                </div>
                            </div>
                        </motion.div>
                    )}
                </AnimatePresence>
            </div>
        </>
    );
}
