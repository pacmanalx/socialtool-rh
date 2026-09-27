import React, { useCallback, useEffect, useState } from 'react';
import { BarChart3, ClipboardList, Plus, RefreshCw, SlidersHorizontal } from 'lucide-react';
import { api } from '../../services/api';
import type { SurveySummary } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage } from '../auth/authForm';
import { SurveyEditor } from './SurveyEditor';
import { SurveyResultsView } from './SurveyResultsView';
import { ScalesManager } from './ScalesManager';
import { STATUS_STYLES, formatDateTime } from './surveyUtils';

type View = { kind: 'list' } | { kind: 'edit'; id?: string } | { kind: 'results'; id: string } | { kind: 'scales' };

// Criar, programar, acompanhar e encerrar enquetes (permissão "Criar e gerenciar enquetes").
export const SurveysAdmin: React.FC = () => {
  const [view, setView] = useState<View>({ kind: 'list' });
  const [surveys, setSurveys] = useState<SurveySummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setSurveys(await api.surveysAdmin.list());
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (view.kind === 'list') load();
  }, [view, load]);

  const back = () => setView({ kind: 'list' });

  if (view.kind === 'edit') return <SurveyEditor surveyId={view.id} onDone={back} onResults={(id) => setView({ kind: 'results', id })} />;
  if (view.kind === 'results') return <SurveyResultsView surveyId={view.id} onBack={back} />;
  if (view.kind === 'scales') return <ScalesManager onBack={back} />;

  return (
    <div className="space-y-6 max-w-5xl">
      <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <ClipboardList className="w-5 h-5 text-indigo-600" /> Enquetes
          </h2>
          <p className="text-sm text-slate-500">Programe início e fim, escolha o público (empresa inteira ou áreas) e acompanhe o resultado.</p>
        </div>
        <div className="flex gap-2">
          <button onClick={() => setView({ kind: 'scales' })} className="flex items-center gap-2 px-4 py-2 rounded-xl border border-slate-200 text-slate-700 font-semibold text-sm hover:bg-slate-50 cursor-pointer">
            <SlidersHorizontal className="w-4 h-4" /> Escalas de resposta
          </button>
          <button onClick={() => setView({ kind: 'edit' })} className="flex items-center gap-2 px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm cursor-pointer">
            <Plus className="w-4 h-4" /> Nova enquete
          </button>
        </div>
      </div>

      <FormError message={error} />

      <div className="bg-white rounded-2xl border border-slate-200 overflow-x-auto">
        <div className="flex justify-end px-5 py-2 border-b border-slate-100">
          <button onClick={load} disabled={loading} className="flex items-center space-x-1 text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer">
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            <span>Atualizar</span>
          </button>
        </div>
        <table className="w-full text-sm">
          <thead className="text-xs text-slate-500 text-left">
            <tr>
              <th className="pl-5 px-3 py-2 font-semibold">Enquete</th>
              <th className="px-3 py-2 font-semibold">Situação</th>
              <th className="px-3 py-2 font-semibold">Período</th>
              <th className="px-3 py-2 font-semibold">Respostas</th>
              <th className="px-5 py-2 font-semibold text-right">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 text-slate-700">
            {surveys.map((s) => (
              <tr key={s.id}>
                <td className="pl-5 px-3 py-3">
                  <div className="font-semibold">{s.title}</div>
                  <div className="text-xs text-slate-500">
                    {s.audienceLabels.join(', ')} · {s.questionCount} pergunta(s) · {s.isAnonymous ? 'anônima' : 'identificada'}
                  </div>
                </td>
                <td className="px-3 py-3">
                  <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full border ${STATUS_STYLES[s.status].className}`}>{STATUS_STYLES[s.status].label}</span>
                </td>
                <td className="px-3 py-3 text-xs text-slate-500 whitespace-nowrap">
                  {formatDateTime(s.startsAt)}
                  <br />
                  até {formatDateTime(s.endsAt)}
                </td>
                <td className="px-3 py-3 text-xs whitespace-nowrap">
                  {s.responses} de {s.audienceSize}
                  {s.audienceSize > 0 && <span className="text-slate-400"> ({Math.round((s.responses * 100) / s.audienceSize)}%)</span>}
                </td>
                <td className="px-5 py-3">
                  <div className="flex items-center justify-end gap-3 text-xs font-semibold">
                    {s.status !== 'Draft' && (
                      <button onClick={() => setView({ kind: 'results', id: s.id })} className="flex items-center gap-1 text-indigo-600 hover:text-indigo-800 cursor-pointer">
                        <BarChart3 className="w-3.5 h-3.5" /> Resultado
                      </button>
                    )}
                    <button onClick={() => setView({ kind: 'edit', id: s.id })} className="text-slate-600 hover:text-slate-800 cursor-pointer">
                      {s.status === 'Closed' ? 'Ver' : 'Editar'}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {surveys.length === 0 && !loading && (
              <tr>
                <td colSpan={5} className="px-5 py-8 text-center text-sm text-slate-500">Nenhuma enquete ainda.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
