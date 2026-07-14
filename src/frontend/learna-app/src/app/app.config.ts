import { registerLocaleData } from '@angular/common';
import localeTh from '@angular/common/locales/th';
import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideTransloco } from '@jsverse/transloco';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { TranslocoHttpLoader } from './core/transloco-loader';
import { LanguageService } from './core/services/language.service';
import { environment } from '../environments/environment';

registerLocaleData(localeTh);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    provideAnimations(),
    provideTransloco({
      config: {
        availableLangs: ['en', 'th'],
        defaultLang: 'en',
        reRenderOnLangChange: true,
        prodMode: environment.production
      },
      loader: TranslocoHttpLoader
    }),
    provideAppInitializer(() => inject(LanguageService).init())
  ]
};
