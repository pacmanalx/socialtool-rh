import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { api, refreshSession, setAccessToken, setSessionHandlers } from '../services/api';
import type { Organization, Session, User } from '../types';

type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

interface AuthContextType {
  status: AuthStatus;
  user: User | null;
  organization: Organization | null;
  googleClientId: string | null;
  passwordResetAvailable: boolean;
  applySession: (session: Session) => void;
  login: (email: string, password: string) => Promise<void>;
  loginWithGoogle: (credential: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<User | null>(null);
  const [organization, setOrganization] = useState<Organization | null>(null);
  const [googleClientId, setGoogleClientId] = useState<string | null>(null);
  const [passwordResetAvailable, setPasswordResetAvailable] = useState(false);

  const applySession = useCallback((session: Session) => {
    setAccessToken(session.accessToken);
    setUser(session.user);
    setOrganization(session.organization);
    setStatus('authenticated');
  }, []);

  const clearSession = useCallback(() => {
    setAccessToken(null);
    setUser(null);
    setOrganization(null);
    setStatus('anonymous');
  }, []);

  useEffect(() => {
    setSessionHandlers({
      onRefreshed: (session) => {
        setUser(session.user);
        setOrganization(session.organization);
      },
      onExpired: clearSession,
    });

    // A sessão sobrevive ao recarregar a página pelo cookie de refresh; sem cookie válido, cai no login.
    Promise.all([api.auth.getConfig().catch(() => null), refreshSession()]).then(([config, session]) => {
      setGoogleClientId(config?.googleClientId ?? null);
      setPasswordResetAvailable(config?.passwordResetAvailable ?? false);
      if (session) {
        applySession(session);
      } else {
        clearSession();
      }
    });
  }, [applySession, clearSession]);

  const login = useCallback(
    async (email: string, password: string) => applySession(await api.auth.login(email, password)),
    [applySession],
  );

  const loginWithGoogle = useCallback(
    async (credential: string) => applySession(await api.auth.loginWithGoogle(credential)),
    [applySession],
  );

  const logout = useCallback(async () => {
    try {
      await api.auth.logout();
    } finally {
      clearSession();
    }
  }, [clearSession]);

  const refreshUser = useCallback(async () => {
    try {
      setUser(await api.auth.getMe());
    } catch (err) {
      console.error('Erro ao atualizar usuário:', err);
    }
  }, []);

  return (
    <AuthContext.Provider
      value={{ status, user, organization, googleClientId, passwordResetAvailable, applySession, login, loginWithGoogle, logout, refreshUser }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth deve ser usado dentro de um AuthProvider');
  }
  return context;
};
