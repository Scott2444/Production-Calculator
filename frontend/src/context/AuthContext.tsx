"use client";

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

interface AuthContextType {
  loggedIn: boolean;
  userId?: string;
  setLoggedIn: (value: boolean) => void;
  setUserId: (id: string | undefined) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [loggedIn, setLoggedIn] = useState(false);
  const [userId, setUserId] = useState<string | undefined>(undefined);
  useEffect(() => {
  const checkAuth = async () => {
    // Check if the user is logged in
    const hasToken = document.cookie.split(';').some(cookie => cookie.trim().startsWith('user_id='));
    if (hasToken) {
      setLoggedIn(hasToken);
      // Grab new access token
      const response = await fetch("/api/auth/refresh", { method: "POST" });
      if (response.ok) {
        const data = await response.json();
        setUserId(data.puid);
      }
    }
  };
  checkAuth();
}, []);

  return (
    <AuthContext.Provider value={{ loggedIn, setLoggedIn, userId, setUserId }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
