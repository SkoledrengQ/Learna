import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Student, CreateStudentDto, UpdateStudentDto } from '../../../shared/models/student.model';

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
}
