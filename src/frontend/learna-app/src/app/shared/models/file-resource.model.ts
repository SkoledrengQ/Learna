export interface FileResource {
  id: number;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  description?: string | null;
  uploadedByUserId: number;
  uploadedBy: string;
  createdAt: string;
  subjectGroupId: number;
  subjectGroupName: string;
  lessonId?: number | null;
  lessonDate?: string | null;
  canDelete: boolean;
  childId?: number | null;
  childName?: string | null;
}
