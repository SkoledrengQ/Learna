export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'ExcusedAbsence' | 'Sick' | 'ApprovedLeave';

export interface AttendanceRecord {
  status: AttendanceStatus;
  note?: string | null;
  recordedByUserId: number;
  recordedByEmail: string;
  recordedAt: string;
  updatedAt: string;
}

export interface AttendanceRosterStudent {
  studentId: number;
  firstName: string;
  lastName: string;
  nickname?: string | null;
  record?: AttendanceRecord | null;
}

export interface LessonAttendance {
  lessonId: number;
  lessonDate: string;
  canEdit: boolean;
  editDeadline: string;
  students: AttendanceRosterStudent[];
}

export interface AttendanceGroupSummary {
  subjectGroupId: number; subjectGroupName: string; subjectNameEnglish: string; subjectNameThai?: string | null;
  recordedLessons: number; presencePercentage: number; excusedAbsences: number; unexcusedAbsences: number;
}

export interface RecentAttendance {
  lessonId: number; date: string; startTime: string; subjectGroupId: number; subjectGroupName: string;
  subjectNameEnglish: string; subjectNameThai?: string | null; status: AttendanceStatus; note?: string | null;
}

export interface AttendanceSummary {
  recordedLessons: number;
  totals: Record<AttendanceStatus, number>;
  presencePercentage: number;
  excusedAbsences: number;
  unexcusedAbsences: number;
  subjectGroups: AttendanceGroupSummary[];
  recentRecords: RecentAttendance[];
}
