import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { TranslocoService } from '@jsverse/transloco';
import { AUTH_USER_STORAGE_KEY } from '../../shared/models/auth.model';
import { environment } from '../../../environments/environment';

export type SupportedLanguage = 'en' | 'th';

const LANGUAGE_STORAGE_KEY = 'learna_language';

function isSupportedLanguage(value: unknown): value is SupportedLanguage {
  return value === 'en' || value === 'th';
}

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private transloco = inject(TranslocoService);
  private http = inject(HttpClient);

  private readonly API_URL = `${environment.apiUrl}/auth`;

  /** Reflects the currently active language; templates read this signal directly for reactivity. */
  readonly activeLang = signal<SupportedLanguage>(this.resolveInitialLanguage());

  /** Sets the active language at startup, before first render, based on the resolution order. */
  init(): void {
    this.activate(this.activeLang());
  }

  /**
   * Called from the toolbar switcher. Always updates localStorage; additionally persists to the
   * backend and the stored user object when the caller is authenticated.
   */
  setLanguage(lang: SupportedLanguage, persistToBackend: boolean): void {
    this.activate(lang);

    if (persistToBackend) {
      this.updateStoredUserLanguage(lang);
      this.http.put(`${this.API_URL}/language`, { language: lang }).subscribe({
        error: (error) => console.error('Failed to persist language preference:', error)
      });
    }
  }

  /**
   * Applied right after login/refresh: the logged-in user's stored preference wins.
   * Returns an observable that only completes once the translation file for that
   * language has actually loaded, so callers (e.g. a post-login snackbar) can wait
   * for it instead of racing setActiveLang's async load.
   */
  applyUserPreference(preferredLanguage?: string | null): Observable<void> {
    if (!isSupportedLanguage(preferredLanguage)) return of(undefined);
    return this.transloco.load(preferredLanguage).pipe(map(() => {
      this.activate(preferredLanguage);
    }));
  }

  private activate(lang: SupportedLanguage): void {
    this.transloco.setActiveLang(lang);
    this.activeLang.set(lang);
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
    document.documentElement.lang = lang;
  }

  private resolveInitialLanguage(): SupportedLanguage {
    const userPreference = this.getStoredUserPreferredLanguage();
    if (userPreference) {
      return userPreference;
    }

    const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (isSupportedLanguage(stored)) {
      return stored;
    }

    return 'en';
  }

  private getStoredUserPreferredLanguage(): SupportedLanguage | null {
    const userJson = localStorage.getItem(AUTH_USER_STORAGE_KEY);
    if (!userJson) {
      return null;
    }

    try {
      const user = JSON.parse(userJson);
      return isSupportedLanguage(user?.preferredLanguage) ? user.preferredLanguage : null;
    } catch {
      return null;
    }
  }

  private updateStoredUserLanguage(lang: SupportedLanguage): void {
    const userJson = localStorage.getItem(AUTH_USER_STORAGE_KEY);
    if (!userJson) {
      return;
    }

    try {
      const user = JSON.parse(userJson);
      user.preferredLanguage = lang;
      localStorage.setItem(AUTH_USER_STORAGE_KEY, JSON.stringify(user));
    } catch {
      // Corrupt stored user JSON; nothing sensible to update.
    }
  }
}
