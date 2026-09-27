export type UserRole = 'Admin' | 'HR' | 'Leader' | 'Employee';

export type PostType = 'General' | 'Recognition' | 'Celebration' | 'Announcement';

export type ReactionType = 'Like' | 'Heart' | 'Clap' | 'Rocket' | 'Star' | 'Party';

export interface User {
  id: string;
  name: string;
  email: string;
  jobTitle: string;
  role: UserRole;
  avatarUrl?: string;
  coinsAvailableToGive: number;
  coinsBalanceToSpend: number;
  departmentName?: string;
  hasPassword: boolean;
  // Permissões administrativas efetivas (Admin recebe todas).
  permissions: string[];
}

export interface Organization {
  name: string;
  currencyName: string;
  monthlyCoinsQuota: number;
}

export interface Session {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: User;
  organization: Organization;
}

export interface InvitationInfo {
  name: string;
  email: string;
  organizationName: string;
}

export type AdminUserStatus = 'Pending' | 'Invited' | 'Active' | 'Inactive';

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  jobTitle: string;
  role: UserRole;
  departmentId?: string;
  departmentName?: string;
  status: AdminUserStatus;
  lastLoginAt?: string;
  createdAt: string;
}

export type UserImportAction = 'Create' | 'Update' | 'Deactivate' | 'Unchanged' | 'Skip';

export interface UserImportOptions {
  includeNeverSignedIn: boolean;
  deactivateSuspended: boolean;
  createMissingDepartments: boolean;
  excludedEmails: string[];
}

export interface UserImportRow {
  email: string;
  name: string;
  jobTitle?: string;
  department?: string;
  action: UserImportAction;
  reason?: string;
  changes: string[];
  suspendedInWorkspace: boolean;
  neverSignedInGoogle: boolean;
  localStatus?: 'Pending' | 'Active' | 'Inactive';
}

export interface UserImportPreview {
  totalInFile: number;
  toCreate: number;
  toUpdate: number;
  toDeactivate: number;
  unchanged: number;
  skipped: number;
  localNotInFile: number;
  departmentsToCreate: string[];
  unmappedDepartments: string[];
  rows: UserImportRow[];
}

export interface UserImportResult {
  created: number;
  updated: number;
  deactivated: number;
  skipped: number;
  departmentsCreated: number;
}

export interface EmailStatus {
  enabled: boolean;
  redirectTo: string | null;
}

export interface BulkInviteResult {
  sent: number;
  skipped: number;
  blocked: number;
  failed: number;
  failedEmails: string[];
}

export interface DepartmentOption {
  id: string;
  name: string;
}

export interface OrganizationSettings {
  name: string;
  currencyName: string;
  monthlyCoinsQuota: number;
  googleWorkspaceDomains: string[];
  googleLoginConfigured: boolean;
}

export const ROLE_LABELS: Record<UserRole, string> = {
  Admin: 'Administrador',
  HR: 'RH',
  Leader: 'Líder',
  Employee: 'Colaborador',
};

export interface RecognitionSummary {
  id: string;
  senderId: string;
  senderName: string;
  receiverId: string;
  receiverName: string;
  receiverAvatarUrl?: string;
  valueTitle: string;
  valueIcon: string;
  coinsAmount: number;
}

export interface PostComment {
  id: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl?: string;
  content: string;
  createdAt: string;
}

export interface Post {
  id: string;
  authorId: string;
  authorName: string;
  authorJobTitle: string;
  authorAvatarUrl?: string;
  type: PostType;
  title?: string;
  content: string;
  imageUrl?: string;
  isPinned: boolean;
  createdAt: string;
  reactionsCount: number;
  reactionsByType: Record<string, number>;
  currentUserReactions: string[];
  recognition?: RecognitionSummary;
  comments: PostComment[];
}

export interface CompanyValue {
  id: string;
  title: string;
  description: string;
  icon: string;
}

export interface LeaderboardItem {
  userId: string;
  name: string;
  jobTitle: string;
  avatarUrl?: string;
  coinsReceived: number;
  recognitionsCount: number;
}

export interface UserSummary {
  id: string;
  name: string;
  email: string;
  jobTitle: string;
  avatarUrl?: string;
  departmentName?: string;
}

export interface DailyMood {
  score: number;
  note?: string;
  date: string;
}

export interface PermissionDefinition {
  key: string;
  group: string;
  label: string;
  description: string;
  // false: já pode ser concedida, mas o recurso ainda não existe na plataforma.
  available: boolean;
}

export interface UserPermissions {
  userId: string;
  role: UserRole;
  editable: boolean;
  permissions: string[];
}

export interface AuditLogEntry {
  id: string;
  createdAt: string;
  actorId?: string;
  actorName: string;
  action: string;
  targetUserId?: string;
  targetName?: string;
  summary: string;
  reason?: string;
}

export interface AuditPage {
  items: AuditLogEntry[];
  total: number;
  page: number;
  pageSize: number;
}
