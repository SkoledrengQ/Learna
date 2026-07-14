import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Teacher, CreateTeacherDto, UpdateTeacherDto } from '../../../shared/models/teacher.model';

@Injectable({
  providedIn: 'root'
})
export class TeacherService {
  private apiService = inject(ApiService);
  private readonly basePath = 'teachers';

  getTeachers(): Observable<Teacher[]> {
    return this.apiService.get<Teacher[]>(this.basePath);
  }

  getTeacher(id: number): Observable<Teacher> {
    return this.apiService.get<Teacher>(`${this.basePath}/${id}`);
  }

  createTeacher(dto: CreateTeacherDto): Observable<Teacher> {
    return this.apiService.post<Teacher>(this.basePath, dto);
  }

  updateTeacher(id: number, dto: UpdateTeacherDto): Observable<Teacher> {
    return this.apiService.put<Teacher>(`${this.basePath}/${id}`, dto);
  }

  deleteTeacher(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }
}
