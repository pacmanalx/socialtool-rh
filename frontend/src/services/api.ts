import type { CompanyValue, DailyMood, LeaderboardItem, Post, PostType, ReactionType, User, UserSummary } from '../types';

const API_BASE = '/api';

export function getAuthToken(): string | null {
  return localStorage.getItem('socialtool_token');
}

export function setAuthToken(token: string | null) {
  if (token) {
    localStorage.setItem('socialtool_token', token);
  } else {
    localStorage.removeItem('socialtool_token');
  }
}

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const token = getAuthToken();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-Tenant-Id': 'demo',
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(errorBody.message || 'Erro na requisição');
  }

  return response.json();
}

export const api = {
  auth: {
    login: async (email: string, password = '123456') => {
      const data = await request<{ token: string; user: User; tenant: any }>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });
      setAuthToken(data.token);
      return data;
    },
    getMe: () => request<User>('/auth/me'),
    getDemoUsers: () => request<any[]>('/auth/demo-users'),
  },

  feed: {
    getPosts: (page = 1, pageSize = 20) => request<Post[]>(`/feed/posts?page=${page}&pageSize=${pageSize}`),
    createPost: (data: { title?: string; content: string; imageUrl?: string; type?: PostType }) =>
      request<Post>('/feed/posts', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    toggleReaction: (postId: string, type: ReactionType) =>
      request<Post>(`/feed/posts/${postId}/react`, {
        method: 'POST',
        body: JSON.stringify({ type }),
      }),
    addComment: (postId: string, content: string) =>
      request<any>(`/feed/posts/${postId}/comments`, {
        method: 'POST',
        body: JSON.stringify({ content }),
      }),
  },

  recognition: {
    getValues: () => request<CompanyValue[]>('/recognition/values'),
    send: (data: { receiverId: string; companyValueId: string; coinsAmount: number; message: string }) =>
      request<Post>('/recognition', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    getLeaderboard: () => request<LeaderboardItem[]>('/recognition/leaderboard'),
  },

  users: {
    getAll: () => request<UserSummary[]>('/users'),
    submitMood: (score: number, note?: string) =>
      request<{ message: string }>('/users/mood', {
        method: 'POST',
        body: JSON.stringify({ score, note }),
      }),
    getTodayMood: () => request<DailyMood | null>('/users/mood/today'),
  },
};
