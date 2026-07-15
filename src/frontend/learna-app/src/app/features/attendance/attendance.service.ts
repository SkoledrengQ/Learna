import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AttendanceStatus, AttendanceSummary, LessonAttendance } from '../../shared/models/attendance.model';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private http = inject(HttpClient);
  getLesson(id: number): Observable<LessonAttendance> { return this.http.get<LessonAttendance>(`${environment.apiUrl}/lessons/${id}/attendance`); }
  saveLesson(id: number, records: { studentId: number; status: AttendanceStatus; note?: string | null }[]): Observable<LessonAttendance> {
    return this.http.put<LessonAttendance>(`${environment.apiUrl}/lessons/${id}/attendance`, { records });
  }
  getMine(): Observable<AttendanceSummary> { return this.http.get<AttendanceSummary>(`${environment.apiUrl}/attendance/me`); }
  getStudent(studentId: number): Observable<AttendanceSummary> { return this.http.get<AttendanceSummary>(`${environment.apiUrl}/students/${studentId}/attendance/summary`); }
  getGuardianChild(studentId: number): Observable<AttendanceSummary> { return this.http.get<AttendanceSummary>(`${environment.apiUrl}/guardians/me/children/${studentId}/attendance`); }
}
