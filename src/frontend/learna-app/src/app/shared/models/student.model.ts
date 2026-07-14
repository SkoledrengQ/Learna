export interface PersonName {
  title?: string | null;
  firstName: string;
  lastName: string;
  firstNameEnglish?: string | null;
  lastNameEnglish?: string | null;
  nickname?: string | null;
}

export function getDisplayName(name: PersonName): string {
  return `${name.firstName} ${name.lastName}`;
}

export interface Student {
  id: number;
  name: PersonName;
  email: string;
  studentId: string;
  idCardNumber: string;
  dateOfBirth: Date;
  enrollmentDate: Date;
  phoneNumber: string;
  address: string;
  height?: number | null;
  weight?: number | null;
}

export interface CreateStudentDto {
  name: PersonName;
  email: string;
  idCardNumber: string;
  dateOfBirth: Date;
  enrollmentDate: Date;
  phoneNumber: string;
  address: string;
  height?: number | null;
  weight?: number | null;
}

export interface UpdateStudentDto {
  name: PersonName;
  email: string;
  idCardNumber: string;
  dateOfBirth: Date;
  phoneNumber: string;
  address: string;
  height?: number | null;
  weight?: number | null;
}
