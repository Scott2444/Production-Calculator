"use client";

import NavBar from "@/components/NavBar";
import { useAuth } from "@/context/AuthContext";
import { useProtectedApi } from "@/lib/api";
import { fetchUser } from "@/lib/user";
import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import React, { useEffect, useMemo, useRef, useState } from "react";

function maskEmail(email: string) {
    const [local, domain] = email.split("@");
    if (!local || !domain) return email;
    const maskedLocal =
        local.length <= 2
            ? `${local[0] ?? ""}*`
            : `${local.slice(0, 2)}***${local.slice(-1)}`;
    const domainParts = domain.split(".");
    const tld =
        domainParts.length > 1 ? domainParts[domainParts.length - 1] : "";
    const domainName = domainParts[0] ?? domain;
    const maskedDomainName =
        domainName.length <= 2
            ? `${domainName[0] ?? ""}*`
            : `${domainName.slice(0, 2)}***${domainName.slice(-1)}`;
    const rest =
        domainParts.length > 2 ? `.${domainParts.slice(1, -1).join(".")}` : "";
    const suffix = tld ? `.${tld}` : "";
    return `${maskedLocal}@${maskedDomainName}${rest}${suffix}`;
}

function isDigit(value: string) {
    return /^[0-9]$/.test(value);
}

