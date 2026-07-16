import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminConversationSummary,
  ConversationDetail,
  ConversationList,
  ConversationSummary,
  CreateConversationRequest,
  DirectoryPerson,
  ConversationMessage,
  MessagePage
} from '../../shared/models/messaging.model';

@Injectable({ providedIn: 'root' })
export class MessagesService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;
  private readonly listSignal = signal<ConversationList>({ conversations: [], totalUnread: 0 });

  readonly conversations = computed(() => this.listSignal().conversations);
  readonly totalUnread = computed(() => this.listSignal().totalUnread);

  loadMine(): Observable<ConversationList> {
    return this.http.get<ConversationList>(`${this.api}/conversations`).pipe(tap(list => this.listSignal.set(list)));
  }

  get(id: number): Observable<ConversationDetail> {
    return this.http.get<ConversationDetail>(`${this.api}/conversations/${id}`);
  }

  create(payload: CreateConversationRequest): Observable<ConversationSummary> {
    return this.http.post<ConversationSummary>(`${this.api}/conversations`, payload);
  }

  messages(id: number, before?: number, limit = 30): Observable<MessagePage> {
    let params = new HttpParams().set('limit', limit);
    if (before != null) params = params.set('before', before);
    return this.http.get<MessagePage>(`${this.api}/conversations/${id}/messages`, { params });
  }

  send(id: number, body: string): Observable<ConversationMessage> {
    return this.http.post<ConversationMessage>(`${this.api}/conversations/${id}/messages`, { body });
  }

  markRead(id: number): Observable<void> {
    return this.http.post<void>(`${this.api}/conversations/${id}/read`, {});
  }

  addParticipants(id: number, userIds: number[]): Observable<ConversationDetail> {
    return this.http.post<ConversationDetail>(`${this.api}/conversations/${id}/participants`, { userIds });
  }

  removeParticipant(id: number, userId: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/conversations/${id}/participants/${userId}`);
  }

  directory(search?: string): Observable<DirectoryPerson[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<DirectoryPerson[]>(`${this.api}/users/directory`, { params });
  }

  adminSearch(search?: string): Observable<AdminConversationSummary[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<AdminConversationSummary[]>(`${this.api}/admin/conversations`, { params });
  }
}
