import React, { useCallback, useEffect, useState } from 'react';
import { ArrowLeft, CheckCircle2, ClipboardList, EyeOff, UserRound } from 'lucide-react';
import { api } from '../../services/api';
import type { MySurvey, SurveyToAnswer } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage } from '../auth/authForm';
import { formatDateTime } from './surveyUtils';

// Enquetes abertas para a pessoa (só as do público dela) e o formulário de resposta.
export const MySurveys: React.FC = () => {
  const [surveys, setSurveys] = useState<MySurvey[]>([]);
  const [openId, setOpenId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setSurveys(await api.surveys.mine());
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!openId) load();
  }, [openId, load]);

  if (openId) return <AnswerSurvey surveyId={openId} onBack={() => setOpenId(null)} />;

  const pending = surveys.filter((s) => !s.answered);
  const answered = surveys.filter((s) => s.answered);

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
          <ClipboardList className="w-5 h-5 text-indigo-600" /> Enquetes
        </h2>
        <p className="text-sm text-slate-500">Sua opinião ajuda a decidir o que muda por aqui.</p>
      </div>

      <FormError message={error} />
      {!loading && surveys.length === 0 && (
        <div className="bg-white rounded-2xl border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500">Nenhuma enquete aberta para você agora.</div>
      )}

      {pending.map((s) => (
        <button key={s.id} onClick={() => setOpenId(s.id)} className="w-full text-left bg-white rounded-2xl border border-indigo-200 hover:border-indigo-400 p-5 transition cursor-pointer">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h3 className="font-semibold text-slate-900">{s.title}</h3>
              {s.description && <p className="text-sm text-slate-600 mt-1">{s.description}</p>}
              <p className="text-xs text-slate-500 mt-2 flex items-center gap-3">
                <span>{s.questionCount} pergunta(s)</span>
                <span>até {formatDateTime(s.endsAt)}</span>
                <AnonymityBadge anonymous={s.isAnonymous} />
              </p>
            </div>
            <span className="text-xs font-semibold text-white bg-indigo-600 px-3 py-1.5 rounded-lg whitespace-nowrap">Responder</span>
          </div>
        </button>
      ))}

      {answered.length > 0 && (
        <div className="space-y-2">
          <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Já respondidas</p>
          {answered.map((s) => (
            <div key={s.id} className="bg-white rounded-2xl border border-slate-200 px-5 py-3 flex items-center justify-between">
              <span className="text-sm text-slate-700">{s.title}</span>
              <span className="flex items-center gap-1 text-xs text-emerald-600 font-semibold"><CheckCircle2 className="w-4 h-4" /> Respondida</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

const AnonymityBadge: React.FC<{ anonymous: boolean }> = ({ anonymous }) =>
  anonymous ? (
    <span className="flex items-center gap-1 text-emerald-700"><EyeOff className="w-3.5 h-3.5" /> anônima</span>
  ) : (
    <span className="flex items-center gap-1 text-amber-700"><UserRound className="w-3.5 h-3.5" /> identificada</span>
  );

const AnswerSurvey: React.FC<{ surveyId: string; onBack: () => void }> = ({ surveyId, onBack }) => {
  const [survey, setSurvey] = useState<SurveyToAnswer | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [sending, setSending] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.surveys.get(surveyId).then(setSurvey).catch((err) => setError(errorMessage(err)));
  }, [surveyId]);

  const missing = survey ? survey.questions.filter((q) => !answers[q.id]).length : 0;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!survey) return;
    setSending(true);
    setError(null);
    try {
      await api.surveys.submit(survey.id, survey.questions.map((q) => ({ questionId: q.id, optionId: answers[q.id] })));
      setDone(true);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="space-y-6 max-w-3xl">
      <button onClick={onBack} className="flex items-center gap-1 text-sm font-semibold text-slate-600 hover:text-slate-800 cursor-pointer">
        <ArrowLeft className="w-4 h-4" /> Enquetes
      </button>
      <FormError message={error} />
      {survey && (done || survey.answered) ? (
        <div className="bg-white rounded-2xl border border-emerald-200 p-8 text-center space-y-2">
          <CheckCircle2 className="w-10 h-10 text-emerald-500 mx-auto" />
          <h3 className="font-bold text-slate-900">Obrigado pela resposta!</h3>
          <p className="text-sm text-slate-500">{survey.isAnonymous ? 'Sua resposta é anônima: ninguém consegue ligá-la a você.' : 'Sua resposta foi registrada.'}</p>
        </div>
      ) : (
        survey && (
          <form onSubmit={submit} className="space-y-4">
            <div className="bg-white rounded-2xl border border-slate-200 p-5">
              <h2 className="text-lg font-bold text-slate-900">{survey.title}</h2>
              {survey.description && <p className="text-sm text-slate-600 mt-1">{survey.description}</p>}
              <p className="text-xs text-slate-500 mt-3 flex items-center gap-3">
                <span>Aberta até {formatDateTime(survey.endsAt)}</span>
                <AnonymityBadge anonymous={survey.isAnonymous} />
              </p>
              <p className="text-xs text-slate-500 mt-1">
                {survey.isAnonymous
                  ? 'Registramos apenas que você respondeu, para não contar duas vezes. O que você responde não fica ligado ao seu nome.'
                  : 'Esta enquete é identificada: sua resposta fica ligada ao seu nome.'}
              </p>
            </div>

            {survey.questions.map((q, i) => (
              <fieldset key={q.id} className="bg-white rounded-2xl border border-slate-200 p-5 space-y-3">
                <legend className="sr-only">{q.text}</legend>
                <p className="text-sm font-semibold text-slate-800">{i + 1}. {q.text}</p>
                <div className={`flex flex-wrap gap-2 ${q.options.length > 6 ? '' : 'flex-col sm:flex-row'}`}>
                  {q.options.map((o) => {
                    const checked = answers[q.id] === o.id;
                    return (
                      <label
                        key={o.id}
                        className={`flex items-center gap-2 px-3 py-2 rounded-xl border text-sm cursor-pointer transition ${checked ? 'border-indigo-500 bg-indigo-50 text-indigo-800 font-semibold' : 'border-slate-200 text-slate-700 hover:bg-slate-50'}`}
                      >
                        <input type="radio" name={q.id} checked={checked} onChange={() => setAnswers({ ...answers, [q.id]: o.id })} className="sr-only" />
                        {o.label}
                      </label>
                    );
                  })}
                </div>
                {q.isNps && <p className="text-[11px] text-slate-400">0 = de jeito nenhum · 10 = com certeza</p>}
              </fieldset>
            ))}

            <div className="flex items-center gap-3">
              <button type="submit" disabled={sending || missing > 0} className="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm disabled:opacity-60 cursor-pointer">
                {sending ? 'Enviando...' : 'Enviar respostas'}
              </button>
              {missing > 0 && <span className="text-xs text-slate-500">Falta(m) {missing} pergunta(s).</span>}
            </div>
          </form>
        )
      )}
    </div>
  );
};
