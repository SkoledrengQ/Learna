export interface Student {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  studentId: string;
  idCardNumber: string;
  dateOfBirth: Date;
  parentPhoneNumber: string;
  enrollmentDate: Date;
  gradeLevel: number;
}

export interface CreateStudentDto {
  firstName: string;
  lastName: string;
  email: string;
  idCardNumber: string;
  dateOfBirth: Date;
  parentPhoneNumber: string;
  enrollmentDate: Date;
  gradeLevel: number;
}

export interface UpdateStudentDto {
  firstName: string;
  lastName: string;
  email: string;
  idCardNumber: string;
  dateOfBirth: Date;
  parentPhoneNumber: string;
  gradeLevel: number;
}
