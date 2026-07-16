import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SchoolSettings, UpdateSchoolSettings } from '../../shared/models/school-settings.model';
import { derivePrimaryPalette } from '../../shared/utils/theme-color.util';

const STORAGE_KEY = 'learna_school_settings';
export const DEFAULT_SCHOOL_SETTINGS: SchoolSettings = {
  schoolName: 'Learna School',
  primaryColor: '#00695C'
};

/**
 * Runtime brand theming: applies the school's primary color as CSS custom property
 * overrides on the Material system tokens, so the whole app re-themes without a rebuild.
 * The last-known settings are cached in localStorage so a hard reload paints the correct
 * brand color immediately, before any network round trip resolves.
 */
@Injectable({ providedIn: 'root' })
export class SchoolSettingsService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  private readonly settingsSignal = signal<SchoolSettings>(this.readCached() ?? DEFAULT_SCHOOL_SETTINGS);
  readonly settings = this.settingsSignal.asReadonly();
  readonly schoolName = computed(() => this.settingsSignal().schoolName);
  readonly primaryColor = computed(() => this.settingsSignal().primaryColor);

  /** Paints the cached (or default) brand color before first render. No network call. */
  init(): void {
    this.applyColorVars(this.settingsSignal().primaryColor);
  }

  /** Authenticated fetch of the latest settings (e.g. admin settings page on load). */
  refresh(): Observable<SchoolSettings> {
    return this.http.get<SchoolSettings>(`${this.api}/school-settings`).pipe(tap(settings => this.apply(settings)));
  }

  update(payload: UpdateSchoolSettings): Observable<SchoolSettings> {
    return this.http.put<SchoolSettings>(`${this.api}/school-settings`, payload).pipe(tap(settings => this.apply(settings)));
  }

  /** Applies settings received out-of-band (login/refresh response) and re-themes immediately. */
  apply(settings: SchoolSettings): void {
    this.settingsSignal.set(settings);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
    this.applyColorVars(settings.primaryColor);
  }

  private applyColorVars(primaryColor: string): void {
    const palette = derivePrimaryPalette(primaryColor);
    const root = document.documentElement.style;
    root.setProperty('--mat-sys-primary', palette.primary);
    root.setProperty('--mat-sys-on-primary', palette.onPrimary);
    root.setProperty('--mat-sys-primary-container', palette.primaryContainer);
    root.setProperty('--mat-sys-on-primary-container', palette.onPrimaryContainer);
    root.setProperty('--mat-sys-primary-fixed', palette.primaryFixed);
    root.setProperty('--mat-sys-primary-fixed-dim', palette.primaryFixedDim);
    root.setProperty('--mat-sys-on-primary-fixed', palette.onPrimaryFixed);
    root.setProperty('--mat-sys-on-primary-fixed-variant', palette.onPrimaryFixedVariant);
    root.setProperty('--mat-sys-inverse-primary', palette.inversePrimary);
    root.setProperty('--mat-sys-surface-tint', palette.surfaceTint);
    // App-specific brand vars for chrome we hand-roll (shell header, badges, chips) rather
    // than route through Material component tokens.
    root.setProperty('--app-brand', palette.primary);
    root.setProperty('--app-on-brand', palette.onPrimary);
    root.setProperty('--app-brand-container', palette.primaryContainer);
    root.setProperty('--app-on-brand-container', palette.onPrimaryContainer);
  }

  private readCached(): SchoolSettings | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const parsed = JSON.parse(raw);
      if (typeof parsed?.schoolName === 'string' && typeof parsed?.primaryColor === 'string') {
        return parsed;
      }
      return null;
    } catch {
      return null;
    }
  }
}
