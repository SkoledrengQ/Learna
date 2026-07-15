import { formatDate } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { SupportedLanguage } from '../../core/services/language.service';

const LOCALE_BY_LANGUAGE: Record<SupportedLanguage, string> = {
  en: 'en-US',
  th: 'th-TH-u-ca-buddhist'
};

const THAI_FORMAT_OPTIONS: Record<string, Intl.DateTimeFormatOptions> = {
  mediumDate: { day: 'numeric', month: 'short', year: 'numeric' },
  'd MMM': { day: 'numeric', month: 'short' },
  'd MMM y': { day: 'numeric', month: 'short', year: 'numeric' },
  fullDate: { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }
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

    if (lang === 'th') {
      const date = value instanceof Date ? value : new Date(value);
      const options = THAI_FORMAT_OPTIONS[format] ?? THAI_FORMAT_OPTIONS['mediumDate'];
      return new Intl.DateTimeFormat(LOCALE_BY_LANGUAGE.th, options).format(date);
    }

    return formatDate(value, format, LOCALE_BY_LANGUAGE[lang]);
  }
}
