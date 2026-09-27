import React, { useState, useEffect } from 'react';
import { useAuth } from '../../context/AuthContext';
import { api } from '../../services/api';
import type { CompanyValue, UserSummary } from '../../types';
import { Sparkles, Send, Coins, MessageSquare, AlertCircle, CheckCircle2 } from 'lucide-react';

interface PostComposerProps {
  onPostCreated: () => void;
  initialMode?: 'post' | 'recognition';
}

export const PostComposer: React.FC<PostComposerProps> = ({ onPostCreated, initialMode = 'post' }) => {
  const { user, refreshUser } = useAuth();
  const canAnnounce = user?.role === 'HR' || user?.role === 'Admin';
  const [mode, setMode] = useState<'post' | 'recognition'>(initialMode);

  // Normal Post State
  const [content, setContent] = useState('');
  const [title, setTitle] = useState('');
  const [isAnnouncement, setIsAnnouncement] = useState(false);

  // Recognition State
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [values, setValues] = useState<CompanyValue[]>([]);
  const [selectedUserId, setSelectedUserId] = useState<string>('');
  const [selectedValueId, setSelectedValueId] = useState<string>('');
  const [coinsAmount, setCoinsAmount] = useState<number>(20);
  const [recognitionMessage, setRecognitionMessage] = useState('');

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    setMode(initialMode);
  }, [initialMode]);

  useEffect(() => {
    // Carrega colegas e valores da empresa
    api.users.getAll().then(setUsers).catch(console.error);
    api.recognition.getValues().then(setValues).catch(console.error);
  }, []);

  const handleCreateNormalPost = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;

    setIsSubmitting(true);
    setErrorMessage(null);
    try {
      await api.feed.createPost({
        content: content.trim(),
        title: isAnnouncement && title.trim() ? title.trim() : undefined,
        type: isAnnouncement ? 'Announcement' : 'General',
      });
      setContent('');
      setTitle('');
      setIsAnnouncement(false);
      setSuccessMessage('Publicação enviada com sucesso!');
      setTimeout(() => setSuccessMessage(null), 3000);
      onPostCreated();
    } catch (err: any) {
      setErrorMessage(err.message || 'Erro ao publicar.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSendRecognition = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedUserId) {
      setErrorMessage('Selecione o colega que deseja reconhecer.');
      return;
    }
    if (!selectedValueId) {
      setErrorMessage('Selecione um valor da empresa.');
      return;
    }
    if (!recognitionMessage.trim()) {
      setErrorMessage('Escreva uma mensagem de reconhecimento.');
      return;
    }
    if ((user?.coinsAvailableToGive ?? 0) < coinsAmount) {
      setErrorMessage('Você não possui saldo de moedas suficiente para doar.');
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);
    try {
      await api.recognition.send({
        receiverId: selectedUserId,
        companyValueId: selectedValueId,
        coinsAmount,
        message: recognitionMessage.trim(),
      });

      setSelectedUserId('');
      setSelectedValueId('');
      setRecognitionMessage('');
      setCoinsAmount(20);
      setSuccessMessage('Reconhecimento enviado e compartilhado no mural!');
      setTimeout(() => setSuccessMessage(null), 3000);
      await refreshUser();
      onPostCreated();
    } catch (err: any) {
      setErrorMessage(err.message || 'Erro ao enviar reconhecimento.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const colleagues = users.filter((u) => u.id !== user?.id);

  return (
    <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden mb-6">
      {/* Tabs */}
      <div className="flex border-b border-slate-100 bg-slate-50/50">
        <button
          type="button"
          onClick={() => {
            setMode('post');
            setErrorMessage(null);
          }}
          className={`flex-1 py-3 px-4 text-xs sm:text-sm font-semibold flex items-center justify-center space-x-2 transition-colors cursor-pointer ${
            mode === 'post'
              ? 'bg-white text-indigo-600 border-b-2 border-indigo-600'
              : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          <MessageSquare className="w-4 h-4" />
          <span>Escrever no Mural</span>
        </button>

        <button
          type="button"
          onClick={() => {
            setMode('recognition');
            setErrorMessage(null);
          }}
          className={`flex-1 py-3 px-4 text-xs sm:text-sm font-semibold flex items-center justify-center space-x-2 transition-colors cursor-pointer ${
            mode === 'recognition'
              ? 'bg-white text-amber-600 border-b-2 border-amber-500'
              : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          <Sparkles className="w-4 h-4 text-amber-500" />
          <span>Elogiar com SocialCoins</span>
        </button>
      </div>

      <div className="p-4 sm:p-5">
        {errorMessage && (
          <div className="mb-4 p-3 bg-rose-50 text-rose-700 rounded-xl text-xs flex items-center space-x-2 border border-rose-200">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {successMessage && (
          <div className="mb-4 p-3 bg-emerald-50 text-emerald-700 rounded-xl text-xs flex items-center space-x-2 border border-emerald-200">
            <CheckCircle2 className="w-4 h-4 shrink-0" />
            <span>{successMessage}</span>
          </div>
        )}

        {mode === 'post' ? (
          <form onSubmit={handleCreateNormalPost} className="space-y-3">
            <div className="flex space-x-3">
              <img
                src={user?.avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100'}
                alt={user?.name}
                className="w-10 h-10 rounded-full object-cover shrink-0"
              />
              <div className="flex-1 space-y-2">
                {isAnnouncement && (
                  <input
                    type="text"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    placeholder="Título do anúncio..."
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 font-semibold"
                  />
                )}
                <textarea
                  rows={3}
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder={`No que você está pensando, ${user?.name?.split(' ')[0]}? Compartilhe com a equipe...`}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 resize-none"
                />
              </div>
            </div>

            <div className="flex items-center justify-between pt-2 border-t border-slate-100">
              {canAnnounce ? (
                <label className="flex items-center space-x-2 text-xs text-slate-600 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={isAnnouncement}
                    onChange={(e) => setIsAnnouncement(e.target.checked)}
                    className="rounded text-indigo-600 focus:ring-indigo-500"
                  />
                  <span>Comunicado oficial / Anúncio</span>
                </label>
              ) : (
                <span />
              )}

              <button
                type="submit"
                disabled={isSubmitting || !content.trim()}
                className="flex items-center space-x-1.5 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-xs font-semibold rounded-xl transition-all cursor-pointer shadow-xs"
              >
                <Send className="w-3.5 h-3.5" />
                <span>{isSubmitting ? 'Publicando...' : 'Publicar'}</span>
              </button>
            </div>
          </form>
        ) : (
          /* Recognition Mode */
          <form onSubmit={handleSendRecognition} className="space-y-4">
            {/* Destinatário */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                Quem você quer reconhecer hoje?
              </label>
              <select
                value={selectedUserId}
                onChange={(e) => setSelectedUserId(e.target.value)}
                className="w-full px-3 py-2 border border-slate-200 rounded-xl text-xs sm:text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 bg-white"
              >
                <option value="">Selecione um colega de equipe...</option>
                {colleagues.map((colleague) => (
                  <option key={colleague.id} value={colleague.id}>
                    {colleague.name} — {colleague.jobTitle} ({colleague.departmentName || 'Geral'})
                  </option>
                ))}
              </select>
            </div>

            {/* Valores da Empresa */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                Qual valor da empresa foi praticado?
              </label>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                {values.map((val) => {
                  const isSelected = selectedValueId === val.id;
                  return (
                    <button
                      key={val.id}
                      type="button"
                      onClick={() => setSelectedValueId(val.id)}
                      className={`p-2.5 rounded-xl border text-left transition-all cursor-pointer ${
                        isSelected
                          ? 'border-amber-500 bg-amber-50/70 text-amber-900 shadow-xs'
                          : 'border-slate-200 hover:border-slate-300 text-slate-700'
                      }`}
                    >
                      <div className="font-semibold text-xs">{val.title}</div>
                      <div className="text-[10px] text-slate-500 line-clamp-1 mt-0.5">{val.description}</div>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Quantidade de Moedas */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="text-xs font-semibold text-slate-700">
                  Quantidade de SocialCoins a doar:
                </label>
                <div className="text-xs text-amber-700 font-medium">
                  Disponível para doar: <strong>{user?.coinsAvailableToGive ?? 0} moedas</strong>
                </div>
              </div>
              <div className="flex items-center space-x-2">
                {[10, 20, 30, 50].map((amount) => (
                  <button
                    key={amount}
                    type="button"
                    onClick={() => setCoinsAmount(amount)}
                    className={`flex-1 py-1.5 px-3 rounded-lg border text-xs font-bold transition-all cursor-pointer ${
                      coinsAmount === amount
                        ? 'bg-amber-500 text-white border-amber-600 shadow-xs'
                        : 'border-slate-200 hover:bg-slate-50 text-slate-700'
                    }`}
                  >
                    +{amount}
                  </button>
                ))}
              </div>
            </div>

            {/* Mensagem de Reconhecimento */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                Mensagem do reconhecimento (visível a todos no mural):
              </label>
              <textarea
                rows={2}
                value={recognitionMessage}
                onChange={(e) => setRecognitionMessage(e.target.value)}
                placeholder="Ex: Mandou muito bem na apresentação para diretoria! Muito obrigado pela ajuda e parceria..."
                className="w-full px-3 py-2 border border-slate-200 rounded-xl text-xs sm:text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 resize-none"
              />
            </div>

            <div className="flex items-center justify-end pt-2 border-t border-slate-100">
              <button
                type="submit"
                disabled={isSubmitting}
                className="flex items-center space-x-2 px-5 py-2.5 bg-gradient-to-r from-amber-500 to-rose-500 hover:from-amber-600 hover:to-rose-600 disabled:opacity-50 text-white text-xs sm:text-sm font-semibold rounded-xl transition-all cursor-pointer shadow-sm hover:shadow"
              >
                <Coins className="w-4 h-4" />
                <span>{isSubmitting ? 'Enviando...' : `Enviar Elogio com ${coinsAmount} Moedas`}</span>
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};
