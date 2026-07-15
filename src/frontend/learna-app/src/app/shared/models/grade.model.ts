export type GradeStatus = 'Draft'|'Published';
export interface Grade { id:number; studentId:number; studentName:string; subjectGroupId:number; submissionId:number|null; assignmentId:number|null; category:string; score:number; maxScore:number; feedback:string|null; status:GradeStatus; publishedAt:string|null; createdAt:string; updatedAt:string; }
export interface GradeGroup { subjectGroupId:number; groupName:string; subjectName:string; averagePercentage:number; grades:Grade[]; }
export interface GradeWrite { score:number; maxScore:number; feedback:string|null; category?:string|null; }
export interface ManualGradeWrite extends GradeWrite { studentId:number; category:string; }
