export type LessonStatus = 'Scheduled' | 'Cancelled';
export type ConflictType = 'Teacher' | 'Room' | 'Student';

export interface Lesson {
  id: number;
  subjectGroupId: number;
  subjectGroupName: string;
  subjectNameEnglish: string;
  subjectNameThai?: string | null;
  date: string;  // "2026-08-03"
  startTime: string;  // "08:00:00" - school-local wall time, not UTC
  endTime: string;
  roomId?: number | null;
  roomName?: string | null;
  teacherId?: number | null;
  teacherName?: string | null;
  status: LessonStatus;
  note?: string | null;
  sourceRuleId?: number | null;
  isModified: boolean;
  attendanceRegistered: boolean;
  canManageAttendance: boolean;
}

export interface CreateLessonDto {
  subjectGroupId: number;
  date: string;
  startTime: string;
  endTime: string;
  roomId?: number | null;
  teacherId?: number | null;
  note?: string | null;
  force?: boolean;
}

export interface UpdateLessonDto {
  date: string;
  startTime: string;
  endTime: string;
  roomId?: number | null;
  teacherId?: number | null;
  note?: string | null;
  status: LessonStatus;
  force?: boolean;
}

export interface LessonConflict {
  type: ConflictType;
  lessonId: number;
  subjectGroupId: number;
  subjectGroupName: string;
  date: string;
  startTime: string;
  endTime: string;
  detail?: string | null;
}

export interface ConflictResponse {
  conflicts: LessonConflict[];
}
