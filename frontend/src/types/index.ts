export type UserRole = 'Admin' | 'HR' | 'Leader' | 'Employee';

export type PostType = 'General' | 'Recognition' | 'Celebration' | 'Announcement';

export type ReactionType = 'Like' | 'Heart' | 'Clap' | 'Rocket' | 'Star' | 'Party';

export interface User {
  id: string;
  tenantId: string;
  name: string;
  email: string;
  jobTitle: string;
  role: UserRole;
  avatarUrl?: string;
  coinsAvailableToGive: number;
  coinsBalanceToSpend: number;
  departmentName?: string;
}

export interface Tenant {
  id: string;
  name: string;
  subdomain: string;
  currencyName: string;
  monthlyCoinsQuota: number;
}

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
