import type {
  AdminUser,
  BulkInviteResult,
  EmailStatus,
  CompanyValue,
  DailyMood,
  DepartmentOption,
  InvitationInfo,
  LeaderboardItem,
  Post,
  PostComment,
  PostType,
  ReactionType,
  Session,
  OrganizationSettings,
  User,
  UserImportOptions,
  UserImportPreview,
  UserImportResult,
  UserRole,
  UserSummary,
} from '../types';

const API_BASE = '/api';

// O access token vive só em memória; a sessão persiste no cookie httpOnly do refresh token,
// que o JavaScript não lê — um script injetado na página não consegue levar a sessão embora.
let accessToken: string | null = null;
let refreshInFlight: Promise<Session | null> | null = null;
let sessionHandlers: { onRefreshed?: (session: Session) => void; onExpired?: () => void } = {};

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function setSessionHandlers(handlers: typeof sessionHandlers) {
  sessionHandlers = handlers;
}

// Uma renovação por vez: requisições que tomam 401 juntas esperam a mesma promessa.
export function refreshSession(): Promise<Session | null> {
  if (!refreshInFlight) {
    refreshInFlight = fetch(`${API_BASE}/auth/refresh`, { method: 'POST', credentials: 'same-origin' })
      .then(async (response) => (response.ok ? ((await response.json()) as Session) : null))
      .catch(() => null)
      .then((session) => {
        accessToken = session?.accessToken ?? null;
        if (session) sessionHandlers.onRefreshed?.(session);
        return session;
      })
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
}

interface RequestOptions extends RequestInit {
  // Endpoints de login/convite/senha: um 401 ali é resposta de negócio, não sessão vencida.
  skipRefresh?: boolean;
}

async function request<T>(endpoint: string, options: RequestOptions = {}, isRetry = false): Promise<T> {
  const { skipRefresh, ...init } = options;
  // Com FormData o navegador define o Content-Type (multipart com boundary) sozinho.
  const headers: Record<string, string> = {
    ...(init.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
    ...(init.headers as Record<string, string>),
  };
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  const response = await fetch(`${API_BASE}${endpoint}`, { ...init, headers, credentials: 'same-origin' });

  if (response.status === 401 && !skipRefresh && !isRetry) {
    const session = await refreshSession();
    if (session) {
      return request<T>(endpoint, options, true);
    }
    sessionHandlers.onExpired?.();
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new ApiError(body?.message || defaultErrorMessage(response.status), response.status);
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

function defaultErrorMessage(status: number): string {
  if (status === 401) return 'Sua sessão expirou. Entre novamente.';
  if (status === 403) return 'Você não tem permissão para esta ação.';
  if (status === 429) return 'Muitas tentativas. Aguarde um minuto e tente de novo.';
  return 'Não foi possível concluir a operação. Tente novamente.';
}

const post = (body?: unknown): RequestOptions => ({
  method: 'POST',
  body: body === undefined ? undefined : JSON.stringify(body),
});

function importForm(file: File, options: UserImportOptions): FormData {
  const form = new FormData();
  form.append('file', file);
  form.append('includeNeverSignedIn', String(options.includeNeverSignedIn));
  form.append('deactivateSuspended', String(options.deactivateSuspended));
  form.append('createMissingDepartments', String(options.createMissingDepartments));
  options.excludedEmails.forEach((email) => form.append('excludedEmails', email));
  return form;
}

export const api = {
  auth: {
    getConfig: () =>
      request<{ googleClientId: string | null; passwordResetAvailable: boolean }>('/auth/config', { skipRefresh: true }),
    login: (email: string, password: string) =>
      request<Session>('/auth/login', { ...post({ email, password }), skipRefresh: true }),
    loginWithGoogle: (credential: string) =>
      request<Session>('/auth/google', { ...post({ credential }), skipRefresh: true }),
    logout: () => request<void>('/auth/logout', { ...post(), skipRefresh: true }),
    getInvitation: (token: string) =>
      request<InvitationInfo>(`/auth/invitations/${encodeURIComponent(token)}`, { skipRefresh: true }),
    acceptInvitation: (token: string, password: string) =>
      request<Session>('/auth/invitations/accept', { ...post({ token, password }), skipRefresh: true }),
    forgotPassword: (email: string) =>
      request<{ message: string }>('/auth/password/forgot', { ...post({ email }), skipRefresh: true }),
    resetPassword: (token: string, password: string) =>
      request<Session>('/auth/password/reset', { ...post({ token, password }), skipRefresh: true }),
    changePassword: (currentPassword: string | undefined, newPassword: string) =>
      request<void>('/auth/password/change', post({ currentPassword, newPassword })),
    getMe: () => request<User>('/auth/me'),
  },

  admin: {
    listUsers: () => request<AdminUser[]>('/admin/users'),
    listDepartments: () => request<DepartmentOption[]>('/admin/users/departments'),
    inviteUser: (data: { name: string; email: string; jobTitle: string; role: UserRole; departmentId?: string }) =>
      request<AdminUser>('/admin/users', post(data)),
    updateUser: (id: string, data: { name: string; jobTitle: string; role: UserRole; departmentId?: string }) =>
      request<AdminUser>(`/admin/users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    getEmailStatus: () => request<EmailStatus>('/admin/users/email-status'),
    sendInvitations: (userIds: string[]) => request<BulkInviteResult>('/admin/users/invitations', post({ userIds })),
    resendInvite: (id: string) => request<void>(`/admin/users/${id}/resend-invite`, post()),
    deactivateUser: (id: string) => request<AdminUser>(`/admin/users/${id}/deactivate`, post()),
    reactivateUser: (id: string) => request<AdminUser>(`/admin/users/${id}/reactivate`, post()),
    previewUserImport: (file: File, options: UserImportOptions) =>
      request<UserImportPreview>('/admin/users/import/preview', { method: 'POST', body: importForm(file, options) }),
    applyUserImport: (file: File, options: UserImportOptions) =>
      request<UserImportResult>('/admin/users/import', { method: 'POST', body: importForm(file, options) }),
    getOrganizationSettings: () => request<OrganizationSettings>('/admin/organization'),
    updateOrganizationSettings: (data: {
      name: string;
      currencyName: string;
      monthlyCoinsQuota: number;
      googleWorkspaceDomains: string[];
    }) => request<OrganizationSettings>('/admin/organization', { method: 'PUT', body: JSON.stringify(data) }),
  },

  feed: {
    getPosts: (page = 1, pageSize = 20) => request<Post[]>(`/feed/posts?page=${page}&pageSize=${pageSize}`),
    createPost: (data: { title?: string; content: string; imageUrl?: string; type?: PostType }) =>
      request<Post>('/feed/posts', post(data)),
    toggleReaction: (postId: string, type: ReactionType) =>
      request<Post>(`/feed/posts/${postId}/react`, post({ type })),
    addComment: (postId: string, content: string) =>
      request<PostComment>(`/feed/posts/${postId}/comments`, post({ content })),
  },

  recognition: {
    getValues: () => request<CompanyValue[]>('/recognition/values'),
    send: (data: { receiverId: string; companyValueId: string; coinsAmount: number; message: string }) =>
      request<Post>('/recognition', post(data)),
    getLeaderboard: () => request<LeaderboardItem[]>('/recognition/leaderboard'),
  },

  users: {
    getAll: () => request<UserSummary[]>('/users'),
    submitMood: (score: number, note?: string) =>
      request<{ message: string }>('/users/mood', post({ score, note })),
    getTodayMood: () => request<DailyMood | null>('/users/mood/today'),
  },
};
