import React, { useState } from 'react';
import type { Post, ReactionType } from '../../types';
import { api } from '../../services/api';
import { useAuth } from '../../context/AuthContext';
import {
  MessageCircle,
  Sparkles,
  Award,
  Pin,
  Send,
  PartyPopper,
  Megaphone
} from 'lucide-react';

interface PostCardProps {
  post: Post;
  onPostUpdated: (updatedPost: Post) => void;
}

const REACTION_CONFIG: { type: ReactionType; emoji: string; label: string }[] = [
  { type: 'Like', emoji: '👍', label: 'Curtir' },
  { type: 'Heart', emoji: '❤️', label: 'Amei' },
  { type: 'Clap', emoji: '👏', label: 'Palmas' },
  { type: 'Rocket', emoji: '🚀', label: 'Foguete' },
  { type: 'Party', emoji: '🎉', label: 'Parabéns' },
  { type: 'Star', emoji: '⭐', label: 'Estrela' },
];

export const PostCard: React.FC<PostCardProps> = ({ post, onPostUpdated }) => {
  const { user } = useAuth();
  const [showComments, setShowComments] = useState(false);
  const [commentText, setCommentText] = useState('');
  const [isCommenting, setIsCommenting] = useState(false);

  const handleToggleReaction = async (type: ReactionType) => {
    try {
      const updated = await api.feed.toggleReaction(post.id, type);
      onPostUpdated(updated);
    } catch (err) {
      console.error('Erro ao reagir:', err);
    }
  };

  const handleAddComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!commentText.trim()) return;

    setIsCommenting(true);
    try {
      const newComment = await api.feed.addComment(post.id, commentText.trim());
      setCommentText('');
      const updatedPost: Post = {
        ...post,
        comments: [...post.comments, newComment],
      };
      onPostUpdated(updatedPost);
    } catch (err) {
      console.error('Erro ao comentar:', err);
    } finally {
      setIsCommenting(false);
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return date.toLocaleDateString('pt-BR', {
        day: '2-digit',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  return (
    <div className={`bg-white rounded-2xl border ${post.isPinned ? 'border-indigo-200 ring-1 ring-indigo-100' : 'border-slate-200'} shadow-xs overflow-hidden mb-4 transition-all`}>
      {/* Pinned Header */}
      {post.isPinned && (
        <div className="bg-indigo-50/70 px-4 py-1.5 flex items-center space-x-1.5 text-xs text-indigo-700 font-semibold border-b border-indigo-100">
          <Pin className="w-3.5 h-3.5 rotate-45" />
          <span>Publicação Fixada pela Gestão</span>
        </div>
      )}

      <div className="p-4 sm:p-5">
        {/* Post Author Header */}
        <div className="flex items-start justify-between mb-3">
          <div className="flex items-center space-x-3">
            <img
              src={post.authorAvatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100'}
              alt={post.authorName}
              className="w-10 h-10 rounded-full object-cover ring-2 ring-slate-100"
            />
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-bold text-slate-800 text-sm">{post.authorName}</span>
                {post.type === 'Announcement' && (
                  <span className="flex items-center space-x-1 px-2 py-0.5 bg-indigo-100 text-indigo-700 rounded-full text-[10px] font-bold">
                    <Megaphone className="w-3 h-3" />
                    <span>Comunicado</span>
                  </span>
                )}
                {post.type === 'Celebration' && (
                  <span className="flex items-center space-x-1 px-2 py-0.5 bg-rose-100 text-rose-700 rounded-full text-[10px] font-bold">
                    <PartyPopper className="w-3 h-3" />
                    <span>Celebração</span>
                  </span>
                )}
                {post.type === 'Recognition' && (
                  <span className="flex items-center space-x-1 px-2 py-0.5 bg-amber-100 text-amber-800 rounded-full text-[10px] font-bold">
                    <Sparkles className="w-3 h-3 text-amber-600" />
                    <span>Reconhecimento</span>
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400">
                {post.authorJobTitle} • {formatDate(post.createdAt)}
              </p>
            </div>
          </div>
        </div>

        {/* Title if present */}
        {post.title && (
          <h3 className="font-bold text-slate-900 text-base mb-2">
            {post.title}
          </h3>
        )}

        {/* Content */}
        <p className="text-slate-700 text-sm leading-relaxed whitespace-pre-wrap mb-4">
          {post.content}
        </p>

        {/* Recognition Card Banner if type is Recognition */}
        {post.recognition && (
          <div className="mb-4 p-4 rounded-xl bg-gradient-to-r from-amber-50 via-orange-50 to-amber-100/60 border border-amber-200">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <img
                  src={post.recognition.receiverAvatarUrl || 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=100'}
                  alt={post.recognition.receiverName}
                  className="w-12 h-12 rounded-full object-cover ring-2 ring-amber-400"
                />
                <div>
                  <p className="text-xs text-amber-800 font-semibold uppercase tracking-wider">
                    Reconhecimento concedido a
                  </p>
                  <p className="font-bold text-slate-900 text-sm">
                    {post.recognition.receiverName}
                  </p>
                  <div className="flex items-center space-x-1.5 mt-0.5 text-xs text-amber-900">
                    <Award className="w-3.5 h-3.5 text-amber-600" />
                    <span className="font-semibold">Valor: {post.recognition.valueTitle}</span>
                  </div>
                </div>
              </div>

              {/* Coins Badge */}
              <div className="text-center bg-white px-3 py-1.5 rounded-xl shadow-xs border border-amber-200">
                <div className="text-xs font-bold text-amber-600">+{post.recognition.coinsAmount}</div>
                <div className="text-[10px] text-slate-500 font-medium">SocialCoins</div>
              </div>
            </div>
          </div>
        )}

        {/* Reactions Summary & Count */}
        <div className="flex items-center justify-between py-2 border-t border-slate-100 text-xs text-slate-500">
          <div className="flex items-center space-x-2">
            {Object.keys(post.reactionsByType).length > 0 && (
              <div className="flex items-center space-x-1">
                {REACTION_CONFIG.filter((r) => post.reactionsByType[r.type] > 0).map((r) => (
                  <span key={r.type} className="text-sm">
                    {r.emoji}
                  </span>
                ))}
                <span className="font-semibold text-slate-700 ml-1">{post.reactionsCount}</span>
              </div>
            )}
          </div>

          <button
            onClick={() => setShowComments(!showComments)}
            className="hover:text-slate-800 cursor-pointer font-medium"
          >
            {post.comments.length} {post.comments.length === 1 ? 'comentário' : 'comentários'}
          </button>
        </div>

        {/* Reaction Buttons */}
        <div className="flex items-center justify-between pt-2 border-t border-slate-100">
          <div className="flex items-center space-x-1 flex-wrap">
            {REACTION_CONFIG.map((r) => {
              const hasReacted = post.currentUserReactions.includes(r.type);
              const count = post.reactionsByType[r.type] || 0;
              return (
                <button
                  key={r.type}
                  onClick={() => handleToggleReaction(r.type)}
                  className={`flex items-center space-x-1 px-2.5 py-1.5 rounded-lg text-xs font-medium transition-all cursor-pointer ${
                    hasReacted
                      ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 font-bold scale-105'
                      : 'hover:bg-slate-100 text-slate-600'
                  }`}
                  title={r.label}
                >
                  <span>{r.emoji}</span>
                  {count > 0 && <span>{count}</span>}
                </button>
              );
            })}
          </div>

          <button
            onClick={() => setShowComments(!showComments)}
            className="flex items-center space-x-1.5 px-3 py-1.5 text-xs text-slate-600 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
          >
            <MessageCircle className="w-4 h-4 text-slate-400" />
            <span>Comentar</span>
          </button>
        </div>

        {/* Comments Section */}
        {showComments && (
          <div className="mt-4 pt-4 border-t border-slate-100 space-y-3">
            {post.comments.map((comment) => (
              <div key={comment.id} className="flex items-start space-x-2.5 text-xs">
                <img
                  src={comment.authorAvatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=50'}
                  alt={comment.authorName}
                  className="w-7 h-7 rounded-full object-cover shrink-0 mt-0.5"
                />
                <div className="flex-1 bg-slate-50 p-2.5 rounded-xl border border-slate-100">
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-semibold text-slate-800">{comment.authorName}</span>
                    <span className="text-[10px] text-slate-400">{formatDate(comment.createdAt)}</span>
                  </div>
                  <p className="text-slate-700 leading-normal">{comment.content}</p>
                </div>
              </div>
            ))}

            {/* Add Comment Input */}
            <form onSubmit={handleAddComment} className="flex items-center space-x-2 pt-2">
              <img
                src={user?.avatarUrl || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=50'}
                alt={user?.name}
                className="w-7 h-7 rounded-full object-cover shrink-0"
              />
              <input
                type="text"
                value={commentText}
                onChange={(e) => setCommentText(e.target.value)}
                placeholder="Escreva um comentário..."
                className="flex-1 px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500"
              />
              <button
                type="submit"
                disabled={isCommenting || !commentText.trim()}
                className="p-1.5 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-40 text-white rounded-lg transition-colors cursor-pointer"
              >
                <Send className="w-3.5 h-3.5" />
              </button>
            </form>
          </div>
        )}
      </div>
    </div>
  );
};
