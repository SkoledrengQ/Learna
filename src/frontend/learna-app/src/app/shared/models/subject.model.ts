export interface Subject {
  id: number;
  code: string;
  nameEnglish: string;
  nameThai?: string | null;
  description?: string | null;
}

export interface CreateSubjectDto {
  code: string;
  nameEnglish: string;
  nameThai?: string | null;
  description?: string | null;
}

export interface UpdateSubjectDto {
  code: string;
  nameEnglish: string;
  nameThai?: string | null;
  description?: string | null;
}
