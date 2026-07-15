import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Assignment, AssignmentWrite, Extension, GuardianAssignment, Submission, SubmissionRoster } from '../../shared/models/assignment.model';
import { SubjectGroup } from '../../shared/models/subject-group.model';

@Injectable({providedIn:'root'})
export class AssignmentsService {
  private readonly http=inject(HttpClient); private readonly api=environment.apiUrl;
  teacherGroups():Observable<SubjectGroup[]>{return this.http.get<SubjectGroup[]>(`${this.api}/subject-groups/mine`)}
  forGroup(id:number):Observable<Assignment[]>{return this.http.get<Assignment[]>(`${this.api}/subject-groups/${id}/assignments`)}
  get(id:number):Observable<Assignment>{return this.http.get<Assignment>(`${this.api}/assignments/${id}`)}
  create(groupId:number,data:AssignmentWrite):Observable<Assignment>{return this.http.post<Assignment>(`${this.api}/subject-groups/${groupId}/assignments`,this.write(data))}
  update(id:number,data:AssignmentWrite):Observable<Assignment>{return this.http.put<Assignment>(`${this.api}/assignments/${id}`,this.write(data))}
  remove(id:number):Observable<void>{return this.http.delete<void>(`${this.api}/assignments/${id}`)}
  publish(id:number):Observable<Assignment>{return this.http.post<Assignment>(`${this.api}/assignments/${id}/publish`,{})}
  close(id:number):Observable<Assignment>{return this.http.post<Assignment>(`${this.api}/assignments/${id}/close`,{})}
  submissions(id:number):Observable<SubmissionRoster[]>{return this.http.get<SubmissionRoster[]>(`${this.api}/assignments/${id}/submissions`)}
  extension(id:number,studentId:number,date:string,time:string,note:string):Observable<Extension>{return this.http.put<Extension>(`${this.api}/assignments/${id}/extensions/${studentId}`,{extendedDeadlineDate:date,extendedDeadlineTime:this.time(time),note:note.trim()||null})}
  mine():Observable<Assignment[]>{return this.http.get<Assignment[]>(`${this.api}/assignments/my`)}
  guardian(studentId:number):Observable<GuardianAssignment[]>{return this.http.get<GuardianAssignment[]>(`${this.api}/guardians/me/children/${studentId}/assignments`)}
  submit(id:number,files:File[],text:string):Observable<Submission>{const data=new FormData();files.forEach(f=>data.append('files',f,f.name));if(text.trim())data.append('text',text.trim());return this.http.post<Submission>(`${this.api}/assignments/${id}/submission`,data)}
  private write(data:AssignmentWrite):AssignmentWrite{return{...data,deadlineTime:this.time(data.deadlineTime)}}
  private time(value:string):string{return value.length===5?`${value}:00`:value}
}
