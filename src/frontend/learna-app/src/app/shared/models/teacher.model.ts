import { PersonName } from './student.model';

export interface Teacher {
  id: number;
  name: PersonName;
  email: string;
  phoneNumber?: string | null;
  employeeId?: string | null;
}

export interface CreateTeacherDto {
  name: PersonName;
  email: string;
  phoneNumber?: string | null;
  employeeId?: string | null;
}

export interface UpdateTeacherDto {
  name: PersonName;
  email: string;
  phoneNumber?: string | null;
  employeeId?: string | null;
}
