import {
  createContext, useCallback, useContext, useEffect, useMemo, useState,
  type ReactNode,
} from 'react';
import type { LoginInput, User } from '../types';
import { api, clearAccessToken, saveAccessToken, TOKEN_KEY } from '../services/api';

interface Ctx {
  current: User | null;
  loading: boolean;
  error: string | null;
  login: (input: LoginInput) => Promise<User>;
  logout: () => void;
  refreshMe: () => Promise<void>;
}

const UserCtx = createContext<Ctx>({
  current: null,
  loading: true,
  error: null,
  login: async () => { throw new Error('UserProvider chưa sẵn sàng'); },
  logout: () => {},
  refreshMe: async () => {},
});

export function UserProvider({ children }: { children: ReactNode }) {
  const [current, setCurrent] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refreshMe = useCallback(async () => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) {
      setCurrent(null);
      setLoading(false);
      return;
    }

    try {
      const user = await api.me();
      setCurrent(user);
      setError(null);
    } catch {
      clearAccessToken();
      setCurrent(null);
      setError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refreshMe();
  }, [refreshMe]);

  const login = useCallback(async (input: LoginInput) => {
    const result = await api.login(input);
    saveAccessToken(result.accessToken);
    setCurrent(result.user);
    setError(null);
    return result.user;
  }, []);

  const logout = useCallback(() => {
    clearAccessToken();
    setCurrent(null);
    setError(null);
  }, []);

  const value = useMemo(
    () => ({ current, loading, error, login, logout, refreshMe }),
    [current, loading, error, login, logout, refreshMe],
  );

  return <UserCtx.Provider value={value}>{children}</UserCtx.Provider>;
}

export const useCurrentUser = () => useContext(UserCtx);
