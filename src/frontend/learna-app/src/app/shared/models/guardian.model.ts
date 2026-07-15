import { PersonName } from './student.model';

export interface StudentGuardian {
  guardianId: number;
  name: PersonName;
  email?: string | null;
  phoneNumber?: string | null;
  relationship: string;
  isPrimaryContact: boolean;
}

export interface CreateGuardianDto {
  name: PersonName;
  email?: string | null;
  phoneNumber?: string | null;
  relationship: string;
  isPrimaryContact: boolean;
}

export interface UpdateGuardianDto {
  name: PersonName;
  email?: string | null;
  phoneNumber?: string | null;
  relationship: string;
  isPrimaryContact: boolean;
}

export interface GuardianChild {
  id: number;
  name: PersonName;
  relationship: string;
  activeClassName?: string | null;
}
