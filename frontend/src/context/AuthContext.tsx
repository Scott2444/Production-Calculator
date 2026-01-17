"use client";

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

interface AuthContextType {
  loggedIn: boolean;
  userId?: string;
  setLoggedIn: (value: boolean) => void;
  setUserId: (id: string) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [loggedIn, setLoggedIn] = useState(false);
  const [userId, setUserId] = useState<string | undefined>(undefined);
  useEffect(() => {
    const hasToken = document.cookie.split(';').some(cookie => cookie.trim().startsWith('refresh_token='));
    setLoggedIn(hasToken);
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
