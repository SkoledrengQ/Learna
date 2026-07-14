import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Student, CreateStudentDto, UpdateStudentDto } from '../../../shared/models/student.model';
import { StudentGuardian, CreateGuardianDto, UpdateGuardianDto } from '../../../shared/models/guardian.model';

@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private apiService = inject(ApiService);
  private readonly basePath = 'students';

  getStudents(): Observable<Student[]> {
    return this.apiService.get<Student[]>(this.basePath);
  }

  getStudent(id: number): Observable<Student> {
    return this.apiService.get<Student>(`${this.basePath}/${id}`);
  }

  createStudent(dto: CreateStudentDto): Observable<Student> {
    return this.apiService.post<Student>(this.basePath, dto);
  }

  updateStudent(id: number, dto: UpdateStudentDto): Observable<Student> {
    return this.apiService.put<Student>(`${this.basePath}/${id}`, dto);
  }

  deleteStudent(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }

  getGuardians(studentId: number): Observable<StudentGuardian[]> {
    return this.apiService.get<StudentGuardian[]>(`${this.basePath}/${studentId}/guardians`);
  }

  addGuardian(studentId: number, dto: CreateGuardianDto): Observable<StudentGuardian> {
    return this.apiService.post<StudentGuardian>(`${this.basePath}/${studentId}/guardians`, dto);
  }

  updateGuardian(studentId: number, guardianId: number, dto: UpdateGuardianDto): Observable<StudentGuardian> {
    return this.apiService.put<StudentGuardian>(`${this.basePath}/${studentId}/guardians/${guardianId}`, dto);
  }

  removeGuardian(studentId: number, guardianId: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${studentId}/guardians/${guardianId}`);
  }
}
