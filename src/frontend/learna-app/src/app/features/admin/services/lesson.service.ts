import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Lesson, CreateLessonDto, UpdateLessonDto } from '../../../shared/models/lesson.model';

export interface LessonQuery {
  from?: string;
  to?: string;
  subjectGroupId?: number;
  roomId?: number;
  teacherId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiService = inject(ApiService);
  private readonly basePath = 'lessons';

  getLessons(query: LessonQuery = {}): Observable<Lesson[]> {
    const params = new URLSearchParams();
    if (query.from) params.set('from', query.from);
    if (query.to) params.set('to', query.to);
    if (query.subjectGroupId) params.set('subjectGroupId', query.subjectGroupId.toString());
    if (query.roomId) params.set('roomId', query.roomId.toString());
    if (query.teacherId) params.set('teacherId', query.teacherId.toString());
    const qs = params.toString() ? `?${params.toString()}` : '';
    return this.apiService.get<Lesson[]>(`${this.basePath}${qs}`);
  }

  createLesson(dto: CreateLessonDto): Observable<Lesson> {
    return this.apiService.post<Lesson>(this.basePath, dto);
  }

  updateLesson(id: number, dto: UpdateLessonDto): Observable<Lesson> {
    return this.apiService.put<Lesson>(`${this.basePath}/${id}`, dto);
  }

  deleteLesson(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }
}
