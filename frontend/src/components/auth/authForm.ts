export const inputClass =
  'w-full px-3 py-2.5 rounded-xl border border-slate-300 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500 disabled:bg-slate-50';

export const primaryButtonClass =
  'w-full py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm transition disabled:opacity-60 cursor-pointer disabled:cursor-not-allowed';

export const MIN_PASSWORD_LENGTH = 10;

export function passwordProblem(password: string, confirmation: string): string | null {
  if (password.length < MIN_PASSWORD_LENGTH) return `A senha precisa ter pelo menos ${MIN_PASSWORD_LENGTH} caracteres.`;
  if (password !== confirmation) return 'As senhas não conferem.';
  return null;
}

export function errorMessage(err: unknown): string {
  return err instanceof Error ? err.message : 'Não foi possível concluir a operação. Tente novamente.';
}
