import React from 'react';

interface AuthLayoutProps {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({ title, subtitle, children }) => (
  <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
    <div className="w-full max-w-sm">
      <div className="flex items-center justify-center space-x-3 mb-6">
        <div className="w-11 h-11 rounded-xl bg-gradient-to-tr from-amber-500 via-rose-500 to-indigo-600 flex items-center justify-center text-white font-bold text-xl shadow-md">
          S
        </div>
        <div className="flex items-center space-x-2">
          <span className="font-bold text-slate-800 text-xl tracking-tight">SocialTool</span>
          <span className="text-xs bg-indigo-100 text-indigo-700 font-semibold px-2 py-0.5 rounded-full">RH</span>
        </div>
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm p-6">
        <h1 className="text-lg font-bold text-slate-900">{title}</h1>
        {subtitle && <p className="text-sm text-slate-500 mt-1">{subtitle}</p>}
        <div className="mt-5">{children}</div>
      </div>
    </div>
  </div>
);

export const FormError: React.FC<{ message: string | null }> = ({ message }) =>
  message ? (
    <p role="alert" className="text-sm text-rose-700 bg-rose-50 border border-rose-200 rounded-xl px-3 py-2">
      {message}
    </p>
  ) : null;
