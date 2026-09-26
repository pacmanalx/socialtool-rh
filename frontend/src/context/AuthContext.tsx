import React, { createContext, useContext, useEffect, useState } from 'react';
import { api, setAuthToken } from '../services/api';
import type { Tenant, User } from '../types';

interface AuthContextType {
  user: User | null;
  tenant: Tenant | null;
  demoUsers: any[];
  isLoading: boolean;
  switchUser: (email: string) => Promise<void>;
  refreshUser: () => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [demoUsers, setDemoUsers] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const initAuth = async () => {
    try {
      setIsLoading(true);
      // Carrega lista de usuários de demonstração
      const demos = await api.auth.getDemoUsers().catch(() => []);
      setDemoUsers(demos);

      // Tenta login com o usuário administrador demo se não houver usuário logado
      const token = localStorage.getItem('socialtool_token');
      if (token) {
        try {
          const profile = await api.auth.getMe();
          setUser(profile);
          setTenant({
            id: profile.tenantId,
            name: 'Demo Company',
            subdomain: 'demo',
            currencyName: 'SocialCoins',
            monthlyCoinsQuota: 100,
          });
        } catch {
          // Token expirou ou inválido — refaz login automático como administrador demo
          await performLogin('alexandre.pereira@example.com');
        }
      } else {
        await performLogin('alexandre.pereira@example.com');
      }
    } catch (err) {
      console.error('Erro na inicialização da autenticação:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const performLogin = async (email: string) => {
    const data = await api.auth.login(email, '123456');
    setUser(data.user);
    setTenant(data.tenant);
  };

  const switchUser = async (email: string) => {
    setIsLoading(true);
    try {
      await performLogin(email);
    } finally {
      setIsLoading(false);
    }
  };

  const refreshUser = async () => {
    try {
      const updated = await api.auth.getMe();
      setUser(updated);
    } catch (err) {
      console.error('Erro ao atualizar usuário:', err);
    }
  };

  const logout = () => {
    setAuthToken(null);
    setUser(null);
    setTenant(null);
  };

  useEffect(() => {
    initAuth();
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        tenant,
        demoUsers,
        isLoading,
        switchUser,
        refreshUser,
        logout,
      }}
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
