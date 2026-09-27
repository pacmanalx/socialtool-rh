import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Building2, ChevronRight, Pencil, Plus, Trash2, UserMinus, UserPlus, X } from 'lucide-react';
import { api } from '../../services/api';
import type { AdminUser, Area, AreaKind, AreaMember } from '../../types';
import { AREA_KIND_LABELS } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage, inputClass } from '../auth/authForm';

const KIND_STYLES: Record<AreaKind, string> = {
  Unit: 'bg-indigo-50 text-indigo-700 border-indigo-200',
  Department: 'bg-sky-50 text-sky-700 border-sky-200',
  Sector: 'bg-slate-50 text-slate-600 border-slate-200',
};

// Tipo sugerido para uma área nova, conforme o que está acima dela.
const childKind = (parent?: Area): AreaKind => (!parent ? 'Unit' : parent.kind === 'Unit' ? 'Department' : 'Sector');

type AreaForm = { id?: string; name: string; kind: AreaKind; parentId: string };

// Unidades, departamentos e setores em árvore, e quem está em cada área (base do público das enquetes).
export const StructureAdmin: React.FC = () => {
  const [areas, setAreas] = useState<Area[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [form, setForm] = useState<AreaForm | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      setAreas(await api.areas.list());
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const byParent = useMemo(() => {
    const map = new Map<string | undefined, Area[]>();
    areas.forEach((a) => map.set(a.parentId, [...(map.get(a.parentId) ?? []), a]));
    map.forEach((list) => list.sort((x, y) => x.name.localeCompare(y.name)));
    return map;
  }, [areas]);

  const selected = areas.find((a) => a.id === selectedId);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setSaving(true);
    setError(null);
    try {
      const data = { name: form.name.trim(), kind: form.kind, parentId: form.parentId || undefined };
      const saved = form.id ? await api.areas.update(form.id, data) : await api.areas.create(data);
      setForm(null);
      await load();
      setSelectedId(saved.id);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (area: Area) => {
    if (!window.confirm(`Apagar a área "${area.path}"?`)) return;
    try {
      await api.areas.remove(area.id);
      if (selectedId === area.id) setSelectedId(null);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    }
  };

  const renderNode = (area: Area, depth: number): React.ReactNode => (
    <div key={area.id}>
      <div
        onClick={() => setSelectedId(area.id)}
        className={`group flex items-center gap-2 pr-2 py-1.5 rounded-lg cursor-pointer ${selectedId === area.id ? 'bg-indigo-50' : 'hover:bg-slate-50'}`}
        style={{ paddingLeft: 8 + depth * 18 }}
      >
        <ChevronRight className={`w-3.5 h-3.5 text-slate-300 ${byParent.get(area.id)?.length ? '' : 'invisible'}`} />
        <span className="text-sm font-medium text-slate-800 flex-1 truncate">{area.name}</span>
        <span className={`text-[10px] font-semibold px-1.5 py-0.5 rounded-full border ${KIND_STYLES[area.kind]}`}>{AREA_KIND_LABELS[area.kind]}</span>
        <span className="text-[11px] text-slate-400 w-10 text-right" title="Pessoas nesta área e abaixo dela">{area.totalMembers}</span>
      </div>
      {byParent.get(area.id)?.map((child) => renderNode(child, depth + 1))}
    </div>
  );

  return (
    <div className="space-y-6 max-w-6xl">
      <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <Building2 className="w-5 h-5 text-indigo-600" /> Estrutura
          </h2>
          <p className="text-sm text-slate-500">Unidades, departamentos e setores, e quem está em cada área. É a base do público das enquetes.</p>
        </div>
        <button
          onClick={() => setForm({ name: '', kind: 'Unit', parentId: '' })}
          className="flex items-center gap-2 px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm cursor-pointer"
        >
          <Plus className="w-4 h-4" /> Nova unidade
        </button>
      </div>

      <FormError message={error} />

      {form && (
        <form onSubmit={save} className="bg-white rounded-2xl border border-slate-200 p-5 space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sm font-semibold text-slate-800">{form.id ? 'Editar área' : 'Nova área'}</span>
            <button type="button" onClick={() => setForm(null)} aria-label="Fechar" className="p-1 text-slate-400 hover:text-slate-600 cursor-pointer">
              <X className="w-4 h-4" />
            </button>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <input required autoFocus placeholder="Nome" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className={inputClass} />
            <select value={form.kind} onChange={(e) => setForm({ ...form, kind: e.target.value as AreaKind })} className={inputClass} aria-label="Tipo">
              {(Object.keys(AREA_KIND_LABELS) as AreaKind[]).map((k) => (
                <option key={k} value={k}>{AREA_KIND_LABELS[k]}</option>
              ))}
            </select>
            <select value={form.parentId} onChange={(e) => setForm({ ...form, parentId: e.target.value })} className={inputClass} aria-label="Fica dentro de">
              <option value="">No topo da estrutura</option>
              {areas.filter((a) => a.id !== form.id).map((a) => (
                <option key={a.id} value={a.id}>Dentro de {a.path}</option>
              ))}
            </select>
          </div>
          <button type="submit" disabled={saving} className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm disabled:opacity-60 cursor-pointer">
            {saving ? 'Salvando...' : 'Salvar'}
          </button>
        </form>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
        <div className="lg:col-span-2 bg-white rounded-2xl border border-slate-200 p-3">
          {areas.length === 0 ? (
            <p className="text-sm text-slate-500 p-3">Nenhuma área ainda. Comece criando uma unidade.</p>
          ) : (
            byParent.get(undefined)?.map((a) => renderNode(a, 0))
          )}
        </div>

        <div className="lg:col-span-3">
          {selected ? (
            <AreaPanel
              key={selected.id}
              area={selected}
              onEdit={() => setForm({ id: selected.id, name: selected.name, kind: selected.kind, parentId: selected.parentId ?? '' })}
              onAddChild={() => setForm({ name: '', kind: childKind(selected), parentId: selected.id })}
              onDelete={() => remove(selected)}
              onChanged={load}
            />
          ) : (
            <div className="bg-white rounded-2xl border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500">
              Selecione uma área para ver e ajustar quem está nela.
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

const AreaPanel: React.FC<{
  area: Area;
  onEdit: () => void;
  onAddChild: () => void;
  onDelete: () => void;
  onChanged: () => void;
}> = ({ area, onEdit, onAddChild, onDelete, onChanged }) => {
  const [members, setMembers] = useState<AreaMember[]>([]);
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setMembers(await api.areas.members(area.id));
    } catch (err) {
      setError(errorMessage(err));
    }
  }, [area.id]);

  useEffect(() => {
    load();
  }, [load]);

  const removeMember = async (m: AreaMember) => {
    try {
      await api.areas.removeMember(area.id, m.id);
      await load();
      onChanged();
    } catch (err) {
      setError(errorMessage(err));
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200">
      <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3 px-5 py-4 border-b border-slate-100">
        <div>
          <p className="text-xs text-slate-500">{AREA_KIND_LABELS[area.kind]}</p>
          <h3 className="font-bold text-slate-900">{area.path}</h3>
          <p className="text-xs text-slate-500 mt-0.5">
            {area.memberCount} nesta área · {area.totalMembers} contando as áreas abaixo
          </p>
        </div>
        <div className="flex items-center gap-3 text-xs font-semibold">
          <button onClick={onAddChild} className="flex items-center gap-1 text-indigo-600 hover:text-indigo-800 cursor-pointer"><Plus className="w-3.5 h-3.5" /> Área abaixo</button>
          <button onClick={onEdit} className="flex items-center gap-1 text-slate-600 hover:text-slate-800 cursor-pointer"><Pencil className="w-3.5 h-3.5" /> Editar</button>
          <button onClick={onDelete} className="flex items-center gap-1 text-rose-600 hover:text-rose-800 cursor-pointer"><Trash2 className="w-3.5 h-3.5" /> Apagar</button>
        </div>
      </div>

      <div className="px-5 py-3 flex items-center justify-between">
        <span className="text-sm font-semibold text-slate-800">Pessoas nesta área</span>
        <button onClick={() => setAdding(true)} className="flex items-center gap-1 text-xs font-semibold text-indigo-600 hover:text-indigo-800 cursor-pointer">
          <UserPlus className="w-3.5 h-3.5" /> Colocar pessoas
        </button>
      </div>
      <FormError message={error} />
      <ul className="divide-y divide-slate-100">
        {members.map((m) => (
          <li key={m.id} className={`flex items-center justify-between px-5 py-2.5 ${m.isActive ? '' : 'text-slate-400'}`}>
            <div>
              <div className="text-sm font-medium">{m.name}{!m.isActive && ' (desativado)'}</div>
              <div className="text-xs text-slate-500">{[m.email, m.jobTitle].filter(Boolean).join(' · ')}</div>
            </div>
            <button onClick={() => removeMember(m)} className="flex items-center gap-1 text-xs font-semibold text-slate-500 hover:text-rose-600 cursor-pointer">
              <UserMinus className="w-3.5 h-3.5" /> Tirar
            </button>
          </li>
        ))}
        {members.length === 0 && <li className="px-5 py-4 text-sm text-slate-500">Ninguém diretamente nesta área.</li>}
      </ul>

      {adding && (
        <AddMembersModal
          area={area}
          onClose={() => setAdding(false)}
          onAdded={async () => {
            setAdding(false);
            await load();
            onChanged();
          }}
        />
      )}
    </div>
  );
};

// Escolher pessoas (de qualquer área ou sem área) para colocar nesta área.
const AddMembersModal: React.FC<{ area: Area; onClose: () => void; onAdded: () => void }> = ({ area, onClose, onAdded }) => {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [search, setSearch] = useState('');
  const [onlyWithoutArea, setOnlyWithoutArea] = useState(true);
  const [picked, setPicked] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.admin.listUsers().then(setUsers).catch((err) => setError(errorMessage(err)));
  }, []);

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    return users.filter(
      (u) =>
        u.departmentId !== area.id &&
        u.status !== 'Inactive' &&
        (!onlyWithoutArea || !u.departmentId) &&
        (!term || u.name.toLowerCase().includes(term) || u.email.includes(term) || u.jobTitle.toLowerCase().includes(term)),
    );
  }, [users, search, onlyWithoutArea, area.id]);

  const toggle = (id: string) =>
    setPicked((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const add = async () => {
    setSaving(true);
    setError(null);
    try {
      await api.areas.addMembers(area.id, [...picked]);
      onAdded();
    } catch (err) {
      setError(errorMessage(err));
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl w-full max-w-2xl max-h-[88vh] flex flex-col shadow-2xl">
        <div className="flex items-start justify-between px-6 py-4 border-b border-slate-100">
          <div>
            <h3 className="font-bold text-slate-900">Colocar pessoas em {area.name}</h3>
            <p className="text-xs text-slate-500">Quem já estiver em outra área sai de lá.</p>
          </div>
          <button onClick={onClose} aria-label="Fechar" className="p-1 text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-100 cursor-pointer">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="px-6 py-3 flex flex-col sm:flex-row gap-3 sm:items-center border-b border-slate-100">
          <input autoFocus placeholder="Buscar nome, e-mail, cargo..." value={search} onChange={(e) => setSearch(e.target.value)} className={`${inputClass} sm:flex-1`} />
          <label className="flex items-center gap-2 text-xs text-slate-600">
            <input type="checkbox" checked={onlyWithoutArea} onChange={(e) => setOnlyWithoutArea(e.target.checked)} />
            Só quem está sem área
          </label>
        </div>
        <ul className="overflow-y-auto divide-y divide-slate-100 flex-1">
          {visible.slice(0, 300).map((u) => (
            <li key={u.id}>
              <label className="flex items-center gap-3 px-6 py-2 hover:bg-slate-50 cursor-pointer">
                <input type="checkbox" checked={picked.has(u.id)} onChange={() => toggle(u.id)} />
                <span className="flex-1">
                  <span className="text-sm font-medium text-slate-800">{u.name}</span>
                  <span className="block text-xs text-slate-500">{[u.email, u.jobTitle, u.departmentName].filter(Boolean).join(' · ')}</span>
                </span>
              </label>
            </li>
          ))}
          {visible.length === 0 && <li className="px-6 py-4 text-sm text-slate-500">Ninguém encontrado.</li>}
          {visible.length > 300 && <li className="px-6 py-3 text-xs text-slate-500">Mostrando 300 de {visible.length}. Refine a busca.</li>}
        </ul>
        <FormError message={error} />
        <div className="flex items-center justify-between px-6 py-4 border-t border-slate-100">
          <span className="text-xs text-slate-500">{picked.size} selecionada(s)</span>
          <div className="flex gap-2">
            <button onClick={onClose} className="px-4 py-2 rounded-xl text-sm font-semibold text-slate-600 hover:bg-slate-100 cursor-pointer">Cancelar</button>
            <button onClick={add} disabled={picked.size === 0 || saving} className="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm disabled:opacity-60 cursor-pointer">
              {saving ? 'Salvando...' : 'Colocar na área'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
