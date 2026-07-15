import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Announcement, AnnouncementFeed, AnnouncementTarget, AnnouncementTargets, AnnouncementWrite } from '../../shared/models/announcement.model';
import { FileResource } from '../../shared/models/file-resource.model';

@Injectable({ providedIn: 'root' })
export class AnnouncementsService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;
  private readonly feedSignal = signal<AnnouncementFeed>({ announcements: [], unreadCount: 0 });

  readonly feed = computed(() => this.feedSignal().announcements);
  readonly unreadCount = computed(() => this.feedSignal().unreadCount);

  loadMine(): Observable<AnnouncementFeed> {
    return this.http.get<AnnouncementFeed>(`${this.api}/announcements/my`).pipe(tap(feed => this.feedSignal.set(feed)));
  }

  get(id: number): Observable<Announcement> {
    return this.http.get<Announcement>(`${this.api}/announcements/${id}`);
  }

  create(payload: AnnouncementWrite): Observable<Announcement> {
    return this.http.post<Announcement>(`${this.api}/announcements`, payload);
  }

  update(id: number, payload: AnnouncementWrite): Observable<Announcement> {
    return this.http.put<Announcement>(`${this.api}/announcements/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/announcements/${id}`);
  }

  markRead(id: number): Observable<void> {
    return this.http.post<void>(`${this.api}/announcements/${id}/read`, {}).pipe(tap(() => this.markLocalRead(id)));
  }

  targets(): Observable<AnnouncementTargets> {
    return this.http.get<AnnouncementTargets>(`${this.api}/announcements/targets`);
  }

  upload(id: number, file: File, description = ''): Observable<FileResource> {
    const data = new FormData();
    data.append('file', file, file.name);
    if (description.trim()) data.append('description', description.trim());
    return this.http.post<FileResource>(`${this.api}/announcements/${id}/files`, data);
  }

  private markLocalRead(id: number): void {
    const current = this.feedSignal();
    const announcements = current.announcements.map(a => a.id === id ? { ...a, isRead: true } : a);
    this.feedSignal.set({ announcements, unreadCount: announcements.filter(a => !a.isRead).length });
  }
}
