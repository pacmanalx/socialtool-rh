import React, { useEffect, useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { api } from '../../services/api';
import type { InvitationInfo } from '../../types';
import { AuthLayout, FormError } from './AuthLayout';
import { MIN_PASSWORD_LENGTH, errorMessage, inputClass, passwordProblem, primaryButtonClass } from './authForm';
import { GoogleSignInButton } from './GoogleSignInButton';

interface AcceptInvitePageProps {
  token: string;
  onDone: () => void;
}

export const AcceptInvitePage: React.FC<AcceptInvitePageProps> = ({ token, onDone }) => {
  const { applySession, loginWithGoogle, googleClientId } = useAuth();
  const [invitation, setInvitation] = useState<InvitationInfo | null>(null);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const loadError = token ? fetchError : 'Link de convite incompleto. Abra o link direto do e-mail.';

  useEffect(() => {
    if (!token) return;
    api.auth.getInvitation(token).then(setInvitation).catch((err) => setFetchError(errorMessage(err)));
  }, [token]);

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
      applySession(await api.auth.acceptInvitation(token, password));
      onDone();
    } catch (err) {
      setError(errorMessage(err));
      setSubmitting(false);
    }
  };

  const handleGoogle = async (credential: string) => {
    setError(null);
    try {
      await loginWithGoogle(credential);
      onDone();
    } catch (err) {
      setError(errorMessage(err));
    }
  };

  if (loadError) {
    return (
      <AuthLayout title="Convite indisponível">
        <FormError message={loadError} />
        <button type="button" onClick={onDone} className={`${primaryButtonClass} mt-4`}>
          Ir para o login
        </button>
      </AuthLayout>
    );
  }

  if (!invitation) {
    return <AuthLayout title="Carregando convite...">{null}</AuthLayout>;
  }

  return (
    <AuthLayout
      title={`Bem-vindo(a), ${invitation.name.split(' ')[0]}!`}
      subtitle={`Crie a sua senha para entrar no SocialTool RH da ${invitation.organizationName}.`}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">E-mail</span>
          <input type="email" value={invitation.email} disabled autoComplete="username" className={`${inputClass} mt-1`} />
        </label>
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Senha (mínimo {MIN_PASSWORD_LENGTH} caracteres)</span>
          <input type="password" required minLength={MIN_PASSWORD_LENGTH} autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} className={`${inputClass} mt-1`} />
        </label>
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Confirme a senha</span>
          <input type="password" required autoComplete="new-password" value={confirmation} onChange={(e) => setConfirmation(e.target.value)} className={`${inputClass} mt-1`} />
        </label>
        <FormError message={error} />
        <button type="submit" disabled={submitting} className={primaryButtonClass}>
          {submitting ? 'Criando acesso...' : 'Criar senha e entrar'}
        </button>
      </form>

      {googleClientId && (
        <>
          <div className="flex items-center my-5 text-xs text-slate-400">
            <div className="flex-1 h-px bg-slate-200" />
            <span className="px-3">ou entre com a conta Google da empresa</span>
            <div className="flex-1 h-px bg-slate-200" />
          </div>
          <GoogleSignInButton clientId={googleClientId} onCredential={handleGoogle} onError={setError} />
        </>
      )}
    </AuthLayout>
  );
};
