import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  SubjectGroup,
  CreateSubjectGroupDto,
  UpdateSubjectGroupDto,
  Enrollment,
  EnrollStudentDto,
  EnrollClassResult
} from '../../../shared/models/subject-group.model';

@Injectable({
  providedIn: 'root'
})
export class SubjectGroupService {
  private apiService = inject(ApiService);
  private readonly basePath = 'subject-groups';

  getSubjectGroups(termId?: number, subjectId?: number): Observable<SubjectGroup[]> {
    const params = new URLSearchParams();
    if (termId) params.set('termId', termId.toString());
    if (subjectId) params.set('subjectId', subjectId.toString());
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.apiService.get<SubjectGroup[]>(`${this.basePath}${query}`);
  }

  getSubjectGroup(id: number): Observable<SubjectGroup> {
    return this.apiService.get<SubjectGroup>(`${this.basePath}/${id}`);
  }

  createSubjectGroup(dto: CreateSubjectGroupDto): Observable<SubjectGroup> {
    return this.apiService.post<SubjectGroup>(this.basePath, dto);
  }

  updateSubjectGroup(id: number, dto: UpdateSubjectGroupDto): Observable<SubjectGroup> {
    return this.apiService.put<SubjectGroup>(`${this.basePath}/${id}`, dto);
  }

  deleteSubjectGroup(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }

  getEnrollments(subjectGroupId: number, includeHistorical = false): Observable<Enrollment[]> {
    return this.apiService.get<Enrollment[]>(`${this.basePath}/${subjectGroupId}/enrollments?includeHistorical=${includeHistorical}`);
  }

  addEnrollment(subjectGroupId: number, dto: EnrollStudentDto): Observable<Enrollment> {
    return this.apiService.post<Enrollment>(`${this.basePath}/${subjectGroupId}/enrollments`, dto);
  }

  removeEnrollment(subjectGroupId: number, studentId: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${subjectGroupId}/enrollments/${studentId}`);
  }

  enrollClass(subjectGroupId: number, classId: number): Observable<EnrollClassResult> {
    return this.apiService.post<EnrollClassResult>(`${this.basePath}/${subjectGroupId}/enroll-class/${classId}`, {});
  }
}
