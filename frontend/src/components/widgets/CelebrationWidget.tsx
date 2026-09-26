import React from 'react';
import { Cake, Calendar, Sparkles } from 'lucide-react';

export const CelebrationWidget: React.FC = () => {
  const celebrations = [
    {
      id: '1',
      name: 'Juliana Santos',
      avatarUrl: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100',
      type: 'work_anniversary',
      title: '3 anos de empresa!',
      date: 'Hoje',
    },
    {
      id: '2',
      name: 'Alexandre Pereira',
      avatarUrl: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100',
      type: 'birthday',
      title: 'Aniversário chegando',
      date: '14 de Out',
    },
  ];

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-xs">
      <div className="flex items-center space-x-2 text-slate-800 font-bold text-sm mb-3">
        <Sparkles className="w-4 h-4 text-rose-500" />
        <span>Celebrações do Mês</span>
      </div>

      <div className="space-y-3">
        {celebrations.map((c) => (
          <div key={c.id} className="flex items-center space-x-3 p-2 rounded-xl bg-slate-50 border border-slate-100">
            <img
              src={c.avatarUrl}
              alt={c.name}
              className="w-8 h-8 rounded-full object-cover shrink-0 ring-2 ring-rose-200"
            />
            <div className="flex-1 min-w-0">
              <p className="text-xs font-bold text-slate-800 truncate">{c.name}</p>
              <div className="flex items-center space-x-1 text-[11px] text-rose-600 font-medium">
                {c.type === 'work_anniversary' ? (
                  <Calendar className="w-3 h-3 text-rose-500" />
                ) : (
                  <Cake className="w-3 h-3 text-rose-500" />
                )}
                <span>{c.title}</span>
              </div>
            </div>
            <span className="text-[10px] text-slate-400 font-medium px-2 py-0.5 bg-white rounded-full border border-slate-200">
              {c.date}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
};
