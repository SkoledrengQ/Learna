import { StudentSummary } from './student.model';

export interface SchoolClass {
  id: number;
  schoolYearId: number;
  name: string;
  description?: string | null;
  homeroomTeacherId?: number | null;
}

export interface CreateSchoolClassDto {
  schoolYearId: number;
  name: string;
  description?: string | null;
  homeroomTeacherId?: number | null;
}

export interface UpdateSchoolClassDto {
  name: string;
  description?: string | null;
  homeroomTeacherId?: number | null;
}

export interface ClassMembership {
  id: number;
  student: StudentSummary;
  joinedDate: Date;
  leftDate?: Date | null;
}

export interface AddClassMemberDto {
  studentId: number;
  joinedDate?: Date | null;
}

export interface MoveClassMemberDto {
  toClassId: number;
  moveDate?: Date | null;
}
