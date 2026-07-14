import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  SchoolClass,
  CreateSchoolClassDto,
  UpdateSchoolClassDto,
  ClassMembership,
  AddClassMemberDto,
  MoveClassMemberDto
} from '../../../shared/models/school-class.model';

@Injectable({
  providedIn: 'root'
})
export class ClassService {
  private apiService = inject(ApiService);
  private readonly basePath = 'classes';

  getClasses(schoolYearId?: number): Observable<SchoolClass[]> {
    const query = schoolYearId ? `?schoolYearId=${schoolYearId}` : '';
    return this.apiService.get<SchoolClass[]>(`${this.basePath}${query}`);
  }

  getClass(id: number): Observable<SchoolClass> {
    return this.apiService.get<SchoolClass>(`${this.basePath}/${id}`);
  }

  createClass(dto: CreateSchoolClassDto): Observable<SchoolClass> {
    return this.apiService.post<SchoolClass>(this.basePath, dto);
  }

  updateClass(id: number, dto: UpdateSchoolClassDto): Observable<SchoolClass> {
    return this.apiService.put<SchoolClass>(`${this.basePath}/${id}`, dto);
  }

  deleteClass(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }

  getMembers(classId: number, includeHistorical = false): Observable<ClassMembership[]> {
    return this.apiService.get<ClassMembership[]>(`${this.basePath}/${classId}/members?includeHistorical=${includeHistorical}`);
  }

  addMember(classId: number, dto: AddClassMemberDto): Observable<ClassMembership> {
    return this.apiService.post<ClassMembership>(`${this.basePath}/${classId}/members`, dto);
  }

  moveMember(classId: number, studentId: number, dto: MoveClassMemberDto): Observable<ClassMembership> {
    return this.apiService.post<ClassMembership>(`${this.basePath}/${classId}/members/${studentId}/move`, dto);
  }

  removeMember(classId: number, studentId: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${classId}/members/${studentId}`);
  }
}
