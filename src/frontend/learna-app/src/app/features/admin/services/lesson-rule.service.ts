import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { LessonRule, CreateLessonRuleDto, UpdateLessonRuleDto } from '../../../shared/models/lesson-rule.model';

@Injectable({
  providedIn: 'root'
})
export class LessonRuleService {
  private apiService = inject(ApiService);

  getRules(subjectGroupId: number): Observable<LessonRule[]> {
    return this.apiService.get<LessonRule[]>(`subject-groups/${subjectGroupId}/rules`);
  }

  createRule(subjectGroupId: number, dto: CreateLessonRuleDto): Observable<LessonRule> {
    return this.apiService.post<LessonRule>(`subject-groups/${subjectGroupId}/rules`, dto);
  }

  updateRule(subjectGroupId: number, ruleId: number, dto: UpdateLessonRuleDto): Observable<LessonRule> {
    return this.apiService.put<LessonRule>(`subject-groups/${subjectGroupId}/rules/${ruleId}`, dto);
  }

  deleteRule(subjectGroupId: number, ruleId: number): Observable<void> {
    return this.apiService.delete<void>(`subject-groups/${subjectGroupId}/rules/${ruleId}`);
  }
}
