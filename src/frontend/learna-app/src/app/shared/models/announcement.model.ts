import { FileResource } from './file-resource.model';

export type AnnouncementAudienceType = 'School' | 'SchoolClass' | 'SubjectGroup';

export interface AnnouncementFile {
  id: number;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  description?: string | null;
  createdAt: string;
}

export interface Announcement {
  id: number;
  title: string;
  body: string;
  createdByUserId: number;
  authorName: string;
  audienceType: AnnouncementAudienceType;
  targetId?: number | null;
  audienceLabel: string;
  publishAt: string;
  expiresAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  isRead: boolean;
  isFuture: boolean;
  isExpired: boolean;
  canEdit: boolean;
  files: AnnouncementFile[];
}

export interface AnnouncementFeed {
  announcements: Announcement[];
  unreadCount: number;
}

export interface AnnouncementWrite {
  title: string;
  body: string;
  audienceType: AnnouncementAudienceType;
  targetId?: number | null;
  publishAt?: string | null;
  expiresAt?: string | null;
}

export interface AnnouncementTarget {
  audienceType: AnnouncementAudienceType;
  targetId?: number | null;
  label: string;
}

export interface AnnouncementTargets {
  targets: AnnouncementTarget[];
}

export type AnnouncementDownload = Pick<FileResource, 'id' | 'originalFileName'>;
