import React, { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { api } from '../../services/api';
import { AuthLayout, FormError } from './AuthLayout';
import { errorMessage, inputClass, primaryButtonClass } from './authForm';
import { GoogleSignInButton } from './GoogleSignInButton';

export const LoginPage: React.FC = () => {
  const { login, loginWithGoogle, googleClientId } = useAuth();
  const [mode, setMode] = useState<'login' | 'forgot'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email.trim(), password);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const handleForgot = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const response = await api.auth.forgotPassword(email.trim());
      setNotice(response.message);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const handleGoogle = async (credential: string) => {
    setError(null);
    try {
      await loginWithGoogle(credential);
    } catch (err) {
      setError(errorMessage(err));
    }
  };

  if (mode === 'forgot') {
    return (
      <AuthLayout title="Esqueci minha senha" subtitle="Informe o seu e-mail e enviaremos um link para definir uma nova senha.">
        {notice ? (
          <div className="space-y-4">
            <p className="text-sm text-emerald-800 bg-emerald-50 border border-emerald-200 rounded-xl px-3 py-2">{notice}</p>
            <button type="button" onClick={() => { setMode('login'); setNotice(null); }} className={primaryButtonClass}>
              Voltar para o login
            </button>
          </div>
        ) : (
          <form onSubmit={handleForgot} className="space-y-4">
            <label className="block">
              <span className="text-xs font-semibold text-slate-700">E-mail</span>
              <input type="email" required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} className={`${inputClass} mt-1`} />
            </label>
            <FormError message={error} />
            <button type="submit" disabled={submitting} className={primaryButtonClass}>
              {submitting ? 'Enviando...' : 'Enviar link'}
            </button>
            <button type="button" onClick={() => { setMode('login'); setError(null); }} className="w-full text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer">
              Voltar para o login
            </button>
          </form>
        )}
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Entrar" subtitle="Use a conta que você recebeu por convite.">
      <form onSubmit={handleLogin} className="space-y-4">
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">E-mail</span>
          <input type="email" required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} className={`${inputClass} mt-1`} />
        </label>
        <label className="block">
          <span className="text-xs font-semibold text-slate-700">Senha</span>
          <input type="password" required autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} className={`${inputClass} mt-1`} />
        </label>
        <FormError message={error} />
        <button type="submit" disabled={submitting} className={primaryButtonClass}>
          {submitting ? 'Entrando...' : 'Entrar'}
        </button>
        <button type="button" onClick={() => { setMode('forgot'); setError(null); }} className="w-full text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer">
          Esqueci minha senha
        </button>
      </form>

      {googleClientId && (
        <>
          <div className="flex items-center my-5 text-xs text-slate-400">
            <div className="flex-1 h-px bg-slate-200" />
            <span className="px-3">ou</span>
            <div className="flex-1 h-px bg-slate-200" />
          </div>
          <GoogleSignInButton clientId={googleClientId} onCredential={handleGoogle} onError={setError} />
        </>
      )}
    </AuthLayout>
  );
};
