import React, { useCallback, useEffect, useState } from 'react';
import { ArrowLeft, Copy, Pencil, Plus, Trash2, X } from 'lucide-react';
import { api } from '../../services/api';
import type { AnswerScale, ScaleOption } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage, inputClass } from '../auth/authForm';
import { formatValue } from './surveyUtils';

type Draft = { id?: string; name: string; isNps: boolean; options: { label: string; value: string }[] };

const toDraft = (s: AnswerScale, copy = false): Draft => ({
  id: copy ? undefined : s.id,
  name: copy ? `${s.name} (cópia)` : s.name,
  isNps: s.isNps,
  options: s.options.map((o) => ({ label: o.label, value: o.value === null ? '' : String(o.value) })),
});

// Escalas de resposta ("dimensões"): cada opção tem rótulo e valor, para o resultado sair em números.
export const ScalesManager: React.FC<{ onBack: () => void }> = ({ onBack }) => {
  const [scales, setScales] = useState<AnswerScale[]>([]);
  const [draft, setDraft] = useState<Draft | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setScales(await api.surveysAdmin.scales());
    } catch (err) {
      setError(errorMessage(err));
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!draft) return;
    setSaving(true);
    setError(null);
    try {
      const options: ScaleOption[] = draft.options.map((o) => ({
        label: o.label.trim(),
        value: o.value.trim() === '' ? null : Number(o.value.replace(',', '.')),
      }));
      if (options.some((o) => o.value !== null && Number.isNaN(o.value))) throw new Error('Os valores precisam ser números (ou ficar em branco).');
      const data = { name: draft.name.trim(), isNps: draft.isNps, options };
      if (draft.id) await api.surveysAdmin.updateScale(draft.id, data);
      else await api.surveysAdmin.createScale(data);
      setDraft(null);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (s: AnswerScale) => {
    if (!window.confirm(`Apagar a escala "${s.name}"? Enquetes que já a usaram não mudam.`)) return;
    try {
      await api.surveysAdmin.deleteScale(s.id);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    }
  };

  const setOption = (i: number, key: 'label' | 'value', value: string) =>
    draft && setDraft({ ...draft, options: draft.options.map((o, j) => (j === i ? { ...o, [key]: value } : o)) });

  return (
    <div className="space-y-6 max-w-4xl">
      <button onClick={onBack} className="flex items-center gap-1 text-sm font-semibold text-slate-600 hover:text-slate-800 cursor-pointer">
        <ArrowLeft className="w-4 h-4" /> Enquetes
      </button>
      <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Escalas de resposta</h2>
          <p className="text-sm text-slate-500">
            Cada opção tem um rótulo e um valor. O valor é o que entra na média; sem valor, a opção só conta na distribuição (ex.: dias da semana).
            Mudar uma escala não altera enquetes que já a usaram.
          </p>
        </div>
        <button
          onClick={() => setDraft({ name: '', isNps: false, options: [{ label: '', value: '' }, { label: '', value: '' }] })}
          className="flex items-center gap-2 px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm cursor-pointer whitespace-nowrap"
        >
          <Plus className="w-4 h-4" /> Nova escala
        </button>
      </div>

      <FormError message={error} />

      {draft && (
        <form onSubmit={save} className="bg-white rounded-2xl border border-slate-200 p-5 space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sm font-semibold text-slate-800">{draft.id ? 'Editar escala' : 'Nova escala'}</span>
            <button type="button" onClick={() => setDraft(null)} aria-label="Fechar" className="p-1 text-slate-400 hover:text-slate-600 cursor-pointer"><X className="w-4 h-4" /></button>
          </div>
          <input required placeholder="Nome da escala" value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} className={inputClass} />
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={draft.isNps} onChange={(e) => setDraft({ ...draft, isNps: e.target.checked })} />
            Calcular eNPS (valores de 0 a 10: promotores 9–10, detratores 0–6)
          </label>
          <div className="space-y-2">
            {draft.options.map((o, i) => (
              <div key={i} className="flex items-center gap-2">
                <input required placeholder={`Rótulo ${i + 1}`} value={o.label} onChange={(e) => setOption(i, 'label', e.target.value)} className={`${inputClass} flex-1`} />
                <input placeholder="Valor" inputMode="decimal" value={o.value} onChange={(e) => setOption(i, 'value', e.target.value)} className={`${inputClass} w-24`} />
                <button type="button" disabled={draft.options.length <= 2} onClick={() => setDraft({ ...draft, options: draft.options.filter((_, j) => j !== i) })} aria-label="Remover opção" className="p-1 text-slate-400 hover:text-rose-600 disabled:opacity-30 cursor-pointer">
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            ))}
            {draft.options.length < 11 && (
              <button type="button" onClick={() => setDraft({ ...draft, options: [...draft.options, { label: '', value: '' }] })} className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 cursor-pointer">
                + Opção
              </button>
            )}
          </div>
          <button type="submit" disabled={saving} className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm disabled:opacity-60 cursor-pointer">
            {saving ? 'Salvando...' : 'Salvar escala'}
          </button>
        </form>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {scales.map((s) => (
          <div key={s.id} className="bg-white rounded-2xl border border-slate-200 p-4 space-y-2">
            <div className="flex items-start justify-between gap-2">
              <div>
                <h3 className="text-sm font-semibold text-slate-800">{s.name}</h3>
                <p className="text-[11px] text-slate-500">{s.isSystem ? 'do sistema' : 'personalizada'}{s.isNps && ' · eNPS'}</p>
              </div>
              <div className="flex items-center gap-2 text-xs font-semibold">
                <button onClick={() => setDraft(toDraft(s, true))} className="flex items-center gap-1 text-slate-500 hover:text-slate-800 cursor-pointer" title="Criar uma nova a partir desta"><Copy className="w-3.5 h-3.5" /></button>
                {!s.isSystem && (
                  <>
                    <button onClick={() => setDraft(toDraft(s))} className="text-slate-500 hover:text-slate-800 cursor-pointer" aria-label="Editar"><Pencil className="w-3.5 h-3.5" /></button>
                    <button onClick={() => remove(s)} className="text-slate-500 hover:text-rose-600 cursor-pointer" aria-label="Apagar"><Trash2 className="w-3.5 h-3.5" /></button>
                  </>
                )}
              </div>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {s.options.map((o) => (
                <span key={o.label} className="text-[11px] px-2 py-0.5 rounded-full border border-slate-200 bg-slate-50 text-slate-600">
                  {o.label}
                  {o.value !== null && o.label !== String(o.value) && <span className="text-slate-400"> = {formatValue(o.value)}</span>}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
