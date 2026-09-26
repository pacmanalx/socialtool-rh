import React, { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { Coins, Gift, ChevronDown, UserCheck, Sparkles, Bell } from 'lucide-react';

interface NavbarProps {
  onOpenRecognitionModal: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onOpenRecognitionModal }) => {
  const { user, tenant, demoUsers, switchUser, isLoading } = useAuth();
  const [showUserMenu, setShowUserMenu] = useState(false);

  return (
    <header className="sticky top-0 z-30 bg-white border-b border-slate-200 shadow-xs">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo & Tenant */}
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-amber-500 via-rose-500 to-indigo-600 flex items-center justify-center text-white font-bold text-xl shadow-md">
              S
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-bold text-slate-800 text-lg tracking-tight">SocialTool</span>
                <span className="text-xs bg-indigo-100 text-indigo-700 font-semibold px-2 py-0.5 rounded-full">RH</span>
              </div>
              <p className="text-xs text-slate-500 font-medium">{tenant?.name || ''}</p>
            </div>
          </div>

          {/* Center / Action: Quick Praise Button */}
          <div className="hidden md:flex items-center space-x-4">
            <button
              onClick={onOpenRecognitionModal}
              className="flex items-center space-x-2 px-4 py-2 bg-gradient-to-r from-amber-500 to-rose-500 hover:from-amber-600 hover:to-rose-600 text-white font-medium rounded-full shadow-sm hover:shadow transition-all duration-200 cursor-pointer text-sm"
            >
              <Sparkles className="w-4 h-4" />
              <span>Reconhecer Colega</span>
            </button>
          </div>

          {/* Right Side: Coins Wallet & User Switcher */}
          <div className="flex items-center space-x-4">
            {/* SocialCoins Wallet Pills */}
            {user && (
              <div className="flex items-center space-x-2 bg-slate-50 p-1.5 rounded-xl border border-slate-200">
                <div className="flex items-center space-x-1.5 px-2.5 py-1 bg-amber-50 rounded-lg text-amber-800 border border-amber-200 text-xs font-semibold" title="Moedas que você pode doar neste mês">
                  <Gift className="w-3.5 h-3.5 text-amber-600" />
                  <span>{user.coinsAvailableToGive}</span>
                  <span className="hidden lg:inline text-amber-700 font-normal">p/ doar</span>
                </div>

                <div className="flex items-center space-x-1.5 px-2.5 py-1 bg-emerald-50 rounded-lg text-emerald-800 border border-emerald-200 text-xs font-semibold" title="Saldo de moedas recebidas para resgatar prêmios">
                  <Coins className="w-3.5 h-3.5 text-emerald-600" />
                  <span>{user.coinsBalanceToSpend}</span>
                  <span className="hidden lg:inline text-emerald-700 font-normal">recebidas</span>
                </div>
              </div>
            )}

            {/* Notification Bell */}
            <button className="p-2 text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-100 relative cursor-pointer">
              <Bell className="w-5 h-5" />
              <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-rose-500 rounded-full"></span>
            </button>

            {/* User Dropdown / Switcher */}
            <div className="relative">
              <button
                onClick={() => setShowUserMenu(!showUserMenu)}
                className="flex items-center space-x-2 p-1.5 rounded-xl hover:bg-slate-100 transition-colors cursor-pointer border border-transparent hover:border-slate-200"
              >
                <img
                  src={user?.avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100'}
                  alt={user?.name}
                  className="w-9 h-9 rounded-full object-cover ring-2 ring-indigo-500/20"
                />
                <div className="hidden sm:block text-left text-xs">
                  <div className="font-semibold text-slate-800 flex items-center space-x-1">
                    <span>{user?.name || 'Carregando...'}</span>
                    <span className="text-[10px] px-1.5 py-0.2 bg-slate-200 text-slate-700 rounded font-normal">
                      {user?.role}
                    </span>
                  </div>
                  <div className="text-slate-500 truncate max-w-[130px]">{user?.jobTitle}</div>
                </div>
                <ChevronDown className="w-4 h-4 text-slate-400" />
              </button>

              {/* Quick Profile & Demo User Switcher Dropdown */}
              {showUserMenu && (
                <div className="absolute right-0 mt-2 w-72 bg-white rounded-2xl shadow-xl border border-slate-200 py-2 z-50">
                  <div className="px-4 py-2 border-b border-slate-100">
                    <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Conectado como</p>
                    <p className="text-sm font-bold text-slate-800">{user?.name}</p>
                    <p className="text-xs text-slate-500">{user?.email}</p>
                    <p className="text-xs text-indigo-600 font-medium mt-0.5">{user?.departmentName || ''}</p>
                  </div>

                  <div className="px-3 py-2">
                    <div className="flex items-center justify-between text-xs font-semibold text-slate-500 mb-2 px-1">
                      <span>Trocar de Colaborador (Demo)</span>
                      <UserCheck className="w-3.5 h-3.5 text-indigo-500" />
                    </div>
                    <div className="space-y-1">
                      {demoUsers.map((u) => {
                        const isCurrent = u.id === user?.id;
                        return (
                          <button
                            key={u.id}
                            disabled={isLoading || isCurrent}
                            onClick={() => {
                              switchUser(u.email);
                              setShowUserMenu(false);
                            }}
                            className={`w-full flex items-center space-x-2.5 px-2.5 py-1.5 rounded-lg text-left text-xs transition-colors cursor-pointer ${
                              isCurrent
                                ? 'bg-indigo-50 text-indigo-900 font-semibold'
                                : 'hover:bg-slate-50 text-slate-700'
                            }`}
                          >
                            <img
                              src={u.avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=50'}
                              alt={u.name}
                              className="w-7 h-7 rounded-full object-cover"
                            />
                            <div className="flex-1 truncate">
                              <div className="flex items-center justify-between">
                                <span className="truncate">{u.name}</span>
                                <span className="text-[10px] text-slate-400 font-normal">({u.role})</span>
                              </div>
                              <div className="text-[10px] text-slate-400 truncate">{u.jobTitle}</div>
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </header>
  );
};
