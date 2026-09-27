import React, { useState, useEffect } from 'react';
import { AuthProvider, useAuth } from './context/AuthContext';
import { LoginPage } from './components/auth/LoginPage';
import { AcceptInvitePage } from './components/auth/AcceptInvitePage';
import { ResetPasswordPage } from './components/auth/ResetPasswordPage';
import { UsersAdmin } from './components/admin/UsersAdmin';
import { Navbar } from './components/layout/Navbar';
import { Sidebar } from './components/layout/Sidebar';
import { PostComposer } from './components/feed/PostComposer';
import { PostCard } from './components/feed/PostCard';
import { MoodThermometerWidget } from './components/widgets/MoodThermometerWidget';
import { CoinsLeaderboardWidget } from './components/widgets/CoinsLeaderboardWidget';
import { CelebrationWidget } from './components/widgets/CelebrationWidget';
import { api } from './services/api';
import type { Post } from './types';
import {
  Sparkles,
  RefreshCw,
  MessageSquare,
  CalendarCheck,
  Users2,
  Target,
  BarChart3,
  Heart,
  ShieldCheck,
  Zap,
  TrendingUp,
  X
} from 'lucide-react';

const MainApp: React.FC = () => {
  const { user } = useAuth();
  const canManageUsers = user?.role === 'Admin' || user?.role === 'HR';
  const [currentTab, setCurrentTab] = useState('feed');
  const [posts, setPosts] = useState<Post[]>([]);
  const [loadingPosts, setLoadingPosts] = useState(true);
  const [isRecognitionModalOpen, setIsRecognitionModalOpen] = useState(false);

  const fetchPosts = async () => {
    try {
      setLoadingPosts(true);
      const data = await api.feed.getPosts();
      setPosts(data);
    } catch (err) {
      console.error('Erro ao buscar publicações:', err);
    } finally {
      setLoadingPosts(false);
    }
  };

  useEffect(() => {
    fetchPosts();
  }, []);

  const handlePostUpdated = (updatedPost: Post) => {
    setPosts((prev) => prev.map((p) => (p.id === updatedPost.id ? updatedPost : p)));
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col text-slate-800">
      <Navbar onOpenRecognitionModal={() => setIsRecognitionModalOpen(true)} />

      <div className="flex-1 max-w-7xl w-full mx-auto flex">
        {/* Left Sidebar */}
        <Sidebar currentTab={currentTab} setCurrentTab={setCurrentTab} />

        {/* Center Main Content */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8 min-w-0">
          {currentTab === 'feed' && (
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
              {/* Central Feed Stream */}
              <div className="lg:col-span-8">
                {/* Post & Praise Composer */}
                <PostComposer onPostCreated={fetchPosts} />

                {/* Feed Header */}
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center space-x-2">
                    <h2 className="font-bold text-slate-900 text-base">Mural de Publicações</h2>
                    <span className="text-xs bg-slate-200 text-slate-700 px-2 py-0.5 rounded-full font-semibold">
                      {posts.length}
                    </span>
                  </div>
                  <button
                    onClick={fetchPosts}
                    disabled={loadingPosts}
                    className="flex items-center space-x-1 text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer"
                  >
                    <RefreshCw className={`w-3.5 h-3.5 ${loadingPosts ? 'animate-spin' : ''}`} />
                    <span>Atualizar</span>
                  </button>
                </div>

                {/* Posts Stream */}
                {loadingPosts && posts.length === 0 ? (
                  <div className="bg-white rounded-2xl border border-slate-200 p-8 text-center text-slate-400">
                    <RefreshCw className="w-6 h-6 mx-auto animate-spin mb-2 text-indigo-500" />
                    <p className="text-sm">Carregando mural corporativo...</p>
                  </div>
                ) : posts.length === 0 ? (
                  <div className="bg-white rounded-2xl border border-slate-200 p-8 text-center text-slate-500">
                    <MessageSquare className="w-10 h-10 mx-auto mb-2 text-slate-300" />
                    <h3 className="font-bold text-slate-700 mb-1">Nenhuma publicação ainda</h3>
                    <p className="text-xs text-slate-400">
                      Seja o primeiro a compartilhar uma conquista ou reconhecer um colega!
                    </p>
                  </div>
                ) : (
                  <div className="space-y-4">
                    {posts.map((post) => (
                      <PostCard
                        key={post.id}
                        post={post}
                        onPostUpdated={handlePostUpdated}
                      />
                    ))}
                  </div>
                )}
              </div>

              {/* Right Sidebar Widgets */}
              <div className="lg:col-span-4 space-y-4">
                <MoodThermometerWidget />
                <CoinsLeaderboardWidget />
                <CelebrationWidget />
              </div>
            </div>
          )}

          {currentTab === 'recognitions' && (
            <div className="space-y-6">
              <div className="bg-gradient-to-r from-amber-500 to-rose-500 rounded-3xl p-6 sm:p-8 text-white shadow-md">
                <div className="max-w-2xl">
                  <div className="flex items-center space-x-2 text-amber-200 text-xs font-bold uppercase tracking-wider mb-2">
                    <Sparkles className="w-4 h-4" />
                    <span>Programa de Gamificação & Cultura</span>
                  </div>
                  <h1 className="text-2xl sm:text-3xl font-extrabold mb-2">
                    Reconheça quem transforma o dia a dia
                  </h1>
                  <p className="text-amber-50 text-sm leading-relaxed mb-4">
                    Todos os meses você recebe 100 SocialCoins para doar e reconhecer colegas que praticam nossos valores fundamentais. As moedas que você recebe podem ser acumuladas!
                  </p>
                  <button
                    onClick={() => setIsRecognitionModalOpen(true)}
                    className="px-5 py-2.5 bg-white text-slate-900 font-bold rounded-xl text-xs sm:text-sm shadow-sm hover:shadow-lg transition cursor-pointer"
                  >
                    Enviar Reconhecimento Agora
                  </button>
                </div>
              </div>

              {/* Valores Corporativos */}
              <div>
                <h3 className="font-bold text-slate-900 text-lg mb-3">Nossos 4 Pilares de Cultura</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="bg-white p-5 rounded-2xl border border-slate-200">
                    <div className="w-10 h-10 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center mb-3">
                      <Users2 className="w-5 h-5" />
                    </div>
                    <h4 className="font-bold text-slate-800 text-sm mb-1">Espírito de Equipe</h4>
                    <p className="text-xs text-slate-500 leading-relaxed">
                      Crescemos juntos com respeito mútuo, apoio irrestrito e celebração coletiva das vitórias.
                    </p>
                  </div>

                  <div className="bg-white p-5 rounded-2xl border border-slate-200">
                    <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center mb-3">
                      <Zap className="w-5 h-5" />
                    </div>
                    <h4 className="font-bold text-slate-800 text-sm mb-1">Inovação & Agilidade</h4>
                    <p className="text-xs text-slate-500 leading-relaxed">
                      Descomplicamos o dia a dia e criamos soluções digitais ágeis com simplicidade.
                    </p>
                  </div>

                  <div className="bg-white p-5 rounded-2xl border border-slate-200">
                    <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center mb-3">
                      <TrendingUp className="w-5 h-5" />
                    </div>
                    <h4 className="font-bold text-slate-800 text-sm mb-1">Foco em Resultados</h4>
                    <p className="text-xs text-slate-500 leading-relaxed">
                      Superamos metas com disciplina, foco na qualidade e alto senso de responsabilidade.
                    </p>
                  </div>

                  <div className="bg-white p-5 rounded-2xl border border-slate-200">
                    <div className="w-10 h-10 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center mb-3">
                      <Heart className="w-5 h-5" />
                    </div>
                    <h4 className="font-bold text-slate-800 text-sm mb-1">Foco nas Pessoas</h4>
                    <p className="text-xs text-slate-500 leading-relaxed">
                      Cuidamos do bem-estar de nossos consultores e valorizamos as pessoas em cada decisão.
                    </p>
                  </div>
                </div>
              </div>

              {/* Leaderboard Completo */}
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <div className="lg:col-span-2">
                  <div className="bg-white rounded-2xl border border-slate-200 p-5">
                    <h3 className="font-bold text-slate-900 text-base mb-4">Feed de Reconhecimentos Recentes</h3>
                    <div className="space-y-4">
                      {posts.filter(p => p.type === 'Recognition').map((p) => (
                        <PostCard key={p.id} post={p} onPostUpdated={handlePostUpdated} />
                      ))}
                    </div>
                  </div>
                </div>
                <div>
                  <CoinsLeaderboardWidget />
                </div>
              </div>
            </div>
          )}

          {currentTab === 'mood' && (
            <div className="max-w-2xl mx-auto space-y-6">
              <MoodThermometerWidget />
              <div className="bg-white rounded-2xl border border-slate-200 p-6">
                <h3 className="font-bold text-slate-900 text-base mb-2">Por que medimos o humor diário?</h3>
                <p className="text-xs text-slate-600 leading-relaxed mb-4">
                  O Termômetro de Humor do SocialTool permite que a liderança e a equipe de Gente & Gestão monitorem tendências de clima, identifiquem momentos de sobrecarga e promovam ações de cuidado com a saúde mental antes que ocorra burnout.
                </p>
                <div className="p-4 bg-indigo-50 rounded-xl border border-indigo-100 flex items-center space-x-3 text-xs text-indigo-900">
                  <ShieldCheck className="w-6 h-6 text-indigo-600 shrink-0" />
                  <span>
                    <strong>Anonimização garantida por design:</strong> Gestores e RH visualizam apenas a média agregada da equipe quando há no mínimo 4 respostas diárias, preservando totalmente a privacidade individual de cada colaborador.
                  </span>
                </div>
              </div>
            </div>
          )}

          {currentTab === 'admin-users' && canManageUsers && <UsersAdmin />}

          {['one-on-one', 'feedback', 'okrs', 'performance', 'org'].includes(currentTab) && (
            <div className="bg-white rounded-3xl border border-slate-200 p-8 sm:p-12 text-center max-w-xl mx-auto my-8">
              <div className="w-16 h-16 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center mx-auto mb-4">
                {currentTab === 'one-on-one' && <CalendarCheck className="w-8 h-8" />}
                {currentTab === 'feedback' && <Users2 className="w-8 h-8" />}
                {currentTab === 'okrs' && <Target className="w-8 h-8" />}
                {currentTab === 'performance' && <BarChart3 className="w-8 h-8" />}
                {currentTab === 'org' && <Users2 className="w-8 h-8" />}
              </div>
              <h2 className="text-xl font-bold text-slate-800 mb-2">
                Módulo em Desenvolvimento (Fases 2 e 3)
              </h2>
              <p className="text-xs sm:text-sm text-slate-500 leading-relaxed mb-6">
                Este módulo faz parte das próximas fases do roadmap. Acompanhe a evolução em docs/06-roadmap-implementacao.md.
              </p>
              <button
                onClick={() => setCurrentTab('feed')}
                className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-xs rounded-xl transition cursor-pointer"
              >
                Voltar ao Mural Social
              </button>
            </div>
          )}
        </main>
      </div>

      {/* Modal de Reconhecimento Rápido */}
      {isRecognitionModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full p-6 shadow-2xl relative animate-in fade-in zoom-in-95 duration-150">
            <button
              onClick={() => setIsRecognitionModalOpen(false)}
              className="absolute top-5 right-5 p-1 text-slate-400 hover:text-slate-600 rounded-full hover:bg-slate-100 cursor-pointer"
            >
              <X className="w-5 h-5" />
            </button>
            <div className="flex items-center space-x-2 text-amber-500 mb-2">
              <Sparkles className="w-5 h-5" />
              <h3 className="font-bold text-slate-900 text-base">Novo Reconhecimento</h3>
            </div>
            <PostComposer
              initialMode="recognition"
              onPostCreated={() => {
                fetchPosts();
                setIsRecognitionModalOpen(false);
              }}
            />
          </div>
        </div>
      )}
    </div>
  );
};

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
}

// Sem roteador: as únicas rotas são os links que chegam por e-mail. O resto da navegação é por abas.
const AppRoutes: React.FC = () => {
  const { status } = useAuth();
  const [path, setPath] = useState(window.location.pathname);
  const token = new URLSearchParams(window.location.search).get('token') ?? '';

  const goHome = () => {
    window.history.replaceState(null, '', '/');
    setPath('/');
  };

  if (path === '/convite') return <AcceptInvitePage token={token} onDone={goHome} />;
  if (path === '/redefinir-senha') return <ResetPasswordPage token={token} onDone={goHome} />;

  if (status === 'loading') {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center text-slate-400">
        <RefreshCw className="w-6 h-6 animate-spin text-indigo-500" />
      </div>
    );
  }

  return status === 'authenticated' ? <MainApp /> : <LoginPage />;
};
