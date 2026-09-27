import React, { useMemo, useState } from 'react';
import { FileUp, X } from 'lucide-react';
import { api } from '../../services/api';
import type { UserImportAction, UserImportOptions, UserImportPreview, UserImportResult } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage, inputClass } from '../auth/authForm';

const ACTION_LABELS: Record<UserImportAction, string> = {
  Create: 'Novo',
  Update: 'Atualizar',
  Deactivate: 'Desativar',
  Unchanged: 'Sem mudança',
  Skip: 'Ignorado',
};

const ACTION_STYLES: Record<UserImportAction, string> = {
  Create: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  Update: 'bg-sky-50 text-sky-700 border-sky-200',
  Deactivate: 'bg-rose-50 text-rose-700 border-rose-200',
  Unchanged: 'bg-slate-50 text-slate-500 border-slate-200',
  Skip: 'bg-amber-50 text-amber-700 border-amber-200',
};

const ACTIONABLE: UserImportAction[] = ['Create', 'Update', 'Deactivate'];
const VISIBLE_ROWS = 200;

interface WorkspaceImportProps {
  isAdmin: boolean;
  onClose: () => void;
  onImported: () => void;
}

export const WorkspaceImport: React.FC<WorkspaceImportProps> = ({ isAdmin, onClose, onImported }) => {
  const [file, setFile] = useState<File | null>(null);
  const [options, setOptions] = useState<Omit<UserImportOptions, 'excludedEmails'>>({
    includeNeverSignedIn: false,
    deactivateSuspended: false,
    createMissingDepartments: false,
  });
  const [excluded, setExcluded] = useState<Set<string>>(new Set());
  const [preview, setPreview] = useState<UserImportPreview | null>(null);
  const [result, setResult] = useState<UserImportResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<UserImportAction | 'All'>('All');
  const [search, setSearch] = useState('');

  const analyze = async (nextOptions = options, nextFile = file) => {
    if (!nextFile) return;
    setBusy(true);
    setError(null);
    try {
      setPreview(await api.admin.previewUserImport(nextFile, { ...nextOptions, excludedEmails: [] }));
      setExcluded(new Set());
    } catch (err) {
      setPreview(null);
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const changeOption = (key: keyof typeof options, value: boolean) => {
    const next = { ...options, [key]: value };
    setOptions(next);
    if (preview) analyze(next);
  };

  const apply = async () => {
    if (!file) return;
    setBusy(true);
    setError(null);
    try {
      setResult(await api.admin.applyUserImport(file, { ...options, excludedEmails: [...excluded] }));
      onImported();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const toggleExcluded = (email: string) =>
    setExcluded((prev) => {
      const next = new Set(prev);
      if (next.has(email)) next.delete(email);
      else next.add(email);
      return next;
    });

  // Retirar uma linha é decisão do navegador; o servidor refaz a comparação ao importar.
  const effective = useMemo(() => {
    const counts: Record<UserImportAction, number> = { Create: 0, Update: 0, Deactivate: 0, Unchanged: 0, Skip: 0 };
    for (const row of preview?.rows ?? []) {
      counts[excluded.has(row.email) && ACTIONABLE.includes(row.action) ? 'Skip' : row.action]++;
    }
    return counts;
  }, [preview, excluded]);

  const rows = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (preview?.rows ?? []).filter(
      (row) =>
        (filter === 'All' || row.action === filter) &&
        (!term || row.email.includes(term) || row.name.toLowerCase().includes(term)),
    );
  }, [preview, filter, search]);

  const toApply = effective.Create + effective.Update + effective.Deactivate;

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl w-full max-w-5xl max-h-[92vh] flex flex-col shadow-2xl">
        <div className="flex items-start justify-between p-5 border-b border-slate-100">
          <div>
            <h3 className="font-bold text-slate-900 text-base">Importar usuários do Google Workspace</h3>
            <p className="text-xs text-slate-500 mt-1">
              Admin Console → Diretório → Usuários → "Fazer o download dos usuários" (JSON). O arquivo é lido e descartado; só nome, e-mail, cargo, departamento e status são usados.
            </p>
          </div>
          <button onClick={onClose} aria-label="Fechar" className="p-1 text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-100 cursor-pointer">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="p-5 space-y-4 overflow-y-auto">
          {result ? (
            <div className="space-y-3">
              <p className="text-sm text-emerald-800 bg-emerald-50 border border-emerald-200 rounded-xl px-3 py-2">
                Importação concluída: {result.created} novos, {result.updated} atualizados, {result.deactivated} desativados,{' '}
                {result.skipped} ignorados{result.departmentsCreated > 0 ? `, ${result.departmentsCreated} departamentos criados` : ''}.
              </p>
              <p className="text-xs text-slate-500">
                Ninguém recebeu e-mail. Os novos usuários aparecem como "Nunca acessou": entram com a conta Google (se o login com Google estiver configurado) ou pelo convite que você enviar na lista.
              </p>
            </div>
          ) : (
            <>
              <div className="flex flex-col sm:flex-row sm:items-center gap-3">
                <label className="flex items-center gap-2 px-3 py-2 rounded-xl border border-dashed border-slate-300 text-sm text-slate-600 cursor-pointer hover:bg-slate-50">
                  <FileUp className="w-4 h-4 text-indigo-600" />
                  <span className="truncate max-w-xs">{file ? file.name : 'Escolher arquivo .json'}</span>
                  <input
                    type="file"
                    accept=".json,application/json"
                    className="hidden"
                    onChange={(e) => {
                      const chosen = e.target.files?.[0] ?? null;
                      setFile(chosen);
                      setPreview(null);
                      if (chosen) analyze(options, chosen);
                    }}
                  />
                </label>
                {busy && <span className="text-xs text-slate-500">Analisando...</span>}
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 text-xs text-slate-700">
                <label className="flex items-start gap-2">
                  <input type="checkbox" checked={options.includeNeverSignedIn} onChange={(e) => changeOption('includeNeverSignedIn', e.target.checked)} className="mt-0.5" />
                  <span>Incluir contas que nunca entraram no Google (em geral de serviço ou compartilhadas)</span>
                </label>
                <label className="flex items-start gap-2">
                  <input type="checkbox" checked={options.createMissingDepartments} onChange={(e) => changeOption('createMissingDepartments', e.target.checked)} className="mt-0.5" />
                  <span>Criar departamentos que ainda não existem</span>
                </label>
                {isAdmin && (
                  <label className="flex items-start gap-2">
                    <input type="checkbox" checked={options.deactivateSuspended} onChange={(e) => changeOption('deactivateSuspended', e.target.checked)} className="mt-0.5" />
                    <span>Desativar aqui quem está suspenso no Workspace</span>
                  </label>
                )}
              </div>

              <FormError message={error} />

              {preview && (
                <>
                  <div className="grid grid-cols-2 sm:grid-cols-6 gap-2">
                    {(['All', 'Create', 'Update', 'Deactivate', 'Unchanged', 'Skip'] as const).map((key) => {
                      const value = key === 'All' ? preview.totalInFile : effective[key];
                      return (
                        <button
                          key={key}
                          onClick={() => setFilter(key)}
                          className={`text-left rounded-xl border px-3 py-2 cursor-pointer ${filter === key ? 'border-indigo-400 bg-indigo-50' : 'border-slate-200 hover:bg-slate-50'}`}
                        >
                          <div className="text-lg font-bold text-slate-900">{value}</div>
                          <div className="text-[11px] text-slate-500">{key === 'All' ? 'no arquivo' : ACTION_LABELS[key]}</div>
                        </button>
                      );
                    })}
                  </div>

                  <div className="text-xs text-slate-500 space-y-1">
                    {preview.localNotInFile > 0 && (
                      <p>{preview.localNotInFile} pessoas ativas aqui não estão no arquivo — nada muda para elas.</p>
                    )}
                    {preview.departmentsToCreate.length > 0 && (
                      <p>Serão criados {preview.departmentsToCreate.length} departamentos.</p>
                    )}
                    {preview.unmappedDepartments.length > 0 && (
                      <p>
                        {preview.unmappedDepartments.length} departamentos do arquivo não existem aqui e ficarão em branco
                        {options.createMissingDepartments ? '' : ' (marque "Criar departamentos" para criá-los)'}.
                      </p>
                    )}
                  </div>

                  <input placeholder="Buscar por nome ou e-mail" value={search} onChange={(e) => setSearch(e.target.value)} className={`${inputClass} sm:max-w-xs`} />

                  <div className="border border-slate-200 rounded-xl overflow-x-auto">
                    <table className="w-full text-xs">
                      <thead className="text-slate-500 text-left bg-slate-50">
                        <tr>
                          <th className="px-3 py-2 font-semibold w-8"></th>
                          <th className="px-3 py-2 font-semibold">Pessoa</th>
                          <th className="px-3 py-2 font-semibold">Cargo / departamento</th>
                          <th className="px-3 py-2 font-semibold">Ação</th>
                          <th className="px-3 py-2 font-semibold">Detalhe</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {rows.slice(0, VISIBLE_ROWS).map((row) => {
                          const actionable = ACTIONABLE.includes(row.action);
                          const isExcluded = excluded.has(row.email);
                          return (
                            <tr key={row.email} className={isExcluded ? 'opacity-50' : ''}>
                              <td className="px-3 py-2">
                                {actionable && (
                                  <input type="checkbox" checked={!isExcluded} onChange={() => toggleExcluded(row.email)} aria-label={`Incluir ${row.email}`} />
                                )}
                              </td>
                              <td className="px-3 py-2">
                                <div className="font-semibold text-slate-800">{row.name || '—'}</div>
                                <div className="text-slate-500">{row.email}</div>
                              </td>
                              <td className="px-3 py-2 text-slate-600">
                                {row.jobTitle || '—'}
                                {row.department ? ` · ${row.department}` : ''}
                              </td>
                              <td className="px-3 py-2">
                                <span className={`font-semibold px-2 py-0.5 rounded-full border ${ACTION_STYLES[row.action]}`}>
                                  {isExcluded ? 'Retirado' : ACTION_LABELS[row.action]}
                                </span>
                              </td>
                              <td className="px-3 py-2 text-slate-500">{[...row.changes, row.reason].filter(Boolean).join(' · ')}</td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                  {rows.length > VISIBLE_ROWS && (
                    <p className="text-xs text-slate-500">Mostrando {VISIBLE_ROWS} de {rows.length}. Use a busca ou os filtros para achar o resto.</p>
                  )}
                </>
              )}
            </>
          )}
        </div>

        <div className="flex items-center justify-end gap-3 p-5 border-t border-slate-100">
          <button onClick={onClose} className="px-4 py-2 rounded-xl text-sm font-semibold text-slate-600 hover:bg-slate-100 cursor-pointer">
            {result ? 'Fechar' : 'Cancelar'}
          </button>
          {!result && (
            <button
              onClick={apply}
              disabled={busy || !preview || toApply === 0}
              className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm transition disabled:opacity-60 cursor-pointer disabled:cursor-not-allowed"
            >
              {busy && preview ? 'Importando...' : `Importar ${toApply} ${toApply === 1 ? 'registro' : 'registros'}`}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
