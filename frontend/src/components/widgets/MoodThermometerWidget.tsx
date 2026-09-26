import React, { useState, useEffect } from 'react';
import { api } from '../../services/api';
import { Smile, CheckCircle, HeartHandshake } from 'lucide-react';

const MOODS = [
  { score: 1, emoji: '😭', label: 'Muito Mal' },
  { score: 2, emoji: '😕', label: 'Para Baixo' },
  { score: 3, emoji: '😐', label: 'Neutro' },
  { score: 4, emoji: '😊', label: 'Bem' },
  { score: 5, emoji: '🤩', label: 'Excelente' },
];

export const MoodThermometerWidget: React.FC = () => {
  const [selectedScore, setSelectedScore] = useState<number | null>(null);
  const [note, setNote] = useState('');
  const [hasSubmittedToday, setHasSubmittedToday] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    api.users.getTodayMood().then((mood) => {
      if (mood) {
        setSelectedScore(mood.score);
        setNote(mood.note || '');
        setHasSubmittedToday(true);
      }
    }).catch(console.error);
  }, []);

  const handleSubmit = async (score: number) => {
    setSelectedScore(score);
    setIsSubmitting(true);
    try {
      await api.users.submitMood(score, note.trim() || undefined);
      setHasSubmittedToday(true);
    } catch (err) {
      console.error('Erro ao enviar humor:', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-xs mb-4">
      <div className="flex items-center space-x-2 text-slate-800 font-bold text-sm mb-1">
        <Smile className="w-4 h-4 text-amber-500" />
        <span>Termômetro de Humor</span>
      </div>
      <p className="text-xs text-slate-500 mb-3">
        Como você está se sentindo no trabalho hoje?
      </p>

      {hasSubmittedToday ? (
        <div className="bg-emerald-50 border border-emerald-200 rounded-xl p-3 text-center">
          <div className="flex items-center justify-center space-x-2 text-emerald-800 font-semibold text-xs mb-1">
            <CheckCircle className="w-4 h-4 text-emerald-600" />
            <span>Humor registrado hoje!</span>
          </div>
          <div className="text-2xl my-1">
            {MOODS.find((m) => m.score === selectedScore)?.emoji}
          </div>
          <p className="text-[11px] text-emerald-700">
            Obrigado pelo check-in! Seus dados ajudam o RH a cuidar da saúde da equipe.
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          <div className="grid grid-cols-5 gap-1.5 text-center">
            {MOODS.map((m) => (
              <button
                key={m.score}
                disabled={isSubmitting}
                onClick={() => handleSubmit(m.score)}
                className={`py-2 px-1 rounded-xl transition-all cursor-pointer flex flex-col items-center hover:scale-110 ${
                  selectedScore === m.score
                    ? 'bg-amber-100 ring-2 ring-amber-400'
                    : 'hover:bg-slate-50'
                }`}
              >
                <span className="text-2xl mb-1">{m.emoji}</span>
                <span className="text-[9px] text-slate-500 font-medium leading-tight">
                  {m.label}
                </span>
              </button>
            ))}
          </div>

          <div className="flex items-center space-x-1.5 text-[10px] text-slate-400 justify-center">
            <HeartHandshake className="w-3 h-3 text-indigo-400" />
            <span>Registro seguro e anônimo para relatórios de equipe</span>
          </div>
        </div>
      )}
    </div>
  );
};
