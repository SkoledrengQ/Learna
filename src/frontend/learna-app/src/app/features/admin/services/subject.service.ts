import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Subject, CreateSubjectDto, UpdateSubjectDto } from '../../../shared/models/subject.model';

@Injectable({
  providedIn: 'root'
})
export class SubjectService {
  private apiService = inject(ApiService);
  private readonly basePath = 'subjects';

  getSubjects(): Observable<Subject[]> {
    return this.apiService.get<Subject[]>(this.basePath);
  }

  getSubject(id: number): Observable<Subject> {
    return this.apiService.get<Subject>(`${this.basePath}/${id}`);
  }

  createSubject(dto: CreateSubjectDto): Observable<Subject> {
    return this.apiService.post<Subject>(this.basePath, dto);
  }

  updateSubject(id: number, dto: UpdateSubjectDto): Observable<Subject> {
    return this.apiService.put<Subject>(`${this.basePath}/${id}`, dto);
  }

  deleteSubject(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }
}
