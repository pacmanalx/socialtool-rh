import React, { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { api } from '../../services/api';
import { AuthLayout, FormError } from './AuthLayout';
import { MIN_PASSWORD_LENGTH, errorMessage, inputClass, passwordProblem, primaryButtonClass } from './authForm';

interface ResetPasswordPageProps {
  token: string;
  onDone: () => void;
}

export const ResetPasswordPage: React.FC<ResetPasswordPageProps> = ({ token, onDone }) => {
  const { applySession } = useAuth();
  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(
    token ? null : 'Link incompleto. Abra o link direto do e-mail ou peça uma nova redefinição.',
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const problem = passwordProblem(password, confirmation);
    if (problem) {
      setError(problem);
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      applySession(await api.auth.resetPassword(token, password));
      onDone();
    } catch (err) {
      setError(errorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <AuthLayout title="Definir nova senha" subtitle="Ao salvar, as suas outras sessões abertas são encerradas.">
      <form onSubmit={handleSubmit} className="space-y-4">
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Nova senha (mínimo {MIN_PASSWORD_LENGTH} caracteres)</span>
          <input type="password" required minLength={MIN_PASSWORD_LENGTH} autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} className={`${inputClass} mt-1`} />
        </label>
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Confirme a nova senha</span>
          <input type="password" required autoComplete="new-password" value={confirmation} onChange={(e) => setConfirmation(e.target.value)} className={`${inputClass} mt-1`} />
        </label>
        <FormError message={error} />
        <button type="submit" disabled={submitting || !token} className={primaryButtonClass}>
          {submitting ? 'Salvando...' : 'Salvar e entrar'}
        </button>
        <button type="button" onClick={onDone} className="w-full text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer">
          Voltar para o login
        </button>
      </form>
    </AuthLayout>
  );
};
