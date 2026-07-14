import { Injectable, effect, inject } from '@angular/core';
import { DateAdapter } from '@angular/material/core';
import { LanguageService } from './language.service';

const MAT_LOCALE_BY_LANGUAGE: Record<string, string> = { en: 'en-US', th: 'th-TH' };

/**
 * Keeps the single app-wide Material DateAdapter (see provideNativeDateAdapter() in
 * app.config.ts) in sync with the active UI language, so every Material datepicker
 * follows language switches without each component providing its own adapter instance.
 */
@Injectable({
  providedIn: 'root'
})
export class DateLocaleSyncService {
  private dateAdapter = inject(DateAdapter);
  private languageService = inject(LanguageService);

  constructor() {
    effect(() => {
      const lang = this.languageService.activeLang();
      this.dateAdapter.setLocale(MAT_LOCALE_BY_LANGUAGE[lang] ?? 'en-US');
    });
  }
}
