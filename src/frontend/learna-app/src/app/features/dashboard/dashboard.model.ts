import { Lesson } from '../../shared/models/lesson.model';

export interface TeacherRecentSubmission {
  submissionId: number;
  studentId: number;
  studentName: string;
  subjectGroupId: number;
  subjectGroupName: string;
  assignmentTitle: string;
  submittedAt: string;
  isLate: boolean;
}

export interface TeacherDashboard {
  todayLessons: Lesson[];
  missingAttendanceCount: number;
  missingAttendanceLessons: Lesson[];
  awaitingGradingCount: number;
  recentSubmissions: TeacherRecentSubmission[];
}

export interface AdminDashboard {
  studentCount: number;
  teacherCount: number;
  classCount: number;
  subjectGroupCount: number;
  todayLessonCount: number;
  missingAttendanceCount: number;
}
