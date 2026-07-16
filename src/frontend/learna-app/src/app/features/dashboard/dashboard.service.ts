import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminDashboard, TeacherDashboard } from './dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  teacher(): Observable<TeacherDashboard> {
    return this.http.get<TeacherDashboard>(`${this.api}/dashboard/teacher`);
  }

  admin(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${this.api}/dashboard/admin`);
  }
}
