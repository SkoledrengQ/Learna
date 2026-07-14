import { StudentSummary } from './student.model';

export interface SubjectGroup {
  id: number;
  subjectId: number;
  termId: number;
  name: string;
  teacherId?: number | null;
}

export interface CreateSubjectGroupDto {
  subjectId: number;
  termId: number;
  name: string;
  teacherId?: number | null;
}

export interface UpdateSubjectGroupDto {
  name: string;
  teacherId?: number | null;
}

export interface Enrollment {
  id: number;
  student: StudentSummary;
  enrolledDate: Date;
  unenrolledDate?: Date | null;
}

export interface EnrollStudentDto {
  studentId: number;
  enrolledDate?: Date | null;
}

export interface EnrollClassResult {
  enrolled: number;
  skipped: number;
}
