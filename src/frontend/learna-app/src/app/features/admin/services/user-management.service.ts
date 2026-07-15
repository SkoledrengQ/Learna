import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CreateManagedUser, LinkablePerson, ManagedUser, ResetPasswordResponse } from '../../../shared/models/user-management.model';

@Injectable({ providedIn: 'root' })
export class UserManagementService {
  private api = inject(ApiService);

  getUsers(search = ''): Observable<ManagedUser[]> {
    return this.api.get<ManagedUser[]>(`users${search ? `?search=${encodeURIComponent(search)}` : ''}`);
  }

  getLinkable(linkType: string): Observable<LinkablePerson[]> {
    return this.api.get<LinkablePerson[]>(`users/linkable?linkType=${encodeURIComponent(linkType)}`);
  }

  createUser(request: CreateManagedUser): Observable<ManagedUser> {
    return this.api.post<ManagedUser>('users', request);
  }

  setActive(id: number, isActive: boolean): Observable<ManagedUser> {
    return this.api.put<ManagedUser>(`users/${id}/active`, { isActive });
  }

  resetPassword(id: number): Observable<ResetPasswordResponse> {
    return this.api.put<ResetPasswordResponse>(`users/${id}/password`, {});
  }
}