export default function Verify() {
    const router = useRouter();
    const { loggedIn, userId } = useAuth();
    const protectedApi = useProtectedApi();

    const { data: user, isLoading: isUserLoading } = useQuery({
        queryKey: ["user", userId],
        queryFn: () => fetchUser(userId!, protectedApi),
        staleTime: 5 * 60 * 1000,
        enabled: Boolean(userId),
    });

    const [digits, setDigits] = useState<string[]>(["", "", "", "", "", ""]);
    const [status, setStatus] = useState<string>("");
    const [error, setError] = useState<string>("");
    const [requestLoading, setRequestLoading] = useState(false);
    const [verifyLoading, setVerifyLoading] = useState(false);
    const [cooldownSeconds, setCooldownSeconds] = useState(0);

    const inputsRef = useRef<Array<HTMLInputElement | null>>([]);

    const code = useMemo(() => digits.join(""), [digits]);

    useEffect(() => {
        if (!loggedIn) return;
        if (!userId) return;
        const timer = window.setInterval(() => {
            setCooldownSeconds((s) => (s > 0 ? s - 1 : 0));
        }, 1000);
        return () => window.clearInterval(timer);
    }, [loggedIn, userId]);

    useEffect(() => {
        if (!loggedIn || !userId || !user?.email) return;
        const alreadyAutoRequested =
            sessionStorage.getItem("verificationCodeAutoRequested") === "1";
        if (alreadyAutoRequested) return;
        sessionStorage.setItem("verificationCodeAutoRequested", "1");
        void requestCode();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [loggedIn, userId, user?.email]);

    useEffect(() => {
        if (!loggedIn) return;
        if (!userId) return;
        if (!code) return;
        if (code.length !== 6) return;
        if (!/^[0-9]{6}$/.test(code)) return;
        // Optional: auto-submit once all digits are present.
        // Keep disabled for now to avoid accidental submits.
    }, [loggedIn, userId, code]);

    async function requestCode() {
        if (requestLoading) return;
        if (cooldownSeconds > 0) return;
        setError("");
        setStatus("");
        setRequestLoading(true);
        try {
            const res = await protectedApi("/api/auth/request-code", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
            });
            if (!res.ok) {
                let message = "Unable to send a verification code.";
                try {
                    const data = await res.json();
                    message = data?.message || data?.error || message;
                } catch {
                    // ignore
                }
                setError(message);
                return;
            }
            setStatus("Verification code sent. Check your email.");
            setCooldownSeconds(30);
        } catch {
            setError("An unexpected error occurred.");
        } finally {
            setRequestLoading(false);
        }
    }

    async function verifyCode(e: React.FormEvent) {
        e.preventDefault();
        if (verifyLoading) return;
        setError("");
        setStatus("");

        const trimmed = code.trim();
        if (!/^[0-9]{6}$/.test(trimmed)) {
            setError("Please enter the 6-digit code.");
            return;
        }

        setVerifyLoading(true);
        try {
            const res = await protectedApi("/api/auth/verify-code", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ code: trimmed }),
            });
            if (!res.ok) {
                let message = "Invalid or expired verification code.";
                try {
                    const data = await res.json();
                    message = data?.message || data?.error || message;
                } catch {
                    // ignore
                }
                setError(message);
                return;
            }

            setStatus("Verified successfully. Redirecting...");
            // Refresh access token so the JWT role updates post-verification.
            await fetch("/api/auth/refresh", { method: "POST" });
            router.push("/");
        } catch {
            setError("An unexpected error occurred.");
        } finally {
            setVerifyLoading(false);
        }
    }

    function setDigitAt(index: number, value: string) {
        setDigits((prev) => {
            const next = [...prev];
            next[index] = value;
            return next;
        });
    }

    function handlePaste(
        e: React.ClipboardEvent<HTMLInputElement>,
        startIndex: number,
    ) {
        const text = e.clipboardData.getData("text").trim();
        if (!text) return;
        const cleaned = text.replace(/\D/g, "").slice(0, 6);
        if (cleaned.length === 0) return;
        e.preventDefault();

        setDigits((prev) => {
            const next = [...prev];
            for (let i = 0; i < cleaned.length && startIndex + i < 6; i++) {
                next[startIndex + i] = cleaned[i];
            }
            return next;
        });

        const nextIndex = Math.min(startIndex + cleaned.length, 5);
        inputsRef.current[nextIndex]?.focus();
    }

    function handleKeyDown(
        e: React.KeyboardEvent<HTMLInputElement>,
        index: number,
    ) {
        if (e.key === "Backspace") {
            if (digits[index]) {
                setDigitAt(index, "");
                return;
            }
            if (index > 0) {
                inputsRef.current[index - 1]?.focus();
                setDigitAt(index - 1, "");
            }
        }
        if (e.key === "ArrowLeft" && index > 0)
            inputsRef.current[index - 1]?.focus();
        if (e.key === "ArrowRight" && index < 5)
            inputsRef.current[index + 1]?.focus();
    }

    useEffect(() => {
        if (!loggedIn) return;
        // Focus first digit by default.
        inputsRef.current[0]?.focus();
    }, [loggedIn]);

    if (!loggedIn) {
        return (
            <>
                <NavBar />
                <div className="flex flex-col items-center justify-center min-h-[80vh] bg-slate-950/80 px-4">
                    <div className="w-full max-w-md bg-slate-900/80 rounded-lg shadow-lg p-8 border border-slate-800">
                        <h2 className="text-3xl font-bold text-slate-200 mb-2 text-center">
                            Verify Your Account
                        </h2>
                        <p className="text-slate-400 text-center mb-6">
                            Please log in to verify your account.
                        </p>
                        <Link
                            href="/login"
                            className="w-full block py-3 bg-purple-700 text-white rounded-md font-semibold text-center hover:bg-purple-600 transition-colors no-underline"
                        >
                            Go to Login
                        </Link>
                    </div>
                </div>
            </>
        );
    }

    return (
        <>
            <NavBar />
            <div className="flex flex-col items-center justify-center min-h-[80vh] bg-slate-950/80 px-4">
                <div className="w-full max-w-lg bg-slate-900/80 rounded-lg shadow-lg p-8 border border-slate-800">
                    <div className="flex flex-col gap-2 mb-6">
                        <h2 className="text-3xl font-bold text-slate-200 text-center">
                            Verify Your Account
                        </h2>
                        <p className="text-slate-400 text-center">
                            Enter the 6-digit code we sent to your email.
                        </p>
                    </div>

                    <div className="bg-slate-950/40 border border-slate-800 rounded-lg p-4 mb-6">
                        <div className="flex items-center justify-between gap-3 flex-wrap">
                            <div className="text-slate-300">
                                <div className="text-sm text-slate-400">
                                    Email
                                </div>
                                <div className="font-medium">
                                    {isUserLoading
                                        ? "Loading..."
                                        : (user?.email ?? "")}
                                </div>
                            </div>
                            <button
                                type="button"
                                onClick={requestCode}
                                disabled={requestLoading || cooldownSeconds > 0}
                                className="px-4 py-2 bg-slate-800 text-purple-300 rounded-md font-semibold border border-slate-700 hover:bg-slate-700 transition-colors cursor-pointer disabled:opacity-60 disabled:cursor-not-allowed"
                            >
                                {requestLoading
                                    ? "Sending..."
                                    : cooldownSeconds > 0
                                      ? `Resend in ${cooldownSeconds}s`
                                      : "Resend Code"}
                            </button>
                        </div>
                        <p className="text-xs text-slate-500 mt-3">
                            Didn’t get it? Check spam/junk or resend a new code.
                        </p>
                    </div>

                    <form onSubmit={verifyCode} className="flex flex-col gap-5">
                        <div className="flex items-center justify-center gap-2">
                            {digits.map((d, i) => (
                                <input
                                    key={i}
                                    ref={(el) => {
                                        inputsRef.current[i] = el;
                                    }}
                                    inputMode="numeric"
                                    autoComplete={
                                        i === 0 ? "one-time-code" : "off"
                                    }
                                    pattern="[0-9]*"
                                    maxLength={1}
                                    value={d}
                                    onPaste={(e) => handlePaste(e, i)}
                                    onKeyDown={(e) => handleKeyDown(e, i)}
                                    onChange={(e) => {
                                        const value = e.target.value;
                                        if (value === "") {
                                            setDigitAt(i, "");
                                            return;
                                        }
                                        const lastChar =
                                            value[value.length - 1] ?? "";
                                        if (!isDigit(lastChar)) return;
                                        setDigitAt(i, lastChar);
                                        if (i < 5)
                                            inputsRef.current[i + 1]?.focus();
                                    }}
                                    className="w-12 h-12 text-center text-lg font-semibold tracking-widest rounded-md bg-slate-800 text-slate-200 border border-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-700"
                                    aria-label={`Digit ${i + 1}`}
                                />
                            ))}
                        </div>

                        <button
                            type="submit"
                            className="w-full py-3 bg-purple-700 text-white rounded-md font-semibold hover:bg-purple-600 transition-colors disabled:opacity-60"
                            disabled={verifyLoading}
                        >
                            {verifyLoading ? "Verifying..." : "Verify"}
                        </button>

                        {(status || error) && (
                            <div
                                className={`text-center rounded-md px-4 py-3 border ${
                                    error
                                        ? "text-red-300 border-red-900/50 bg-red-950/30"
                                        : "text-emerald-200 border-emerald-900/40 bg-emerald-950/20"
                                }`}
                            >
                                {error || status}
                            </div>
                        )}
                    </form>

                    <div className="mt-6 text-center">
                        <Link
                            href="/"
                            className="text-slate-400 hover:text-purple-300 transition-colors no-underline"
                        >
                            Back to Home
                        </Link>
                    </div>
                </div>
            </div>
        </>
    );
}
