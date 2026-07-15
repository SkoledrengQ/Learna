import { Grade } from './grade.model';

export type LatePolicy = 'Block' | 'AllowMarkLate';
export type AssignmentStatus = 'Draft' | 'Published' | 'Closed' | 'Graded' | 'Archived';
export interface AssignmentFile { id:number; originalFileName:string; contentType:string; sizeBytes:number; description:string|null; createdAt:string; }
export interface Submission { id:number; studentId:number; submittedAt:string; text:string|null; isLate:boolean; files:AssignmentFile[]; grade:Grade|null; }
export interface Assignment { id:number; subjectGroupId:number; groupName:string; title:string; description:string|null; startDate:string|null; deadlineDate:string; deadlineTime:string; effectiveDeadlineDate:string; effectiveDeadlineTime:string; hasExtension:boolean; latePolicy:LatePolicy; status:AssignmentStatus; submittedCount:number; rosterCount:number; ownSubmission:Submission|null; files:AssignmentFile[]; createdAt:string; updatedAt:string; }
export interface AssignmentWrite { title:string; description:string|null; startDate:string|null; deadlineDate:string; deadlineTime:string; latePolicy:LatePolicy; }
export interface Extension { studentId:number; extendedDeadlineDate:string; extendedDeadlineTime:string; note:string|null; }
export interface SubmissionRoster { studentId:number; studentName:string; studentNumber:string; status:'Submitted'|'Missing'|'Late'; submission:Submission|null; extension:Extension|null; }
export interface GuardianAssignment { id:number; title:string; subjectGroupId:number; groupName:string; effectiveDeadlineDate:string; effectiveDeadlineTime:string; hasExtension:boolean; status:'Submitted'|'Missing'|'Late'; isPast:boolean; }
