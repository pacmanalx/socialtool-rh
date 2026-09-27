import type { SurveyStatus } from '../../types';

export const STATUS_STYLES: Record<SurveyStatus, { label: string; className: string }> = {
  Draft: { label: 'Rascunho', className: 'bg-slate-50 text-slate-600 border-slate-200' },
  Scheduled: { label: 'Agendada', className: 'bg-amber-50 text-amber-700 border-amber-200' },
  Open: { label: 'Aberta', className: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  Closed: { label: 'Encerrada', className: 'bg-slate-100 text-slate-500 border-slate-200' },
};

export const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

// <input type="datetime-local"> trabalha no horário local, sem fuso; o backend guarda em UTC.
export const toLocalInput = (iso: string) => {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

export const fromLocalInput = (value: string) => new Date(value).toISOString();

export const formatValue = (value: number | null) => (value === null ? '—' : Number(value).toLocaleString('pt-BR'));
