"use client";

import NavBar from "@/components/NavBar";
import Popup from "@/components/Popup";
import { useAuth } from "@/context/AuthContext";
import { useUserQuery } from "@/hooks/useQueries";
import { useProtectedApi } from "@/lib/api";
import { useLogout } from "@/lib/logout";
import { deleteUser } from "@/lib/user";
import { useMutation } from "@tanstack/react-query";
import { Link, useNavigate } from "@tanstack/react-router";
import {
    IconAlertTriangle,
    IconShieldCheck,
    IconTrash,
} from "@tabler/icons-react";
import { useEffect, useMemo, useRef, useState } from "react";

function formatTimestamp(value: string | undefined): string {
    if (!value) return "Unavailable";

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "Unavailable";

    return date.toLocaleString();
}

export default function SettingsRoute() {
    const navigate = useNavigate();
    const protectedApi = useProtectedApi();
    const logout = useLogout();
    const { loggedIn, isHydrated, userId } = useAuth();

    const {
        data: user,
        isLoading,
        error,
    } = useUserQuery(userId, {
        enabled: loggedIn && Boolean(userId),
    });

    const [deletePassword, setDeletePassword] = useState("");
    const [deleteError, setDeleteError] = useState<string>("");
    const [deletePopupOpen, setDeletePopupOpen] = useState(false);
    const deletePasswordRef = useRef<HTMLInputElement>(null);

    const deleteUserMutation = useMutation({
        mutationFn: async () => {
            if (!userId || !user?.username) {
                throw new Error("Unable to load account details for deletion.");
            }

            await deleteUser(
                userId,
                {
                    username: user.username,
                    password: deletePassword,
                },
                protectedApi,
            );
        },
        onSuccess: async () => {
            setDeleteError("");
            setDeletePassword("");
            setDeletePopupOpen(false);
            await logout();
            void navigate({ to: "/" });
        },
        onError: (mutationError) => {
            setDeleteError(
                mutationError instanceof Error
                    ? mutationError.message
                    : "Failed to delete user.",
            );
        },
    });

    const userErrorMessage =
        error instanceof Error ? error.message : "Failed to load account.";
    const canDelete = Boolean(deletePassword.trim());

    useEffect(() => {
        if (deletePopupOpen) {
            setDeletePassword("");
            setDeleteError("");
        }
    }, [deletePopupOpen]);

    const createdAt = useMemo(
        () => formatTimestamp(user?.createdAt),
        [user?.createdAt],
    );
    const updatedAt = useMemo(
        () => formatTimestamp(user?.updatedAt),
        [user?.updatedAt],
    );

    function handleDeleteAccount(event: React.FormEvent<HTMLFormElement>) {
        event.preventDefault();
        if (deleteUserMutation.isPending) return;

        setDeleteError("");

        if (!deletePassword.trim()) {
            setDeleteError("Please enter your password.");
            return;
        }

        deleteUserMutation.mutate();
    }

    if (!isHydrated) {
        return (
            <>
                <NavBar />
                <div className="flex min-h-[80vh] items-center justify-center bg-slate-950/80 px-4">
                    <div className="w-full max-w-2xl rounded-2xl border border-slate-800 bg-slate-900/70 p-8">
                        <div className="h-7 w-56 animate-pulse rounded-md bg-slate-700/80" />
                        <div className="mt-4 h-4 w-72 animate-pulse rounded-md bg-slate-700/60" />
                        <div className="mt-8 h-36 animate-pulse rounded-xl bg-slate-800/70" />
                    </div>
                </div>
            </>
        );
    }

    if (!loggedIn) {
        return (
            <>
                <NavBar />
                <div className="flex min-h-[80vh] flex-col items-center justify-center bg-slate-950/80 px-4">
                    <div className="w-full max-w-md rounded-lg border border-slate-800 bg-slate-900/80 p-8 shadow-lg">
                        <h2 className="mb-2 text-center text-3xl font-bold text-slate-200">
                            Account Settings
                        </h2>
                        <p className="mb-6 text-center text-slate-400">
                            Please log in to view and manage your account.
                        </p>
                        <Link
                            to="/login"
                            className="block w-full rounded-md bg-purple-700 py-3 text-center font-semibold text-white no-underline transition-colors hover:bg-purple-600"
                        >
                            Go to Login
                        </Link>
                    </div>
                </div>
            </>
        );
    }

    return (
        <div className="relative min-h-screen overflow-x-hidden bg-slate-950 text-slate-100">
            <div className="pointer-events-none absolute inset-0">
                <div className="absolute -top-30 left-1/2 h-136 w-136 -translate-x-1/2 rounded-full bg-indigo-500/14 blur-3xl" />
                <div className="absolute top-60 -left-14 h-72 w-72 rounded-full bg-blue-500/10 blur-3xl" />
                <div className="absolute bottom-12 right-0 h-96 w-96 rounded-full bg-indigo-500/10 blur-3xl" />
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(148,163,184,0.14)_1px,transparent_1px)] bg-size-[22px_22px] opacity-35" />
            </div>

            <NavBar />

            <main className="relative mx-auto flex w-full max-w-5xl flex-col gap-6 px-6 pb-16 pt-10 md:px-10">
                <section className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-7 backdrop-blur-sm">
                    <h1 className="text-3xl font-semibold text-white">
                        Account Settings
                    </h1>
                    <p className="mt-2 max-w-3xl text-slate-300">
                        Manage your account profile, verification status, and
                        security-sensitive actions.
                    </p>
                </section>

                {isLoading && (
                    <section className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-7 backdrop-blur-sm">
                        <div className="h-6 w-52 animate-pulse rounded-md bg-slate-700/80" />
                        <div className="mt-5 grid gap-3 sm:grid-cols-2">
                            <div className="h-16 animate-pulse rounded-lg bg-slate-800/70" />
                            <div className="h-16 animate-pulse rounded-lg bg-slate-800/70" />
                            <div className="h-16 animate-pulse rounded-lg bg-slate-800/70" />
                            <div className="h-16 animate-pulse rounded-lg bg-slate-800/70" />
                        </div>
                    </section>
                )}

                {!isLoading && error && (
                    <section className="rounded-2xl border border-red-900/60 bg-red-950/25 p-6">
                        <h2 className="text-xl font-semibold text-red-200">
                            Unable to load account
                        </h2>
                        <p className="mt-2 text-red-100/90">
                            {userErrorMessage}
                        </p>
                    </section>
                )}

                {!isLoading && user && (
                    <>
                        <section className="grid gap-6 lg:grid-cols-[1.25fr_0.75fr]">
                            <div className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-6 backdrop-blur-sm">
                                <h2 className="text-xl font-semibold text-white">
                                    Profile Information
                                </h2>
                                <div className="mt-5 grid gap-3 sm:grid-cols-2">
                                    <div className="rounded-lg border border-slate-700 bg-slate-950/55 px-4 py-3">
                                        <div className="text-xs font-semibold tracking-wide text-slate-400 uppercase">
                                            Username
                                        </div>
                                        <div className="mt-1 text-slate-100">
                                            {user.username}
                                        </div>
                                    </div>
                                    <div className="rounded-lg border border-slate-700 bg-slate-950/55 px-4 py-3">
                                        <div className="text-xs font-semibold tracking-wide text-slate-400 uppercase">
                                            Email
                                        </div>
                                        <div className="mt-1 break-all text-slate-100">
                                            {user.email}
                                        </div>
                                    </div>
                                    <div className="rounded-lg border border-slate-700 bg-slate-950/55 px-4 py-3">
                                        <div className="text-xs font-semibold tracking-wide text-slate-400 uppercase">
                                            User ID
                                        </div>
                                        <div className="mt-1 break-all text-sm text-slate-200">
                                            {user.puid}
                                        </div>
                                    </div>
                                    <div className="rounded-lg border border-slate-700 bg-slate-950/55 px-4 py-3">
                                        <div className="text-xs font-semibold tracking-wide text-slate-400 uppercase">
                                            Projects
                                        </div>
                                        <div className="mt-1 text-slate-100">
                                            {user.projectCount}
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="rounded-2xl border border-slate-700/70 bg-slate-900/75 p-6 backdrop-blur-sm">
                                <h2 className="text-xl font-semibold text-white">
                                    Account Status
                                </h2>
                                <div className="mt-4 flex flex-col gap-3 text-sm">
                                    <div className="rounded-lg border border-slate-700 bg-slate-950/55 px-4 py-3">
                                        <div className="text-xs font-semibold tracking-wide text-slate-400 uppercase">
                                            Verification
                                        </div>
                                        <div
                                            className={`mt-1 font-medium ${
                                                user.isVerified
                                                    ? "text-emerald-300"
                                                    : "text-amber-300"
                                            }`}
                                        >
                                            {user.isVerified
                                                ? "Verified"
                                                : "Unverified"}
                                        </div>
                                    </div>
                                    <div className="rounded-lg border border-slate-700 bg-slate-950/55 px-4 py-3">
                                        <div className="text-xs font-semibold tracking-wide text-slate-400 uppercase">
                                            Member Since
                                        </div>
                                        <div className="mt-1 text-slate-200">
                                            {createdAt}
                                        </div>
                                    </div>
                                    <div className="rounded-lg border border-slate-700 bg-slate-950/55 px-4 py-3">
                                        <div className="text-xs font-semibold tracking-wide text-slate-400 uppercase">
                                            Last Updated
                                        </div>
                                        <div className="mt-1 text-slate-200">
                                            {updatedAt}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </section>

                        {!user.isVerified && (
                            <section className="rounded-2xl border border-amber-700/50 bg-amber-950/25 p-6">
                                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                                    <div>
                                        <h2 className="text-xl font-semibold text-amber-200">
                                            Verify Your Account
                                        </h2>
                                        <p className="mt-2 max-w-2xl text-amber-100/90">
                                            Your account is currently
                                            unverified. Verify your email to
                                            fully secure access to your account.
                                        </p>
                                    </div>
                                    <Link
                                        to="/verify"
                                        className="inline-flex items-center gap-2 rounded-md border border-amber-400/50 bg-amber-500/20 px-4 py-2 font-semibold text-amber-100 no-underline transition-colors hover:bg-amber-500/30"
                                    >
                                        <IconShieldCheck size={18} />
                                        <span>Go to Verification</span>
                                    </Link>
                                </div>
                            </section>
                        )}

                        <section className="rounded-2xl border border-red-700/50 bg-red-950/30 p-6">
                            <div className="flex items-center gap-3">
                                <span className="rounded-md border border-red-500/50 bg-red-500/20 p-2 text-red-200">
                                    <IconAlertTriangle size={18} />
                                </span>
                                <h2 className="text-xl font-semibold text-red-100">
                                    Dangerous Action
                                </h2>
                            </div>

                            <p className="mt-4 max-w-3xl text-sm text-red-100/90">
                                Deleting your account is permanent and cannot be
                                undone. All projects and related data owned by
                                this account will be removed.
                            </p>

                            <div className="mt-5">
                                <button
                                    type="button"
                                    onClick={() => setDeletePopupOpen(true)}
                                    className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-red-700 px-4 py-3 font-semibold text-white transition-colors hover:bg-red-600 cursor-pointer"
                                >
                                    <IconTrash size={18} />
                                    <span>Delete Account Permanently</span>
                                </button>
                            </div>
                        </section>

                        <Popup
                            open={deletePopupOpen}
                            onOpenChange={(next) => {
                                if (deleteUserMutation.isPending) return;
                                setDeletePopupOpen(next);
                            }}
                            title="Delete account"
                            description="This action cannot be undone. Enter your password to continue."
                            initialFocusRef={deletePasswordRef}
                            footer={
                                <div className="flex items-center justify-end gap-2">
                                    <button
                                        type="button"
                                        className="rounded-lg border border-slate-700 bg-slate-900/60 px-4 py-2 text-sm font-medium text-slate-200 transition-colors cursor-pointer hover:border-purple-500/60 hover:bg-slate-800/60 focus:outline-none focus:ring-2 focus:ring-purple-500/40 disabled:opacity-50"
                                        onClick={() =>
                                            setDeletePopupOpen(false)
                                        }
                                        disabled={deleteUserMutation.isPending}
                                    >
                                        Cancel
                                    </button>
                                    <button
                                        type="submit"
                                        form="delete-account-form"
                                        className="rounded-lg bg-red-600/30 px-4 py-2 text-sm font-medium text-red-100 transition-colors cursor-pointer hover:bg-red-600/40 focus:outline-none focus:ring-2 focus:ring-red-500/40 disabled:opacity-50"
                                        disabled={
                                            deleteUserMutation.isPending ||
                                            !canDelete
                                        }
                                    >
                                        {deleteUserMutation.isPending
                                            ? "Deleting..."
                                            : "Delete Account"}
                                    </button>
                                </div>
                            }
                        >
                            <form
                                id="delete-account-form"
                                className="flex flex-col gap-4"
                                onSubmit={handleDeleteAccount}
                            >
                                {deleteError && (
                                    <div className="rounded-lg border border-red-900/50 bg-red-950/30 px-3 py-2 text-sm text-red-200">
                                        {deleteError}
                                    </div>
                                )}
                                <div className="flex flex-col gap-2">
                                    <label
                                        htmlFor="delete-password"
                                        className="text-sm font-medium text-slate-200"
                                    >
                                        Password
                                    </label>
                                    <input
                                        ref={deletePasswordRef}
                                        id="delete-password"
                                        type="password"
                                        value={deletePassword}
                                        onChange={(event) =>
                                            setDeletePassword(
                                                event.target.value,
                                            )
                                        }
                                        autoComplete="current-password"
                                        className="rounded-md border border-slate-700 bg-slate-900/70 px-4 py-3 text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500"
                                        placeholder="Enter your password"
                                        disabled={deleteUserMutation.isPending}
                                    />
                                </div>
                            </form>
                        </Popup>
                    </>
                )}
            </main>
        </div>
    );
}
