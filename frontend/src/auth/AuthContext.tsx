import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { api, clearToken, ensureValidSession, revokeRefreshToken, setAuthTokens } from '../api/client';
import { setSessionExpiredHandler } from './session';
import type { AuthResponse, UserProfile } from '../api/types';

interface AuthContextValue {
  user: UserProfile | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (...roles: string[]) => boolean;
  refreshUser: () => Promise<void>;
  updateEmployeeProfile: (patch: Partial<Pick<UserProfile,
    'employeeAvatarUrl' | 'employeeFullName' | 'employeeFirstName' | 'employeeLastName' | 'emailNotificationsEnabled'>>) => void;
  updateNotificationPreferences: (enabled: boolean) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshUser = useCallback(async () => {
    try {
      const profile = await api.get<UserProfile>('/api/auth/me');
      setUser((prev) => (profile ? { ...(prev ?? {}), ...profile } : null));
    } catch {
      setUser(null);
      clearToken();
    }
  }, []);

  const updateEmployeeProfile = useCallback(
    (patch: Partial<Pick<UserProfile,
      'employeeAvatarUrl' | 'employeeFullName' | 'employeeFirstName' | 'employeeLastName' | 'emailNotificationsEnabled'>>) => {
      setUser((prev) => (prev ? { ...prev, ...patch } : prev));
    },
    [],
  );

  const updateNotificationPreferences = useCallback(async (enabled: boolean) => {
    const profile = await api.put<UserProfile>('/api/auth/notification-preferences', {
      emailNotificationsEnabled: enabled,
    });
    setUser((prev) => (prev ? { ...prev, ...profile } : profile));
  }, []);

  useEffect(() => {
    setSessionExpiredHandler(() => {
      setUser(null);
    });
    return () => setSessionExpiredHandler(null);
  }, []);

  useEffect(() => {
    void ensureValidSession()
      .then(() => refreshUser())
      .finally(() => setLoading(false));
  }, [refreshUser]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await api.post<AuthResponse>('/api/auth/login', { email, password });
    setAuthTokens(response.accessToken, response.refreshToken);
    setUser(response.user);
  }, []);

  const logout = useCallback(() => {
    revokeRefreshToken();
    clearToken();
    setUser(null);
  }, []);

  const hasRole = useCallback(
    (...roles: string[]) => roles.some((r) => user?.roles.includes(r) ?? false),
    [user],
  );

  const value = useMemo(
    () => ({ user, loading, login, logout, hasRole, refreshUser, updateEmployeeProfile, updateNotificationPreferences }),
    [user, loading, login, logout, hasRole, refreshUser, updateEmployeeProfile, updateNotificationPreferences],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
