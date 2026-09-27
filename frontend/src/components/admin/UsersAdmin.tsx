import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FileUp, Mail, RefreshCw, UserPlus } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { api, refreshSession } from '../../services/api';
import type { AdminUser, AdminUserStatus, DepartmentOption, OrganizationSettings, UserRole } from '../../types';
import { ROLE_LABELS } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage, inputClass } from '../auth/authForm';
import { WorkspaceImport } from './WorkspaceImport';

const STATUS_STYLES: Record<AdminUserStatus, { label: string; className: string }> = {
  Active: { label: 'Ativo', className: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  Pending: { label: 'Nunca acessou', className: 'bg-slate-50 text-slate-600 border-slate-200' },
  Invited: { label: 'Convite enviado', className: 'bg-amber-50 text-amber-700 border-amber-200' },
  Inactive: { label: 'Desativado', className: 'bg-slate-100 text-slate-500 border-slate-200' },
};

const STATUS_FILTERS: { key: AdminUserStatus | 'All'; label: string }[] = [
  { key: 'All', label: 'Todos' },
  { key: 'Active', label: 'Já acessaram' },
  { key: 'Pending', label: 'Nunca acessaram' },
  { key: 'Invited', label: 'Convite enviado' },
  { key: 'Inactive', label: 'Desativados' },
];

// Mesmo limite do backend (AdminUsersController.BulkInviteMax): o lote é enviado em partes, com progresso.
const INVITE_BATCH = 25;

const isInvitable = (u: AdminUser) => u.status === 'Pending' || u.status === 'Invited';

const emptyInvite = { name: '', email: '', jobTitle: '', role: 'Employee' as UserRole, departmentId: '' };

export const UsersAdmin: React.FC = () => {
  const { user: currentUser } = useAuth();
  const isAdmin = currentUser?.role === 'Admin';
  const assignableRoles: UserRole[] = isAdmin ? ['Employee', 'Leader', 'HR', 'Admin'] : ['Employee', 'Leader', 'HR'];

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [departments, setDepartments] = useState<DepartmentOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const [statusFilter, setStatusFilter] = useState<AdminUserStatus | 'All'>('All');
  const [search, setSearch] = useState('');
  const [showImport, setShowImport] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [inviteProgress, setInviteProgress] = useState<{ done: number; total: number } | null>(null);

  const [invite, setInvite] = useState(emptyInvite);
  const [inviting, setInviting] = useState(false);
  const [inviteError, setInviteError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [list, deps] = await Promise.all([api.admin.listUsers(), api.admin.listDepartments()]);
      setUsers(list);
      setDepartments(deps);
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const statusCounts = useMemo(() => {
    const counts: Record<AdminUserStatus | 'All', number> = { All: users.length, Active: 0, Pending: 0, Invited: 0, Inactive: 0 };
    users.forEach((u) => counts[u.status]++);
    return counts;
  }, [users]);

  const visibleUsers = useMemo(() => {
    const term = search.trim().toLowerCase();
    return users.filter(
      (u) =>
        (statusFilter === 'All' || u.status === statusFilter) &&
        (!term ||
          u.name.toLowerCase().includes(term) ||
          u.email.includes(term) ||
          (u.departmentName ?? '').toLowerCase().includes(term) ||
          u.jobTitle.toLowerCase().includes(term)),
    );
  }, [users, statusFilter, search]);

  const canInvite = (u: AdminUser) => isInvitable(u) && (isAdmin || u.role !== 'Admin');
  const neverAccessed = useMemo(() => users.filter((u) => u.status === 'Pending' && (isAdmin || u.role !== 'Admin')), [users, isAdmin]);
  const visibleInvitable = visibleUsers.filter(canInvite);
  const allVisibleSelected = visibleInvitable.length > 0 && visibleInvitable.every((u) => selected.has(u.id));

  const toggleSelected = (id: string) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const toggleAllVisible = () =>
    setSelected((prev) => {
      const next = new Set(prev);
      visibleInvitable.forEach((u) => (allVisibleSelected ? next.delete(u.id) : next.add(u.id)));
      return next;
    });

  const sendSelectedInvites = async () => {
    const ids = [...selected];
    if (ids.length === 0) return;
    if (!window.confirm(`Enviar convite por e-mail para ${ids.length} ${ids.length === 1 ? 'pessoa' : 'pessoas'}?`)) return;

    setError(null);
    setNotice(null);
    const totals = { sent: 0, skipped: 0, failed: [] as string[] };
    setInviteProgress({ done: 0, total: ids.length });
    try {
      for (let i = 0; i < ids.length; i += INVITE_BATCH) {
        const result = await api.admin.sendInvitations(ids.slice(i, i + INVITE_BATCH));
        totals.sent += result.sent;
        totals.skipped += result.skipped;
        totals.failed.push(...result.failedEmails);
        setInviteProgress({ done: Math.min(i + INVITE_BATCH, ids.length), total: ids.length });
      }
      setNotice(
        `Convites enviados: ${totals.sent}.` +
          (totals.skipped ? ` Ignorados (já acessaram ou sem permissão): ${totals.skipped}.` : '') +
          (totals.failed.length ? ` Falharam: ${totals.failed.length} — confira a configuração de e-mail.` : ''),
      );
      setSelected(new Set(totals.failed.length ? ids.filter((id) => users.find((u) => u.id === id && totals.failed.includes(u.email))) : []));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setInviteProgress(null);
      await load();
    }
  };

  const replaceUser = (updated: AdminUser) => setUsers((prev) => prev.map((u) => (u.id === updated.id ? updated : u)));

  const runAction = async (id: string, action: () => Promise<void>) => {
    setBusyId(id);
    setError(null);
    setNotice(null);
    try {
      await action();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusyId(null);
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    setInviteError(null);
    setNotice(null);
    setInviting(true);
    try {
      const created = await api.admin.inviteUser({
        name: invite.name.trim(),
        email: invite.email.trim(),
        jobTitle: invite.jobTitle.trim(),
        role: invite.role,
        departmentId: invite.departmentId || undefined,
      });
      setUsers((prev) => [...prev, created].sort((a, b) => a.name.localeCompare(b.name)));
      setNotice(`Convite enviado para ${created.email}.`);
      setInvite(emptyInvite);
    } catch (err) {
      setInviteError(errorMessage(err));
      // Em 502 o usuário foi criado mas o e-mail falhou: recarrega para mostrar o convite pendente.
      await load();
    } finally {
      setInviting(false);
    }
  };

  const changeRole = (u: AdminUser, role: UserRole) =>
    runAction(u.id, async () => {
      replaceUser(await api.admin.updateUser(u.id, { name: u.name, jobTitle: u.jobTitle, role, departmentId: u.departmentId }));
    });

  const resend = (u: AdminUser) =>
    runAction(u.id, async () => {
      await api.admin.resendInvite(u.id);
      replaceUser({ ...u, status: 'Invited' });
      setNotice(`Convite enviado para ${u.email}.`);
    });

  const toggleActive = (u: AdminUser) =>
    runAction(u.id, async () => {
      if (u.status === 'Inactive') {
        replaceUser(await api.admin.reactivateUser(u.id));
      } else if (window.confirm(`Desativar ${u.name}? A pessoa perde o acesso na hora.`)) {
        replaceUser(await api.admin.deactivateUser(u.id));
      }
    });

  return (
    <div className="space-y-6 max-w-5xl">
      <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Usuários</h2>
          <p className="text-sm text-slate-500">Convide pessoas por e-mail ou importe do Google Workspace, e gerencie papéis e acessos.</p>
        </div>
        <button onClick={() => setShowImport(true)} className="flex items-center gap-2 px-4 py-2 rounded-xl border border-indigo-200 bg-indigo-50 text-indigo-700 font-semibold text-sm hover:bg-indigo-100 cursor-pointer">
          <FileUp className="w-4 h-4" />
          <span>Importar do Workspace</span>
        </button>
      </div>

      {showImport && (
        <WorkspaceImport
          isAdmin={isAdmin}
          onClose={() => setShowImport(false)}
          onImported={load}
        />
      )}

      <form onSubmit={handleInvite} className="bg-white rounded-2xl border border-slate-200 p-5 space-y-4">
        <div className="flex items-center space-x-2 text-slate-800 font-semibold text-sm">
          <UserPlus className="w-4 h-4 text-indigo-600" />
          <span>Convidar pessoa</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
          <input required placeholder="Nome completo" value={invite.name} onChange={(e) => setInvite({ ...invite, name: e.target.value })} className={`${inputClass} lg:col-span-1`} />
          <input required type="email" placeholder="E-mail" value={invite.email} onChange={(e) => setInvite({ ...invite, email: e.target.value })} className={inputClass} />
          <input required placeholder="Cargo" value={invite.jobTitle} onChange={(e) => setInvite({ ...invite, jobTitle: e.target.value })} className={inputClass} />
          <select value={invite.role} onChange={(e) => setInvite({ ...invite, role: e.target.value as UserRole })} className={inputClass} aria-label="Papel">
            {assignableRoles.map((role) => (
              <option key={role} value={role}>{ROLE_LABELS[role]}</option>
            ))}
          </select>
          <select value={invite.departmentId} onChange={(e) => setInvite({ ...invite, departmentId: e.target.value })} className={inputClass} aria-label="Departamento">
            <option value="">Sem departamento</option>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        </div>
        <FormError message={inviteError} />
        <button type="submit" disabled={inviting} className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm transition disabled:opacity-60 cursor-pointer">
          {inviting ? 'Enviando convite...' : 'Enviar convite'}
        </button>
      </form>

      {notice && <p className="text-sm text-emerald-800 bg-emerald-50 border border-emerald-200 rounded-xl px-3 py-2">{notice}</p>}
      <FormError message={error} />

      <div className="bg-white rounded-2xl border border-slate-200 overflow-x-auto">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3 px-5 py-3 border-b border-slate-100">
          <div className="flex flex-wrap gap-2">
            {STATUS_FILTERS.map(({ key, label }) => (
              <button
                key={key}
                onClick={() => setStatusFilter(key)}
                className={`text-xs font-semibold px-3 py-1.5 rounded-full border cursor-pointer ${statusFilter === key ? 'bg-indigo-600 text-white border-indigo-600' : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'}`}
              >
                {label} ({statusCounts[key]})
              </button>
            ))}
          </div>
          <div className="flex items-center gap-3">
            {neverAccessed.length > 0 && (
              <button
                onClick={() => setSelected(new Set(neverAccessed.map((u) => u.id)))}
                className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 cursor-pointer whitespace-nowrap"
              >
                Selecionar quem nunca acessou ({neverAccessed.length})
              </button>
            )}
            <input placeholder="Buscar nome, e-mail, cargo..." value={search} onChange={(e) => setSearch(e.target.value)} className="px-3 py-1.5 rounded-lg border border-slate-300 text-xs w-56" />
            <button onClick={load} disabled={loading} className="flex items-center space-x-1 text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer">
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
              <span>Atualizar</span>
            </button>
          </div>
        </div>
        {(selected.size > 0 || inviteProgress) && (
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 px-5 py-2 bg-indigo-50 border-b border-indigo-100 text-xs">
            <span className="text-indigo-900 font-semibold">
              {inviteProgress
                ? `Enviando convites... ${inviteProgress.done} de ${inviteProgress.total}`
                : `${selected.size} ${selected.size === 1 ? 'pessoa selecionada' : 'pessoas selecionadas'}`}
            </span>
            {!inviteProgress && (
              <div className="flex items-center gap-3">
                <button onClick={() => setSelected(new Set())} className="font-semibold text-slate-600 hover:text-slate-800 cursor-pointer">
                  Limpar seleção
                </button>
                <button onClick={sendSelectedInvites} className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-700 text-white font-semibold cursor-pointer">
                  <Mail className="w-3.5 h-3.5" />
                  <span>Enviar convite por e-mail</span>
                </button>
              </div>
            )}
          </div>
        )}
        <table className="w-full text-sm">
          <thead className="text-xs text-slate-500 text-left">
            <tr>
              <th className="pl-5 py-2 w-8">
                {visibleInvitable.length > 0 && (
                  <input type="checkbox" checked={allVisibleSelected} onChange={toggleAllVisible} aria-label="Selecionar todos os convidáveis visíveis" />
                )}
              </th>
              <th className="px-3 py-2 font-semibold">Pessoa</th>
              <th className="px-3 py-2 font-semibold">Papel</th>
              <th className="px-3 py-2 font-semibold">Situação</th>
              <th className="px-3 py-2 font-semibold">Último acesso</th>
              <th className="px-5 py-2 font-semibold text-right">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {visibleUsers.map((u) => {
              const isSelf = u.id === currentUser?.id;
              const canManage = isAdmin || u.role !== 'Admin';
              const busy = busyId === u.id;
              return (
                <tr key={u.id} className={u.status === 'Inactive' ? 'text-slate-400' : 'text-slate-700'}>
                  <td className="pl-5 py-3">
                    {canInvite(u) && (
                      <input type="checkbox" checked={selected.has(u.id)} onChange={() => toggleSelected(u.id)} aria-label={`Selecionar ${u.name}`} />
                    )}
                  </td>
                  <td className="px-3 py-3">
                    <div className="font-semibold">{u.name}</div>
                    <div className="text-xs text-slate-500">{[u.email, u.jobTitle, u.departmentName].filter(Boolean).join(' · ')}</div>
                  </td>
                  <td className="px-3 py-3">
                    {isAdmin && !isSelf ? (
                      <select value={u.role} disabled={busy} onChange={(e) => changeRole(u, e.target.value as UserRole)} className="px-2 py-1 rounded-lg border border-slate-300 text-xs" aria-label={`Papel de ${u.name}`}>
                        {assignableRoles.map((role) => (
                          <option key={role} value={role}>{ROLE_LABELS[role]}</option>
                        ))}
                      </select>
                    ) : (
                      <span className="text-xs">{ROLE_LABELS[u.role]}</span>
                    )}
                  </td>
                  <td className="px-3 py-3">
                    <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full border ${STATUS_STYLES[u.status].className}`}>
                      {STATUS_STYLES[u.status].label}
                    </span>
                  </td>
                  <td className="px-3 py-3 text-xs text-slate-500">
                    {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) : '—'}
                  </td>
                  <td className="px-5 py-3">
                    <div className="flex items-center justify-end space-x-3 text-xs font-semibold">
                      {(u.status === 'Pending' || u.status === 'Invited') && canManage && (
                        <button onClick={() => resend(u)} disabled={busy} className="flex items-center space-x-1 text-indigo-600 hover:text-indigo-800 cursor-pointer">
                          <Mail className="w-3.5 h-3.5" />
                          <span>{u.status === 'Pending' ? 'Enviar convite' : 'Reenviar convite'}</span>
                        </button>
                      )}
                      {isAdmin && !isSelf && (
                        <button onClick={() => toggleActive(u)} disabled={busy} className={`cursor-pointer ${u.status === 'Inactive' ? 'text-emerald-600 hover:text-emerald-800' : 'text-rose-600 hover:text-rose-800'}`}>
                          {u.status === 'Inactive' ? 'Reativar' : 'Desativar'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {isAdmin && <OrganizationSettingsForm />}
    </div>
  );
};

const OrganizationSettingsForm: React.FC = () => {
  const [settings, setSettings] = useState<OrganizationSettings | null>(null);
  const [form, setForm] = useState({ name: '', currencyName: '', monthlyCoinsQuota: '', googleWorkspaceDomains: '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const fill = (s: OrganizationSettings) => {
    setSettings(s);
    setForm({
      name: s.name,
      currencyName: s.currencyName,
      monthlyCoinsQuota: String(s.monthlyCoinsQuota),
      googleWorkspaceDomains: s.googleWorkspaceDomains.join('\n'),
    });
  };

  useEffect(() => {
    api.admin.getOrganizationSettings().then(fill).catch((err) => setError(errorMessage(err)));
  }, []);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    setSaved(false);
    try {
      fill(
        await api.admin.updateOrganizationSettings({
          name: form.name.trim(),
          currencyName: form.currencyName.trim(),
          monthlyCoinsQuota: Number(form.monthlyCoinsQuota),
          googleWorkspaceDomains: form.googleWorkspaceDomains.split(/[\s,;]+/).map((d) => d.trim()).filter(Boolean),
        }),
      );
      // Renova a sessão para o novo nome/moeda chegarem ao resto da interface.
      await refreshSession();
      setSaved(true);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  if (!settings) return <FormError message={error} />;

  return (
    <form onSubmit={handleSave} className="bg-white rounded-2xl border border-slate-200 p-5 space-y-4">
      <div>
        <h3 className="text-sm font-semibold text-slate-800">Configurações da organização</h3>
        <p className="text-xs text-slate-500 mt-1">Valem para toda esta instalação.</p>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Nome da organização</span>
          <input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className={`${inputClass} mt-1`} />
        </label>
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Nome da moeda</span>
          <input required value={form.currencyName} onChange={(e) => setForm({ ...form, currencyName: e.target.value })} className={`${inputClass} mt-1`} />
        </label>
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Moedas por pessoa por mês</span>
          <input required type="number" min={0} max={100000} value={form.monthlyCoinsQuota} onChange={(e) => setForm({ ...form, monthlyCoinsQuota: e.target.value })} className={`${inputClass} mt-1`} />
        </label>
      </div>
      <label className="block sm:max-w-xs">
        <span className="text-xs font-semibold text-slate-700">Domínios Google Workspace (login com Google)</span>
        <textarea
          rows={3}
          placeholder={'empresa.com.br\nempresa.com'}
          value={form.googleWorkspaceDomains}
          onChange={(e) => setForm({ ...form, googleWorkspaceDomains: e.target.value })}
          className={`${inputClass} mt-1 font-mono`}
        />
        <span className="text-[11px] text-slate-500">Um por linha. Um Workspace pode ter vários domínios.</span>
      </label>
      <p className="text-xs text-slate-500">
        Só entram pelo Google as pessoas já cadastradas aqui (convidadas ou importadas) cuja conta pertence a um desses domínios. Deixe em branco para desligar.
      </p>
      {!settings.googleLoginConfigured && (
        <p className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-2 py-1">
          O servidor ainda não tem um Client ID do Google configurado (Auth:Google:ClientId); o botão do Google não aparece até isso ser feito.
        </p>
      )}
      <FormError message={error} />
      {saved && <p className="text-xs text-emerald-700">Configurações salvas.</p>}
      <button type="submit" disabled={saving} className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm transition disabled:opacity-60 cursor-pointer">
        {saving ? 'Salvando...' : 'Salvar'}
      </button>
    </form>
  );
};
