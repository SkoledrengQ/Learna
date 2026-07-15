import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Enrollment } from '../../shared/models/subject-group.model';
import { Grade, GradeGroup, GradeWrite, ManualGradeWrite } from '../../shared/models/grade.model';

@Injectable({providedIn:'root'})
export class GradesService {
  private readonly http=inject(HttpClient);private readonly api=environment.apiUrl;
  forGroup(id:number):Observable<Grade[]>{return this.http.get<Grade[]>(`${this.api}/subject-groups/${id}/grades`)}
  roster(id:number):Observable<Enrollment[]>{return this.http.get<Enrollment[]>(`${this.api}/subject-groups/${id}/enrollments`)}
  gradeSubmission(id:number,data:GradeWrite):Observable<Grade>{return this.http.put<Grade>(`${this.api}/submissions/${id}/grade`,data)}
  createManual(groupId:number,data:ManualGradeWrite):Observable<Grade>{return this.http.post<Grade>(`${this.api}/subject-groups/${groupId}/grades`,data)}
  update(id:number,data:GradeWrite):Observable<Grade>{return this.http.put<Grade>(`${this.api}/grades/${id}`,data)}
  publish(id:number):Observable<Grade>{return this.http.post<Grade>(`${this.api}/grades/${id}/publish`,{})}
  publishAssignment(id:number):Observable<Grade[]>{return this.http.post<Grade[]>(`${this.api}/assignments/${id}/grades/publish`,{})}
  mine():Observable<GradeGroup[]>{return this.http.get<GradeGroup[]>(`${this.api}/grades/my`)}
  guardian(studentId:number):Observable<GradeGroup[]>{return this.http.get<GradeGroup[]>(`${this.api}/guardians/me/children/${studentId}/grades`)}
}
