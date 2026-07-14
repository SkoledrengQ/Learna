import { formatDate } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { SupportedLanguage } from '../../core/services/language.service';

// TODO: Buddhist Era year display for Thai locale is deferred (out of scope for WO4).
const LOCALE_BY_LANGUAGE: Record<SupportedLanguage, string> = {
  en: 'en-US',
  th: 'th-TH'
};

@Pipe({
  name: 'localizedDate',
  standalone: true
})
export class LocalizedDatePipe implements PipeTransform {
  transform(value: Date | string | null | undefined, lang: SupportedLanguage, format = 'mediumDate'): string {
    if (!value) {
      return '';
    }
    return formatDate(value, format, LOCALE_BY_LANGUAGE[lang]);
  }
}
