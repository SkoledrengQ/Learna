export const DAYS_OF_WEEK = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const;
export type DayOfWeekName = typeof DAYS_OF_WEEK[number];

export interface LessonRule {
  id: number;
  subjectGroupId: number;
  dayOfWeek: DayOfWeekName;
  startTime: string;  // "08:00:00" - school-local wall time, not UTC
  endTime: string;
  roomId?: number | null;
  roomName?: string | null;
  startDate: string;  // "2026-08-03"
  endDate: string;
}

export interface CreateLessonRuleDto {
  dayOfWeek: DayOfWeekName;
  startTime: string;
  endTime: string;
  roomId?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  force?: boolean;
}

export interface UpdateLessonRuleDto {
  dayOfWeek: DayOfWeekName;
  startTime: string;
  endTime: string;
  roomId?: number | null;
  startDate: string;
  endDate: string;
  force?: boolean;
}
