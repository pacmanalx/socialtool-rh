import React, { useEffect, useMemo, useState } from 'react';
import { ArrowDown, ArrowLeft, ArrowUp, Lock, Plus, Trash2 } from 'lucide-react';
import { api } from '../../services/api';
import type { AnswerScale, Area, SaveQuestion, SurveyDetail } from '../../types';
import { AREA_KIND_LABELS } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage, inputClass } from '../auth/authForm';
import { STATUS_STYLES, formatValue, fromLocalInput, toLocalInput } from './surveyUtils';

interface SurveyEditorProps {
  surveyId?: string;
  onDone: () => void;
  onResults: (id: string) => void;
}

type Form = {
  title: string;
  description: string;
  startsAt: string;
  endsAt: string;
  isAnonymous: boolean;
  audienceAll: boolean;
  audienceAreaIds: string[];
  questions: SaveQuestion[];
};

const inHours = (h: number) => toLocalInput(new Date(Date.now() + h * 3600_000).toISOString());

const emptyForm = (): Form => ({
  title: '',
  description: '',
  startsAt: inHours(0),
  endsAt: inHours(24 * 7),
  isAnonymous: true,
  audienceAll: true,
  audienceAreaIds: [],
  questions: [],
});

export const SurveyEditor: React.FC<SurveyEditorProps> = ({ surveyId, onDone, onResults }) => {
  const [survey, setSurvey] = useState<SurveyDetail | null>(null);
  const [form, setForm] = useState<Form>(emptyForm);
  const [scales, setScales] = useState<AnswerScale[]>([]);
  const [areas, setAreas] = useState<Area[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const fill = (s: SurveyDetail) => {
    setSurvey(s);
    setForm({
      title: s.title,
      description: s.description ?? '',
      startsAt: toLocalInput(s.startsAt),
      endsAt: toLocalInput(s.endsAt),
      isAnonymous: s.isAnonymous,
      audienceAll: s.audienceAll,
      audienceAreaIds: s.audienceAreaIds,
      questions: s.questions.map((q) => ({
        text: q.text,
        scaleName: q.scaleName,
        isNps: q.isNps,
        options: q.options.map((o) => ({ label: o.label, value: o.value })),
      })),
    });
  };

  useEffect(() => {
    Promise.all([api.surveysAdmin.scales(), api.areas.list(), surveyId ? api.surveysAdmin.get(surveyId) : Promise.resolve(null)])
      .then(([sc, ar, s]) => {
        setScales(sc);
        setAreas(ar);
        if (s) fill(s);
      })
      .catch((err) => setError(errorMessage(err)));
  }, [surveyId]);

  const closed = survey?.status === 'Closed';
  const locked = closed || (survey?.locked ?? false);
  const startLocked = closed || locked || survey?.status === 'Open';

  const selectedAreas = useMemo(() => new Set(form.audienceAreaIds), [form.audienceAreaIds]);
  const set = <K extends keyof Form>(key: K, value: Form[K]) => setForm((f) => ({ ...f, [key]: value }));

  const setQuestion = (i: number, q: SaveQuestion) => set('questions', form.questions.map((x, j) => (j === i ? q : x)));
  const moveQuestion = (i: number, delta: number) => {
    const list = [...form.questions];
    const [q] = list.splice(i, 1);
    list.splice(i + delta, 0, q);
    set('questions', list);
  };
  const applyScale = (i: number, scaleId: string) => {
    const scale = scales.find((s) => s.id === scaleId);
    if (scale) setQuestion(i, { ...form.questions[i], scaleName: scale.name, isNps: scale.isNps, options: scale.options.map((o) => ({ ...o })) });
  };
  const addQuestion = () => {
    const first = scales[0];
    set('questions', [
      ...form.questions,
      { text: '', scaleName: first?.name ?? '', isNps: first?.isNps ?? false, options: first ? first.options.map((o) => ({ ...o })) : [] },
    ]);
  };

  const payload = () => ({
    title: form.title.trim(),
    description: form.description.trim() || undefined,
    startsAt: fromLocalInput(form.startsAt),
    endsAt: fromLocalInput(form.endsAt),
    isAnonymous: form.isAnonymous,
    audienceAll: form.audienceAll,
    audienceAreaIds: form.audienceAll ? [] : form.audienceAreaIds,
    questions: form.questions,
  });

  const run = async (action: () => Promise<void>) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await action();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const save = () =>
    run(async () => {
      const saved = survey ? await api.surveysAdmin.update(survey.id, payload()) : await api.surveysAdmin.create(payload());
      fill(saved);
      setNotice('Enquete salva.');
    });

  const publish = () =>
    run(async () => {
      const saved = survey ? await api.surveysAdmin.update(survey.id, payload()) : await api.surveysAdmin.create(payload());
      const starts = new Date(saved.startsAt) > new Date();
      if (!window.confirm(starts ? `Publicar e agendar para ${new Date(saved.startsAt).toLocaleString('pt-BR')}?` : 'Publicar e abrir a enquete agora?')) {
        fill(saved);
        return;
      }
      fill(await api.surveysAdmin.publish(saved.id));
      setNotice(starts ? 'Enquete agendada.' : 'Enquete aberta: o público já pode responder.');
    });

  const close = () =>
    run(async () => {
      if (!survey) return;
      const msg = survey.status === 'Scheduled' ? 'Cancelar esta enquete agendada?' : 'Encerrar a enquete agora? Ninguém mais poderá responder.';
      if (!window.confirm(msg)) return;
      fill(await api.surveysAdmin.close(survey.id));
    });

  const remove = () =>
    run(async () => {
      if (!survey || !window.confirm('Apagar esta enquete?')) return;
      await api.surveysAdmin.remove(survey.id);
      onDone();
    });

  // Árvore de áreas para o público, indentada pelo caminho.
  const depth = (a: Area) => a.path.split(' › ').length - 1;

  return (
    <div className="space-y-6 max-w-4xl">
      <div className="flex items-center justify-between gap-3">
        <button onClick={onDone} className="flex items-center gap-1 text-sm font-semibold text-slate-600 hover:text-slate-800 cursor-pointer">
          <ArrowLeft className="w-4 h-4" /> Enquetes
        </button>
        {survey && (
          <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full border ${STATUS_STYLES[survey.status].className}`}>
            {STATUS_STYLES[survey.status].label} · {survey.responses} resposta(s)
          </span>
        )}
      </div>

      {locked && !closed && (
        <p className="flex items-start gap-2 text-sm text-amber-900 bg-amber-50 border border-amber-200 rounded-xl px-3 py-2">
          <Lock className="w-4 h-4 mt-0.5 shrink-0" />
          A enquete já tem respostas: perguntas, público e anonimato estão travados para não distorcer o resultado. Título, descrição e término ainda podem mudar.
        </p>
      )}

      <section className="bg-white rounded-2xl border border-slate-200 p-5 space-y-4">
        <input
          placeholder="Título da enquete"
          value={form.title}
          disabled={closed}
          onChange={(e) => set('title', e.target.value)}
          className={`${inputClass} text-base font-semibold`}
        />
        <textarea
          rows={2}
          placeholder="Descrição (opcional): o objetivo da enquete, para quem responde"
          value={form.description}
          disabled={closed}
          onChange={(e) => set('description', e.target.value)}
          className={inputClass}
        />
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <label className="block">
            <span className="text-xs font-semibold text-slate-700">Começa em</span>
            <input type="datetime-local" value={form.startsAt} disabled={startLocked} onChange={(e) => set('startsAt', e.target.value)} className={`${inputClass} mt-1`} />
          </label>
          <label className="block">
            <span className="text-xs font-semibold text-slate-700">Termina em</span>
            <input type="datetime-local" value={form.endsAt} disabled={closed} onChange={(e) => set('endsAt', e.target.value)} className={`${inputClass} mt-1`} />
          </label>
        </div>
        <div className="flex flex-col sm:flex-row gap-2">
          {[true, false].map((anon) => (
            <label
              key={String(anon)}
              className={`flex-1 flex items-start gap-2 p-3 rounded-xl border cursor-pointer ${form.isAnonymous === anon ? 'border-indigo-300 bg-indigo-50' : 'border-slate-200'} ${locked ? 'opacity-60' : ''}`}
            >
              <input type="radio" disabled={locked} checked={form.isAnonymous === anon} onChange={() => set('isAnonymous', anon)} className="mt-1" />
              <span>
                <span className="text-sm font-semibold text-slate-800">{anon ? 'Anônima' : 'Identificada'}</span>
                <span className="block text-xs text-slate-500">
                  {anon
                    ? 'Ninguém consegue ligar uma resposta a uma pessoa, nem com permissão especial.'
                    : 'A resposta fica ligada à pessoa. Ver respostas individuais exige permissão e motivo registrado.'}
                </span>
              </span>
            </label>
          ))}
        </div>
      </section>

      <section className="bg-white rounded-2xl border border-slate-200 p-5 space-y-3">
        <h3 className="text-sm font-semibold text-slate-800">Público</h3>
        <label className="flex items-center gap-2 text-sm">
          <input type="radio" disabled={locked} checked={form.audienceAll} onChange={() => set('audienceAll', true)} />
          Empresa inteira
        </label>
        <label className="flex items-center gap-2 text-sm">
          <input type="radio" disabled={locked} checked={!form.audienceAll} onChange={() => set('audienceAll', false)} />
          Só algumas áreas (inclui tudo o que está abaixo de cada área escolhida)
        </label>
        {!form.audienceAll && (
          <div className="max-h-64 overflow-y-auto border border-slate-200 rounded-xl p-2">
            {areas.length === 0 && <p className="text-xs text-slate-500 p-2">Nenhuma área cadastrada. Monte a estrutura em Administração › Estrutura.</p>}
            {areas.map((a) => (
              <label key={a.id} className="flex items-center gap-2 py-1 text-sm cursor-pointer" style={{ paddingLeft: 8 + depth(a) * 18 }}>
                <input
                  type="checkbox"
                  disabled={locked}
                  checked={selectedAreas.has(a.id)}
                  onChange={() =>
                    set('audienceAreaIds', selectedAreas.has(a.id) ? form.audienceAreaIds.filter((x) => x !== a.id) : [...form.audienceAreaIds, a.id])
                  }
                />
                <span className="text-slate-800">{a.name}</span>
                <span className="text-[11px] text-slate-400">{AREA_KIND_LABELS[a.kind]} · {a.totalMembers} pessoa(s)</span>
              </label>
            ))}
          </div>
        )}
      </section>

      <section className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-800">Perguntas</h3>
          {!locked && (
            <button onClick={addQuestion} className="flex items-center gap-1 text-xs font-semibold text-indigo-600 hover:text-indigo-800 cursor-pointer">
              <Plus className="w-3.5 h-3.5" /> Adicionar pergunta
            </button>
          )}
        </div>
        {form.questions.map((q, i) => (
          <div key={i} className="bg-white rounded-2xl border border-slate-200 p-4 space-y-3">
            <div className="flex items-start gap-2">
              <span className="text-xs font-bold text-slate-400 mt-2.5 w-5">{i + 1}.</span>
              <input
                placeholder="Texto da pergunta"
                value={q.text}
                disabled={locked}
                onChange={(e) => setQuestion(i, { ...q, text: e.target.value })}
                className={`${inputClass} flex-1`}
              />
              {!locked && (
                <div className="flex items-center gap-1 mt-1.5">
                  <button disabled={i === 0} onClick={() => moveQuestion(i, -1)} aria-label="Subir" className="p-1 text-slate-400 hover:text-slate-700 disabled:opacity-30 cursor-pointer"><ArrowUp className="w-4 h-4" /></button>
                  <button disabled={i === form.questions.length - 1} onClick={() => moveQuestion(i, 1)} aria-label="Descer" className="p-1 text-slate-400 hover:text-slate-700 disabled:opacity-30 cursor-pointer"><ArrowDown className="w-4 h-4" /></button>
                  <button onClick={() => set('questions', form.questions.filter((_, j) => j !== i))} aria-label="Remover" className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"><Trash2 className="w-4 h-4" /></button>
                </div>
              )}
            </div>
            <div className="pl-7 space-y-2">
              {!locked && (
                <select value={scales.find((s) => s.name === q.scaleName)?.id ?? ''} onChange={(e) => applyScale(i, e.target.value)} className={`${inputClass} sm:max-w-xs`} aria-label="Escala de respostas">
                  {!scales.some((s) => s.name === q.scaleName) && <option value="">{q.scaleName || 'Escolha a escala'}</option>}
                  {scales.map((s) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select>
              )}
              <div className="flex flex-wrap gap-1.5">
                {q.options.map((o) => (
                  <span key={o.label} className="text-[11px] px-2 py-0.5 rounded-full border border-slate-200 bg-slate-50 text-slate-600">
                    {o.label}
                    {o.value !== null && o.label !== String(o.value) && <span className="text-slate-400"> = {formatValue(o.value)}</span>}
                  </span>
                ))}
                {q.isNps && <span className="text-[11px] px-2 py-0.5 rounded-full border border-indigo-200 bg-indigo-50 text-indigo-700">calcula eNPS</span>}
              </div>
            </div>
          </div>
        ))}
        {form.questions.length === 0 && (
          <p className="text-sm text-slate-500 bg-white rounded-2xl border border-dashed border-slate-300 p-6 text-center">Nenhuma pergunta ainda.</p>
        )}
      </section>

      {notice && <p className="text-sm text-emerald-800 bg-emerald-50 border border-emerald-200 rounded-xl px-3 py-2">{notice}</p>}
      <FormError message={error} />

      <div className="flex flex-wrap items-center gap-2">
        {!closed && (
          <button onClick={save} disabled={busy} className="px-4 py-2 rounded-xl border border-slate-200 text-slate-700 font-semibold text-sm hover:bg-slate-50 disabled:opacity-60 cursor-pointer">
            {survey?.status === 'Draft' || !survey ? 'Salvar rascunho' : 'Salvar alterações'}
          </button>
        )}
        {(!survey || survey.status === 'Draft') && (
          <button onClick={publish} disabled={busy} className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm disabled:opacity-60 cursor-pointer">
            Publicar
          </button>
        )}
        {survey && (survey.status === 'Open' || survey.status === 'Scheduled') && (
          <button onClick={close} disabled={busy} className="px-4 py-2 rounded-xl bg-rose-600 hover:bg-rose-700 text-white font-semibold text-sm disabled:opacity-60 cursor-pointer">
            {survey.status === 'Scheduled' ? 'Cancelar agendamento' : 'Encerrar agora'}
          </button>
        )}
        {survey && survey.status !== 'Draft' && (
          <button onClick={() => onResults(survey.id)} className="px-4 py-2 rounded-xl text-indigo-600 font-semibold text-sm hover:bg-indigo-50 cursor-pointer">
            Ver resultado
          </button>
        )}
        {survey && survey.responses === 0 && (
          <button onClick={remove} disabled={busy} className="ml-auto px-4 py-2 rounded-xl text-rose-600 font-semibold text-sm hover:bg-rose-50 disabled:opacity-60 cursor-pointer">
            Apagar enquete
          </button>
        )}
      </div>
    </div>
  );
};
