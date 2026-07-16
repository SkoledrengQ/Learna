import { SchoolSettings } from './school-settings.model';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: Date;
  user: User;
  schoolSettings: SchoolSettings;
}

export interface User {
  id: number;
  email: string;
  roles: string[];
  studentId?: number;
  guardianId?: number;
  teacherId?: number;
  isActive: boolean;
  preferredLanguage?: string | null;
}

/** localStorage key for the persisted current user; shared by AuthService and LanguageService. */
export const AUTH_USER_STORAGE_KEY = 'current_user';

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AuthResult {
  success: boolean;
  errorMessage?: string;
}
