export interface SchoolYear {
  id: number;
  name: string;
  startDate: Date;
  endDate: Date;
  isArchived: boolean;
}

export interface CreateSchoolYearDto {
  name: string;
  startDate: Date;
  endDate: Date;
}

export interface UpdateSchoolYearDto {
  name: string;
  startDate: Date;
  endDate: Date;
  isArchived: boolean;
}

export interface Term {
  id: number;
  schoolYearId: number;
  name: string;
  startDate: Date;
  endDate: Date;
}

export interface CreateTermDto {
  name: string;
  startDate: Date;
  endDate: Date;
}

export interface UpdateTermDto {
  name: string;
  startDate: Date;
  endDate: Date;
}
