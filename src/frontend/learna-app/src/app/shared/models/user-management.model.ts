export type UserRole = 'Admin' | 'Teacher' | 'Student' | 'Parent';
export type LinkType = 'none' | 'teacher' | 'student' | 'guardian';

export interface ManagedUser {
  id: number;
  email: string;
  roles: string[];
  linkType: LinkType;
  linkedId: number | null;
  linkedName: string | null;
  isActive: boolean;
  lastLoginAt: string | null;
}

export interface LinkablePerson {
  linkType: Exclude<LinkType, 'none'>;
  id: number;
  name: string;
  email: string | null;
}

export interface CreateManagedUser {
  email: string;
  initialPassword: string;
  role: UserRole;
  linkType: LinkType;
  linkedId: number | null;
}

export interface ResetPasswordResponse { temporaryPassword: string; }
