import React, { useState } from 'react';
import { ShieldOff, ShieldCheck } from 'lucide-react';
import type { AdminUser } from '../../types';
import { FormError } from '../auth/AuthLayout';
import { errorMessage, inputClass } from '../auth/authForm';

interface AccessChangeModalProps {
  user: AdminUser;
  onCancel: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

// Revogar ou liberar acesso, com motivo opcional que vai para a auditoria.
export const AccessChangeModal: React.FC<AccessChangeModalProps> = ({ user, onCancel, onConfirm }) => {
  const revoking = user.status !== 'Inactive';
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const confirm = async (e: React.FormEvent) => {
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

  const Icon = revoking ? ShieldOff : ShieldCheck;

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <form onSubmit={confirm} className="bg-white rounded-2xl w-full max-w-md shadow-2xl p-6 space-y-4">
        <div className="flex items-start gap-3">
          <Icon className={`w-5 h-5 mt-0.5 ${revoking ? 'text-rose-600' : 'text-emerald-600'}`} />
          <div>
            <h3 className="font-bold text-slate-900">{revoking ? 'Revogar acesso' : 'Liberar acesso'} de {user.name}</h3>
            <p className="text-xs text-slate-500">
              {revoking
                ? 'A pessoa perde o acesso na hora e é desconectada de todos os dispositivos.'
                : 'A pessoa volta a poder entrar na plataforma.'}
            </p>
          </div>
        </div>
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Motivo (opcional, fica na auditoria)</span>
          <textarea
            rows={3}
            maxLength={500}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={revoking ? 'Ex.: desligamento em 30/09' : 'Ex.: retorno de licença'}
            className={`${inputClass} mt-1`}
          />
        </label>
        <FormError message={error} />
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onCancel} className="px-4 py-2 rounded-xl text-sm font-semibold text-slate-600 hover:bg-slate-100 cursor-pointer">
            Cancelar
          </button>
          <button
            type="submit"
            disabled={saving}
            className={`px-4 py-2 rounded-xl text-white font-semibold text-sm transition disabled:opacity-60 cursor-pointer ${revoking ? 'bg-rose-600 hover:bg-rose-700' : 'bg-emerald-600 hover:bg-emerald-700'}`}
          >
            {saving ? 'Salvando...' : revoking ? 'Revogar acesso' : 'Liberar acesso'}
          </button>
        </div>
      </form>
    </div>
  );
};
