import React, { useState } from 'react';
import { X } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { api } from '../../services/api';
import { FormError } from '../auth/AuthLayout';
import { MIN_PASSWORD_LENGTH, errorMessage, inputClass, passwordProblem, primaryButtonClass } from '../auth/authForm';

export const ChangePasswordModal: React.FC<{ onClose: () => void }> = ({ onClose }) => {
  const { user, refreshUser } = useAuth();
  const hasPassword = user?.hasPassword ?? true;
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const problem = passwordProblem(newPassword, confirmation);
    if (problem) {
      setError(problem);
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      await api.auth.changePassword(hasPassword ? currentPassword : undefined, newPassword);
      await refreshUser();
      setDone(true);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl max-w-sm w-full p-6 shadow-2xl relative">
        <button onClick={onClose} aria-label="Fechar" className="absolute top-4 right-4 p-1 text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-100 cursor-pointer">
          <X className="w-5 h-5" />
        </button>
        <h3 className="font-bold text-slate-900 text-base">{hasPassword ? 'Alterar senha' : 'Criar senha'}</h3>
        <p className="text-xs text-slate-500 mt-1">
          {hasPassword
            ? 'As suas outras sessões abertas serão encerradas.'
            : 'Hoje você entra com o Google. Criando uma senha, também pode entrar com e-mail e senha.'}
        </p>

        {done ? (
          <div className="mt-5 space-y-4">
            <p className="text-sm text-emerald-800 bg-emerald-50 border border-emerald-200 rounded-xl px-3 py-2">Senha salva.</p>
            <button type="button" onClick={onClose} className={primaryButtonClass}>
              Fechar
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="mt-5 space-y-4">
            {hasPassword && (
              <label className="block">
                <span className="text-xs font-semibold text-slate-700">Senha atual</span>
                <input type="password" required autoComplete="current-password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} className={`${inputClass} mt-1`} />
              </label>
            )}
            <label className="block">
              <span className="text-xs font-semibold text-slate-700">Nova senha (mínimo {MIN_PASSWORD_LENGTH} caracteres)</span>
              <input type="password" required minLength={MIN_PASSWORD_LENGTH} autoComplete="new-password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} className={`${inputClass} mt-1`} />
            </label>
            <label className="block">
              <span className="text-xs font-semibold text-slate-700">Confirme a nova senha</span>
              <input type="password" required autoComplete="new-password" value={confirmation} onChange={(e) => setConfirmation(e.target.value)} className={`${inputClass} mt-1`} />
            </label>
            <FormError message={error} />
            <button type="submit" disabled={submitting} className={primaryButtonClass}>
              {submitting ? 'Salvando...' : 'Salvar senha'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
};
