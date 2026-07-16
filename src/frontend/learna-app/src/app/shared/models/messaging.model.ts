export type ConversationType = 'Direct' | 'Group';

export interface ConversationSummary {
  id: number;
  type: ConversationType;
  displayTitle: string;
  participantNames: string[];
  lastMessagePreview?: string | null;
  lastMessageAt?: string | null;
  unreadCount: number;
  isCreator: boolean;
}

export interface ConversationList {
  conversations: ConversationSummary[];
  totalUnread: number;
}

export interface ConversationParticipant {
  userId: number;
  name: string;
  roles: string[];
  joinedAt: string;
  leftAt?: string | null;
  isCreator: boolean;
}

export interface ConversationDetail {
  id: number;
  type: ConversationType;
  displayTitle: string;
  createdByUserId: number;
  isCreator: boolean;
  participants: ConversationParticipant[];
}

export interface ConversationMessage {
  id: number;
  senderUserId: number;
  senderName: string;
  body: string;
  sentAt: string;
  isMine: boolean;
}

export interface MessagePage {
  messages: ConversationMessage[];
  hasMore: boolean;
}

export interface CreateConversationRequest {
  type: ConversationType;
  userId?: number | null;
  title?: string | null;
  userIds?: number[] | null;
}

export interface ConversationError {
  code: string;
}

export interface DirectoryPerson {
  userId: number;
  name: string;
  roles: string[];
}

export interface AdminConversationSummary {
  id: number;
  type: ConversationType;
  displayTitle: string;
  participantNames: string[];
  messageCount: number;
  lastMessageAt?: string | null;
  createdAt: string;
}

export interface MessagingPolicyRule {
  roleA: string;
  roleB: string;
  allowed: boolean;
}

export interface MessagingPolicy {
  rules: MessagingPolicyRule[];
}
