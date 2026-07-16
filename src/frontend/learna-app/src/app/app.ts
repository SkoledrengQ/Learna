import { Component, signal, inject, computed, effect } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { CommonModule } from '@angular/common';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from './core/services/auth.service';
import { LanguageService, SupportedLanguage } from './core/services/language.service';
import { SchoolSettingsService } from './core/services/school-settings.service';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AnnouncementsService } from './core/services/announcements.service';
import { MessagesService } from './core/services/messages.service';

const MOBILE_BREAKPOINT = '(max-width: 899.98px)';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatBadgeModule,
    TranslocoModule,
    MatDialogModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private authService = inject(AuthService);
  protected readonly languageService = inject(LanguageService);
  protected readonly schoolSettings = inject(SchoolSettingsService);
  private readonly dialog = inject(MatDialog);
  private readonly breakpointObserver = inject(BreakpointObserver);
  protected readonly announcements = inject(AnnouncementsService);
  protected readonly messages = inject(MessagesService);

  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly currentUser = computed(() => this.authService.getCurrentUser());
  protected readonly isAdmin = computed(() => this.authService.hasAnyRole(['Admin']));
  protected readonly canViewStudents = computed(() => this.authService.hasAnyRole(['Admin', 'Teacher']));
  protected readonly canViewAttendance = computed(() => !!this.currentUser()?.studentId || !!this.currentUser()?.guardianId);
  protected readonly canViewMaterials = computed(() => !!this.currentUser()?.studentId || !!this.currentUser()?.guardianId);
  protected readonly canViewGroups = computed(() => !!this.currentUser()?.teacherId);
  protected readonly canViewAssignments = computed(() => !!this.currentUser()?.studentId || !!this.currentUser()?.guardianId);
  protected readonly canViewGrades = computed(() => !!this.currentUser()?.studentId || !!this.currentUser()?.guardianId);

  protected readonly schoolName = this.schoolSettings.schoolName;
  protected readonly schoolInitial = computed(() => this.schoolName().trim().charAt(0).toUpperCase() || 'L');

  protected readonly isMobile = toSignal(
    this.breakpointObserver.observe(MOBILE_BREAKPOINT).pipe(map(state => state.matches)),
    { initialValue: false }
  );
  protected readonly sidenavOpened = signal(true);

  constructor() {
    effect(() => {
      if (this.isAuthenticated()) {
        this.announcements.loadMine().subscribe({ error: () => undefined });
        this.messages.loadMine().subscribe({ error: () => undefined });
      }
    });

    // Mobile starts as a closed drawer; desktop starts with the sidenav pinned open.
    effect(() => {
      this.sidenavOpened.set(!this.isMobile());
    });
  }

  onToggleSidenav(): void {
    this.sidenavOpened.update(open => !open);
  }

  onNavItemClick(): void {
    if (this.isMobile()) {
      this.sidenavOpened.set(false);
    }
  }

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
