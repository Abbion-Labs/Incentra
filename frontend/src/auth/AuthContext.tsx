import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import type { ReactNode } from 'react';
import {
  api,
  clearToken,
  restoreSession,
  revokeRefreshToken,
  setAccessToken,
} from '../api/client';
import { setSessionExpiredHandler } from './session';
import { openSessionChannel } from './sessionChannel';
import type { SessionChannel } from './sessionChannel';
import type { AuthResponse, UserProfile } from '../api/types';

interface AuthContextValue {
  user: UserProfile | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (...roles: string[]) => boolean;
  updateEmployeeProfile: (
    patch: Partial<
      Pick<
        UserProfile,
        | 'employeeAvatarUrl'
        | 'employeeFullName'
        | 'employeeFirstName'
        | 'employeeLastName'
        | 'emailNotificationsEnabled'
      >
    >,
  ) => void;
  updateNotificationPreferences: (enabled: boolean) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  const updateEmployeeProfile = useCallback(
    (
      patch: Partial<
        Pick<
          UserProfile,
          | 'employeeAvatarUrl'
          | 'employeeFullName'
          | 'employeeFirstName'
          | 'employeeLastName'
          | 'emailNotificationsEnabled'
        >
      >,
    ) => {
      setUser((prev) => (prev ? { ...prev, ...patch } : prev));
    },
    [],
  );

  const updateNotificationPreferences = useCallback(
    async (enabled: boolean) => {
      const profile = await api.put<UserProfile>(
        '/api/auth/notification-preferences',
        {
          emailNotificationsEnabled: enabled,
        },
      );
      setUser((prev) => (prev ? { ...prev, ...profile } : profile));
    },
    [],
  );

  useEffect(() => {
    setSessionExpiredHandler(() => {
      setUser(null);
    });
    return () => setSessionExpiredHandler(null);
  }, []);

  useEffect(() => {
    // The refresh cookie is the only thing that survives a reload, so the session is restored from it.
    // The response already carries the profile, which saves a separate /api/auth/me call.
    void restoreSession()
      .then((profile) => setUser(profile))
      .finally(() => setLoading(false));
  }, []);

  const sessionChannel = useRef<SessionChannel | null>(null);

  useEffect(() => {
    const channel = openSessionChannel((event) => {
      if (event === 'signed-out') {
        clearToken();
        setUser(null);
        return;
      }

      // Another tab signed in, possibly as someone else, and the cookie now belongs to that session.
      void restoreSession().then((profile) => setUser(profile));
    });
    sessionChannel.current = channel;

    return () => {
      channel.close();
      sessionChannel.current = null;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await api.post<AuthResponse>('/api/auth/login', {
      email,
      password,
    });
    setAccessToken(response.accessToken);
    setUser(response.user);
    sessionChannel.current?.announce('signed-in');
  }, []);

  const logout = useCallback(() => {
    revokeRefreshToken();
    clearToken();
    setUser(null);
    sessionChannel.current?.announce('signed-out');
  }, []);

  const hasRole = useCallback(
    (...roles: string[]) => roles.some((r) => user?.roles.includes(r) ?? false),
    [user],
  );

  const value = useMemo(
    () => ({
      user,
      loading,
      login,
      logout,
      hasRole,
      updateEmployeeProfile,
      updateNotificationPreferences,
    }),
    [
      user,
      loading,
      login,
      logout,
      hasRole,
      updateEmployeeProfile,
      updateNotificationPreferences,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
