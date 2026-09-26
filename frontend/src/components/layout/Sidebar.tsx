import React from 'react';
import {
  MessageSquare,
  Award,
  Users2,
  Smile,
  Target,
  BarChart3,
  CalendarCheck,
  Building2,
  Sparkles
} from 'lucide-react';

interface SidebarProps {
  currentTab: string;
  setCurrentTab: (tab: string) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ currentTab, setCurrentTab }) => {
  const menuItems = [
    { id: 'feed', label: 'Mural Social', icon: MessageSquare, badge: 'Live' },
    { id: 'recognitions', label: 'Reconhecimentos & Moedas', icon: Award },
    { id: 'mood', label: 'Termômetro de Humor', icon: Smile },
    { id: 'one-on-one', label: 'Reuniões 1-on-1', icon: CalendarCheck },
    { id: 'feedback', label: 'Feedback Contínuo', icon: Users2 },
    { id: 'okrs', label: 'OKRs & Metas', icon: Target },
    { id: 'performance', label: 'Avaliação 360° & 9-Box', icon: BarChart3 },
    { id: 'org', label: 'Organograma & Equipes', icon: Building2 },
  ];

  return (
    <aside className="w-64 bg-white border-r border-slate-200 hidden md:block shrink-0 min-h-[calc(100vh-4rem)] p-4">
      <div className="space-y-1">
        <p className="px-3 text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">
          Comunicação & Cultura
        </p>
        {menuItems.slice(0, 3).map((item) => {
          const Icon = item.icon;
          const isActive = currentTab === item.id;
          return (
            <button
              key={item.id}
              onClick={() => setCurrentTab(item.id)}
              className={`w-full flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 cursor-pointer ${
                isActive
                  ? 'bg-indigo-50 text-indigo-700 shadow-xs'
                  : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
              }`}
            >
              <div className="flex items-center space-x-3">
                <Icon className={`w-4 h-4 ${isActive ? 'text-indigo-600' : 'text-slate-400'}`} />
                <span>{item.label}</span>
              </div>
              {item.badge && (
                <span className="text-[10px] bg-rose-100 text-rose-600 font-bold px-2 py-0.5 rounded-full animate-pulse">
                  {item.badge}
                </span>
              )}
            </button>
          );
        })}

        <div className="pt-4">
          <p className="px-3 text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">
            Gestão & Performance
          </p>
          {menuItems.slice(3).map((item) => {
            const Icon = item.icon;
            const isActive = currentTab === item.id;
            return (
              <button
                key={item.id}
                onClick={() => setCurrentTab(item.id)}
                className={`w-full flex items-center justify-between px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 cursor-pointer ${
                  isActive
                    ? 'bg-indigo-50 text-indigo-700 shadow-xs'
                    : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                }`}
              >
                <div className="flex items-center space-x-3">
                  <Icon className={`w-4 h-4 ${isActive ? 'text-indigo-600' : 'text-slate-400'}`} />
                  <span>{item.label}</span>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* Mini banner de cultura */}
      <div className="mt-8 p-3.5 bg-gradient-to-br from-indigo-50 to-purple-50 rounded-2xl border border-indigo-100">
        <div className="flex items-center space-x-2 text-indigo-800 font-semibold text-xs mb-1">
          <Sparkles className="w-4 h-4 text-indigo-600" />
          <span>Nossa cultura</span>
        </div>
        <p className="text-[11px] text-slate-600 leading-relaxed">
          Reconhecer um colega reforça nossos valores e fortalece nossa conexão todos os dias!
        </p>
      </div>
    </aside>
  );
};
