import React, { useCallback, useEffect, useState } from 'react';
import { ArrowLeft, EyeOff, ShieldAlert, Users } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { P } from '../../permissions';
import { api } from '../../services/api';
import type { IndividualResponse, SurveyResults } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage, inputClass } from '../auth/authForm';
import { STATUS_STYLES, formatDateTime, formatValue } from './surveyUtils';

const npsTone = (score: number) => (score >= 50 ? 'text-emerald-600' : score >= 0 ? 'text-amber-600' : 'text-rose-600');

export const SurveyResultsView: React.FC<{ surveyId: string; onBack: () => void }> = ({ surveyId, onBack }) => {
  const { can } = useAuth();
  const [results, setResults] = useState<SurveyResults | null>(null);
  const [areaId, setAreaId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [askReason, setAskReason] = useState(false);
  const [individual, setIndividual] = useState<IndividualResponse[] | null>(null);

  const load = useCallback(async () => {
    try {
      setResults(await api.surveysAdmin.results(surveyId, areaId || undefined));
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    }
  }, [surveyId, areaId]);

  useEffect(() => {
    load();
  }, [load]);

  if (!results) return <FormError message={error} />;

  return (
    <div className="space-y-6 max-w-4xl">
      <button onClick={onBack} className="flex items-center gap-1 text-sm font-semibold text-slate-600 hover:text-slate-800 cursor-pointer">
        <ArrowLeft className="w-4 h-4" /> Enquetes
      </button>

      <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold text-slate-900">{results.title}</h2>
          <p className="text-sm text-slate-500 flex items-center gap-2 mt-1">
            <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full border ${STATUS_STYLES[results.status].className}`}>{STATUS_STYLES[results.status].label}</span>
            {results.isAnonymous ? (
              <span className="flex items-center gap-1 text-xs"><EyeOff className="w-3.5 h-3.5" /> anônima</span>
            ) : (
              <span className="text-xs">identificada</span>
            )}
          </p>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 px-5 py-3 text-right">
          <div className="text-2xl font-bold text-slate-900">{results.participationPercent}%</div>
          <div className="text-xs text-slate-500">{results.participants} de {results.audienceSize} responderam</div>
        </div>
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 p-4 flex flex-col sm:flex-row sm:items-center gap-3">
        <label className="text-xs font-semibold text-slate-700 whitespace-nowrap">Recortar por área</label>
        <select value={areaId} onChange={(e) => setAreaId(e.target.value)} className={`${inputClass} sm:max-w-md`}>
          <option value="">Todas as respostas</option>
          {results.areas.filter((a) => a.areaId).map((a) => (
            <option key={a.areaId} value={a.areaId}>{a.label} ({a.responses})</option>
          ))}
        </select>
        <span className="text-[11px] text-slate-500">Só aparecem áreas com pelo menos {results.minimumGroup} respostas, para ninguém ser identificado.</span>
      </div>

      <FormError message={error} />

      {results.suppressed ? (
        <p className="text-sm text-amber-900 bg-amber-50 border border-amber-200 rounded-xl px-3 py-2">
          {results.areaLabel} tem menos de {results.minimumGroup} respostas. O resultado não é mostrado para proteger quem respondeu.
        </p>
      ) : (
        results.questions.map((q, i) => (
          <section key={q.id} className="bg-white rounded-2xl border border-slate-200 p-5 space-y-3">
            <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-2">
              <h3 className="text-sm font-semibold text-slate-800">{i + 1}. {q.text}</h3>
              <div className="flex items-center gap-4 text-right shrink-0">
                {q.nps && (
                  <div>
                    <div className={`text-xl font-bold ${npsTone(q.nps.score)}`}>{q.nps.score > 0 ? '+' : ''}{formatValue(q.nps.score)}</div>
                    <div className="text-[11px] text-slate-500">eNPS</div>
                  </div>
                )}
                {q.average !== null && (
                  <div>
                    <div className="text-xl font-bold text-slate-900">{formatValue(q.average)}</div>
                    <div className="text-[11px] text-slate-500">média</div>
                  </div>
                )}
                <div>
                  <div className="text-xl font-bold text-slate-400">{q.answered}</div>
                  <div className="text-[11px] text-slate-500">respostas</div>
                </div>
              </div>
            </div>
            <div className="space-y-1.5">
              {q.options.map((o) => (
                <div key={o.label} className="flex items-center gap-3 text-xs">
                  <span className="w-40 truncate text-slate-700" title={o.label}>
                    {o.label}
                    {o.value !== null && o.label !== String(o.value) && <span className="text-slate-400"> ({formatValue(o.value)})</span>}
                  </span>
                  <div className="flex-1 h-2.5 bg-slate-100 rounded-full overflow-hidden">
                    <div className="h-full bg-indigo-500 rounded-full" style={{ width: `${o.percent}%` }} />
                  </div>
                  <span className="w-20 text-right text-slate-500">{o.count} · {formatValue(o.percent)}%</span>
                </div>
              ))}
            </div>
            {q.nps && (
              <p className="text-[11px] text-slate-500">
                Promotores (9–10): {q.nps.promoters} · Neutros (7–8): {q.nps.passives} · Detratores (0–6): {q.nps.detractors}
              </p>
            )}
          </section>
        ))
      )}

      {!results.isAnonymous && can(P.SensitiveViewIndividual) && (
        <section className="bg-white rounded-2xl border border-slate-200 p-5 space-y-3">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-start gap-2">
              <Users className="w-4 h-4 text-slate-500 mt-0.5" />
              <div>
                <h3 className="text-sm font-semibold text-slate-800">Respostas individuais</h3>
                <p className="text-xs text-slate-500">Cada acesso exige um motivo e fica registrado na auditoria.</p>
              </div>
            </div>
            {!individual && (
              <button onClick={() => setAskReason(true)} className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 cursor-pointer whitespace-nowrap">
                Abrir respostas individuais
              </button>
            )}
          </div>
          {individual && (
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead className="text-slate-500 text-left">
                  <tr>
                    <th className="py-2 pr-3 font-semibold">Pessoa</th>
                    <th className="py-2 pr-3 font-semibold">Quando</th>
                    {results.questions.map((q, i) => (
                      <th key={q.id} className="py-2 pr-3 font-semibold" title={q.text}>P{i + 1}</th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-slate-700">
                  {individual.map((r) => (
                    <tr key={r.email}>
                      <td className="py-2 pr-3">
                        <div className="font-semibold">{r.name}</div>
                        <div className="text-slate-500">{r.area ?? 'sem área'}</div>
                      </td>
                      <td className="py-2 pr-3 whitespace-nowrap text-slate-500">{formatDateTime(r.submittedAt)}</td>
                      {r.answers.map((a, i) => (
                        <td key={i} className="py-2 pr-3">{a.answer}</td>
                      ))}
                    </tr>
                  ))}
                  {individual.length === 0 && (
                    <tr><td colSpan={2 + results.questions.length} className="py-3 text-slate-500">Nenhuma resposta ainda.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      {askReason && (
        <IndividualReasonModal
          onCancel={() => setAskReason(false)}
          onConfirm={async (reason) => {
            setIndividual(await api.surveysAdmin.individual(surveyId, reason));
            setAskReason(false);
          }}
        />
      )}
    </div>
  );
};

const IndividualReasonModal: React.FC<{ onCancel: () => void; onConfirm: (reason: string) => Promise<void> }> = ({ onCancel, onConfirm }) => {
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await onConfirm(reason.trim());
    } catch (err) {
      setError(errorMessage(err));
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <form onSubmit={submit} className="bg-white rounded-2xl w-full max-w-md shadow-2xl p-6 space-y-4">
        <div className="flex items-start gap-3">
          <ShieldAlert className="w-5 h-5 mt-0.5 text-amber-600" />
          <div>
            <h3 className="font-bold text-slate-900">Acesso a dado individual</h3>
            <p className="text-xs text-slate-500">Você vai ver o que cada pessoa respondeu. O motivo fica registrado na auditoria, com seu nome e horário.</p>
          </div>
        </div>
        <textarea
          required
          minLength={10}
          rows={3}
          maxLength={500}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Ex.: planejar ações de desenvolvimento com cada pessoa do setor"
          className={inputClass}
        />
        <FormError message={error} />
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onCancel} className="px-4 py-2 rounded-xl text-sm font-semibold text-slate-600 hover:bg-slate-100 cursor-pointer">Cancelar</button>
          <button type="submit" disabled={saving || reason.trim().length < 10} className="px-4 py-2 rounded-xl bg-amber-600 hover:bg-amber-700 text-white font-semibold text-sm disabled:opacity-60 cursor-pointer">
            {saving ? 'Abrindo...' : 'Registrar motivo e abrir'}
          </button>
        </div>
      </form>
    </div>
  );
};
