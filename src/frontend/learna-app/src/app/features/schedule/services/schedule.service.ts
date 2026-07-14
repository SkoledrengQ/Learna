import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Lesson } from '../../../shared/models/lesson.model';

@Injectable({
  providedIn: 'root'
})
export class ScheduleService {
  private apiService = inject(ApiService);
  private readonly basePath = 'schedule';

  getMySchedule(from: string, to: string): Observable<Lesson[]> {
    return this.apiService.get<Lesson[]>(`${this.basePath}/me${this.query(from, to)}`);
  }

  getStudentSchedule(studentId: number, from: string, to: string): Observable<Lesson[]> {
    return this.apiService.get<Lesson[]>(`${this.basePath}/students/${studentId}${this.query(from, to)}`);
  }

  getTeacherSchedule(teacherId: number, from: string, to: string): Observable<Lesson[]> {
    return this.apiService.get<Lesson[]>(`${this.basePath}/teachers/${teacherId}${this.query(from, to)}`);
  }

  getRoomSchedule(roomId: number, from: string, to: string): Observable<Lesson[]> {
    return this.apiService.get<Lesson[]>(`${this.basePath}/rooms/${roomId}${this.query(from, to)}`);
  }

  private query(from: string, to: string): string {
    const params = new URLSearchParams({ from, to });
    return `?${params.toString()}`;
  }
}
