import React, { useCallback, useEffect, useState } from 'react';
import { RefreshCw, ScrollText } from 'lucide-react';
import { api } from '../../services/api';
import type { AuditPage } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage } from '../auth/authForm';

// Prefixos de AuditService.Actions no backend; o filtro usa "começa com".
const ACTION_FILTERS: { key: string; label: string }[] = [
  { key: '', label: 'Todas as ações' },
  { key: 'user.', label: 'Usuários' },
  { key: 'user.revoked', label: 'Acessos revogados' },
  { key: 'user.authorized', label: 'Acessos liberados' },
  { key: 'user.role_changed', label: 'Mudanças de papel' },
  { key: 'permissions.', label: 'Permissões' },
  { key: 'users.imported', label: 'Importações' },
  { key: 'structure.', label: 'Estrutura' },
  { key: 'survey.', label: 'Enquetes' },
  { key: 'organization.', label: 'Organização' },
  { key: 'sensitive.', label: 'Acesso a dado sensível' },
];

const ACTION_LABELS: Record<string, string> = {
  'user.invited': 'Cadastro',
  'user.invite_resent': 'Convite reenviado',
  'user.invites_bulk': 'Convites em lote',
  'user.updated': 'Edição',
  'user.role_changed': 'Papel',
  'user.revoked': 'Acesso revogado',
  'user.authorized': 'Acesso liberado',
  'users.imported': 'Importação',
  'permissions.changed': 'Permissões',
  'organization.updated': 'Organização',
  'sensitive.accessed': 'Dado sensível',
  'structure.area_created': 'Área criada',
  'structure.area_updated': 'Área alterada',
  'structure.area_deleted': 'Área apagada',
  'structure.members_changed': 'Pessoas na área',
  'survey.created': 'Enquete criada',
  'survey.updated': 'Enquete alterada',
  'survey.published': 'Enquete publicada',
  'survey.closed': 'Enquete encerrada',
  'survey.deleted': 'Enquete apagada',
  'survey.scale_changed': 'Escala de resposta',
};

const PAGE_SIZE = 50;

export const AuditLog: React.FC = () => {
  const [data, setData] = useState<AuditPage | null>(null);
  const [page, setPage] = useState(1);
  const [action, setAction] = useState('');
  const [search, setSearch] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setData(await api.admin.listAudit({ page, pageSize: PAGE_SIZE, action, search: appliedSearch }));
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [page, action, appliedSearch]);

  useEffect(() => {
    load();
  }, [load]);

  const pages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1;

  return (
    <div className="space-y-6 max-w-5xl">
      <div>
        <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
          <ScrollText className="w-5 h-5 text-indigo-600" /> Auditoria
        </h2>
        <p className="text-sm text-slate-500">Quem fez o quê, sobre quem e quando. Os registros não podem ser alterados nem apagados.</p>
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 overflow-x-auto">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 px-5 py-3 border-b border-slate-100">
          <select
            value={action}
            onChange={(e) => {
              setAction(e.target.value);
              setPage(1);
            }}
            className="px-3 py-1.5 rounded-lg border border-slate-300 text-xs"
            aria-label="Filtrar por ação"
          >
            {ACTION_FILTERS.map((f) => (
              <option key={f.key} value={f.key}>{f.label}</option>
            ))}
          </select>
          <form
            className="flex items-center gap-3"
            onSubmit={(e) => {
              e.preventDefault();
              setAppliedSearch(search.trim());
              setPage(1);
            }}
          >
            <input placeholder="Buscar pessoa, e-mail, motivo..." value={search} onChange={(e) => setSearch(e.target.value)} className="px-3 py-1.5 rounded-lg border border-slate-300 text-xs w-60" />
            <button type="button" onClick={load} disabled={loading} className="flex items-center space-x-1 text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer">
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
              <span>Atualizar</span>
            </button>
          </form>
        </div>

        <FormError message={error} />

        <table className="w-full text-sm">
          <thead className="text-xs text-slate-500 text-left">
            <tr>
              <th className="pl-5 px-3 py-2 font-semibold whitespace-nowrap">Quando</th>
              <th className="px-3 py-2 font-semibold">Quem</th>
              <th className="px-3 py-2 font-semibold">Ação</th>
              <th className="px-5 py-2 font-semibold">O que aconteceu</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 text-slate-700">
            {data?.items.map((a) => (
              <tr key={a.id}>
                <td className="pl-5 px-3 py-3 text-xs text-slate-500 whitespace-nowrap">
                  {new Date(a.createdAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                </td>
                <td className="px-3 py-3 text-xs font-semibold whitespace-nowrap">{a.actorName}</td>
                <td className="px-3 py-3">
                  <span className="text-[11px] font-semibold px-2 py-0.5 rounded-full border bg-slate-50 text-slate-600 border-slate-200 whitespace-nowrap">
                    {ACTION_LABELS[a.action] ?? a.action}
                  </span>
                </td>
                <td className="px-5 py-3 text-xs">
                  {a.summary}
                  {a.reason && <div className="text-slate-500 mt-0.5">Motivo: {a.reason}</div>}
                </td>
              </tr>
            ))}
            {data && data.items.length === 0 && (
              <tr>
                <td colSpan={4} className="px-5 py-6 text-center text-sm text-slate-500">Nenhum registro encontrado.</td>
              </tr>
            )}
          </tbody>
        </table>

        {data && data.total > PAGE_SIZE && (
          <div className="flex items-center justify-between px-5 py-3 border-t border-slate-100 text-xs">
            <span className="text-slate-500">{data.total} registros</span>
            <div className="flex items-center gap-3">
              <button disabled={page <= 1} onClick={() => setPage(page - 1)} className="font-semibold text-indigo-600 disabled:text-slate-300 cursor-pointer">Anterior</button>
              <span className="text-slate-500">Página {page} de {pages}</span>
              <button disabled={page >= pages} onClick={() => setPage(page + 1)} className="font-semibold text-indigo-600 disabled:text-slate-300 cursor-pointer">Próxima</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
