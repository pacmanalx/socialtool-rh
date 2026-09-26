import React, { useEffect, useState } from 'react';
import { api } from '../../services/api';
import type { LeaderboardItem } from '../../types';
import { Trophy, Coins } from 'lucide-react';

export const CoinsLeaderboardWidget: React.FC = () => {
  const [leaders, setLeaders] = useState<LeaderboardItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.recognition.getLeaderboard()
      .then(setLeaders)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-xs mb-4">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center space-x-2 text-slate-800 font-bold text-sm">
          <Trophy className="w-4 h-4 text-amber-500" />
          <span>Ranking de Reconhecimento</span>
        </div>
        <span className="text-[10px] text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full font-semibold">
          Mês Atual
        </span>
      </div>

      {loading ? (
        <div className="text-center py-4 text-xs text-slate-400">Carregando ranking...</div>
      ) : leaders.length === 0 ? (
        <div className="text-center py-4 text-xs text-slate-400">Nenhum reconhecimento ainda.</div>
      ) : (
        <div className="space-y-2.5">
          {leaders.slice(0, 5).map((item, index) => {
            const isTop3 = index < 3;
            const medals = ['🥇', '🥈', '🥉'];
            return (
              <div
                key={item.userId}
                className="flex items-center justify-between text-xs p-1.5 rounded-xl hover:bg-slate-50 transition-colors"
              >
                <div className="flex items-center space-x-2.5 min-w-0">
                  <span className="w-5 text-center font-bold text-slate-400 text-xs shrink-0">
                    {isTop3 ? medals[index] : `${index + 1}º`}
                  </span>
                  <img
                    src={item.avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=50'}
                    alt={item.name}
                    className="w-7 h-7 rounded-full object-cover shrink-0"
                  />
                  <div className="min-w-0">
                    <p className="font-semibold text-slate-800 truncate">{item.name}</p>
                    <p className="text-[10px] text-slate-400 truncate">{item.jobTitle}</p>
                  </div>
                </div>

                <div className="flex items-center space-x-1 text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-lg font-bold text-xs shrink-0">
                  <Coins className="w-3 h-3 text-emerald-600" />
                  <span>{item.coinsReceived}</span>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
