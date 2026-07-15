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
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

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
    TranslocoModule,
    MatDialogModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private authService = inject(AuthService);
  protected readonly languageService = inject(LanguageService);
  private readonly dialog = inject(MatDialog);

  protected readonly title = signal('Learna');
  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly currentUser = computed(() => this.authService.getCurrentUser());
  protected readonly isAdmin = computed(() => this.authService.hasAnyRole(['Admin']));
  protected readonly canViewStudents = computed(() => this.authService.hasAnyRole(['Admin', 'Teacher']));
  protected readonly canViewAttendance = computed(() => !!this.currentUser()?.studentId || !!this.currentUser()?.guardianId);

  onLogout(): void {
    this.authService.logout();
  }

  onChangePassword(): void {
    import('./shared/components/change-password-dialog/change-password-dialog.component').then(({ ChangePasswordDialogComponent }) =>
      this.dialog.open(ChangePasswordDialogComponent, { width: '480px' })
    );
  }

  onSelectLanguage(lang: SupportedLanguage): void {
    this.languageService.setLanguage(lang, this.isAuthenticated());
  }
}
