import { Component, signal, inject, computed } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { CommonModule } from '@angular/common';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from './core/services/auth.service';
import { LanguageService, SupportedLanguage } from './core/services/language.service';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    TranslocoModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private authService = inject(AuthService);
  protected readonly languageService = inject(LanguageService);

  protected readonly title = signal('Learna');
  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly currentUser = computed(() => this.authService.getCurrentUser());
  protected readonly isAdmin = computed(() => this.authService.hasAnyRole(['Admin']));
  protected readonly isStudentLinked = computed(() => !!this.currentUser()?.studentId);

  onLogout(): void {
    this.authService.logout();
  }

  onSelectLanguage(lang: SupportedLanguage): void {
    this.languageService.setLanguage(lang, this.isAuthenticated());
  }
}
