import React, { useEffect, useMemo, useState } from 'react';
import { KeyRound, X } from 'lucide-react';
import { api } from '../../services/api';
import type { AdminUser, PermissionDefinition } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage } from '../auth/authForm';

interface PermissionsModalProps {
  user: AdminUser;
  onClose: () => void;
}

// Só o Admin abre: concede ou retira, pessoa a pessoa, o que um gestor de RH pode fazer.
export const PermissionsModal: React.FC<PermissionsModalProps> = ({ user, onClose }) => {
  const [catalog, setCatalog] = useState<PermissionDefinition[]>([]);
  const [granted, setGranted] = useState<Set<string>>(new Set());
  const [initial, setInitial] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([api.admin.permissionCatalog(), api.admin.getUserPermissions(user.id)])
      .then(([defs, current]) => {
        setCatalog(defs);
        setGranted(new Set(current.permissions));
        setInitial(new Set(current.permissions));
      })
      .catch((err) => setError(errorMessage(err)))
      .finally(() => setLoading(false));
  }, [user.id]);

  const groups = useMemo(() => {
    const byGroup = new Map<string, PermissionDefinition[]>();
    catalog.forEach((p) => byGroup.set(p.group, [...(byGroup.get(p.group) ?? []), p]));
    return [...byGroup.entries()];
  }, [catalog]);

  const dirty = granted.size !== initial.size || [...granted].some((p) => !initial.has(p));

  const toggle = (key: string) =>
    setGranted((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      await api.admin.updateUserPermissions(user.id, [...granted]);
      onClose();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl w-full max-w-2xl max-h-[92vh] flex flex-col shadow-2xl">
        <div className="flex items-start justify-between px-6 py-4 border-b border-slate-100">
          <div className="flex items-start gap-3">
            <KeyRound className="w-5 h-5 text-indigo-600 mt-0.5" />
            <div>
              <h3 className="font-bold text-slate-900">Permissões de {user.name}</h3>
              <p className="text-xs text-slate-500">
                Gestor de RH · {user.email}. Vale na hora, sem a pessoa precisar sair e entrar de novo.
              </p>
            </div>
          </div>
          <button onClick={onClose} aria-label="Fechar" className="p-1 text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-100 cursor-pointer">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="overflow-y-auto px-6 py-4 space-y-5">
          {loading && <p className="text-sm text-slate-500">Carregando...</p>}
          {groups.map(([group, items]) => (
            <div key={group}>
              <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">{group}</p>
              <div className="space-y-2">
                {items.map((p) => (
                  <label key={p.key} className="flex items-start gap-3 p-3 rounded-xl border border-slate-200 hover:bg-slate-50 cursor-pointer">
                    <input type="checkbox" checked={granted.has(p.key)} onChange={() => toggle(p.key)} className="mt-1" />
                    <span className="flex-1">
                      <span className="text-sm font-semibold text-slate-800">{p.label}</span>
                      {!p.available && (
                        <span className="ml-2 text-[10px] font-semibold px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500 border border-slate-200">em breve</span>
                      )}
                      <span className="block text-xs text-slate-500">{p.description}</span>
                    </span>
                  </label>
                ))}
              </div>
            </div>
          ))}
          <p className="text-xs text-slate-500">
            Configurações da organização, criar administradores e conceder permissões continuam só com administradores.
            Toda mudança fica registrada na auditoria.
          </p>
          <FormError message={error} />
        </div>

        <div className="flex items-center justify-between gap-3 px-6 py-4 border-t border-slate-100">
          <span className="text-xs text-slate-500">{granted.size} de {catalog.length} permissões</span>
          <div className="flex gap-2">
            <button onClick={onClose} className="px-4 py-2 rounded-xl text-sm font-semibold text-slate-600 hover:bg-slate-100 cursor-pointer">
              Cancelar
            </button>
            <button
              onClick={save}
              disabled={!dirty || saving}
              className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm transition disabled:opacity-60 cursor-pointer"
            >
              {saving ? 'Salvando...' : 'Salvar permissões'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
