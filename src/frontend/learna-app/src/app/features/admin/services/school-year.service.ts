import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  SchoolYear,
  CreateSchoolYearDto,
  UpdateSchoolYearDto,
  Term,
  CreateTermDto,
  UpdateTermDto
} from '../../../shared/models/school-year.model';

@Injectable({
  providedIn: 'root'
})
export class SchoolYearService {
  private apiService = inject(ApiService);
  private readonly basePath = 'school-years';

  getSchoolYears(): Observable<SchoolYear[]> {
    return this.apiService.get<SchoolYear[]>(this.basePath);
  }

  getSchoolYear(id: number): Observable<SchoolYear> {
    return this.apiService.get<SchoolYear>(`${this.basePath}/${id}`);
  }

  createSchoolYear(dto: CreateSchoolYearDto): Observable<SchoolYear> {
    return this.apiService.post<SchoolYear>(this.basePath, dto);
  }

  updateSchoolYear(id: number, dto: UpdateSchoolYearDto): Observable<SchoolYear> {
    return this.apiService.put<SchoolYear>(`${this.basePath}/${id}`, dto);
  }

  deleteSchoolYear(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }

  getTerms(schoolYearId: number): Observable<Term[]> {
    return this.apiService.get<Term[]>(`${this.basePath}/${schoolYearId}/terms`);
  }

  addTerm(schoolYearId: number, dto: CreateTermDto): Observable<Term> {
    return this.apiService.post<Term>(`${this.basePath}/${schoolYearId}/terms`, dto);
  }

  updateTerm(schoolYearId: number, termId: number, dto: UpdateTermDto): Observable<Term> {
    return this.apiService.put<Term>(`${this.basePath}/${schoolYearId}/terms/${termId}`, dto);
  }

  removeTerm(schoolYearId: number, termId: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${schoolYearId}/terms/${termId}`);
  }
}
