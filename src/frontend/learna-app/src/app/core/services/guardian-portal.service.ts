import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GuardianChild } from '../../shared/models/guardian.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class GuardianPortalService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private loadedGuardianId: number | null = null;

  readonly children = signal<GuardianChild[]>([]);
  readonly selectedChildId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly loadFailed = signal(false);
  readonly isGuardianLinked = computed(() => !!this.auth.getCurrentUser()?.guardianId);
  readonly selectedChild = computed(() => this.children().find(child => child.id === this.selectedChildId()) ?? null);

  loadChildren(): void {
    const guardianId = this.auth.getCurrentUser()?.guardianId ?? null;
    if (!guardianId || this.loading() || this.loadedGuardianId === guardianId) return;

    if (this.loadedGuardianId !== null && this.loadedGuardianId !== guardianId) {
      this.children.set([]);
      this.selectedChildId.set(null);
    }

    this.loading.set(true);
    this.loadFailed.set(false);
    this.http.get<GuardianChild[]>(`${environment.apiUrl}/guardians/me/children`).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: children => {
        this.loadedGuardianId = guardianId;
        this.children.set(children);
        const stored = Number(localStorage.getItem(this.storageKey(guardianId)));
        const selected = children.find(child => child.id === stored) ?? children[0] ?? null;
        this.selectedChildId.set(selected?.id ?? null);
        if (selected) localStorage.setItem(this.storageKey(guardianId), selected.id.toString());
      },
      error: () => {
        this.children.set([]);
        this.selectedChildId.set(null);
        this.loadFailed.set(true);
      }
    });
  }

  selectChild(studentId: number): void {
    const guardianId = this.auth.getCurrentUser()?.guardianId;
    if (!guardianId || !this.children().some(child => child.id === studentId)) return;
    this.selectedChildId.set(studentId);
    localStorage.setItem(this.storageKey(guardianId), studentId.toString());
  }

  private storageKey(guardianId: number): string {
    return `guardian_${guardianId}_selected_child`;
  }
}
