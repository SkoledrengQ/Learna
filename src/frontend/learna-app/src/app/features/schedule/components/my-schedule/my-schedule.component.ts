import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ScheduleService } from '../../services/schedule.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Lesson } from '../../../../shared/models/lesson.model';
import { ScheduleViewComponent } from '../../../../shared/components/schedule-view/schedule-view.component';
import { GuardianPortalService } from '../../../../core/services/guardian-portal.service';
import { ChildSelectorComponent } from '../../../../shared/components/child-selector/child-selector.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'app-my-schedule',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatSnackBarModule, TranslocoModule, ScheduleViewComponent, ChildSelectorComponent, PageHeaderComponent, LoadingStateComponent],
  templateUrl: './my-schedule.component.html',
  styleUrl: './my-schedule.component.scss'
})
export class MyScheduleComponent {
  private scheduleService = inject(ScheduleService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);
  protected portal = inject(GuardianPortalService);

  lessons = signal<Lesson[]>([]);
  isLoading = signal(false);
  notFound = signal(false);
  isAdmin = computed(() => this.authService.hasAnyRole(['Admin']));
  isGuardian = this.portal.isGuardianLinked;
  noChildren = computed(() => this.isGuardian() && !this.portal.loading() && !this.portal.loadFailed() && this.portal.children().length === 0);

  private currentRange = signal<{ from: string; to: string } | null>(null);
  private loadSequence = 0;

  constructor() {
    effect(() => {
      if (this.isGuardian()) this.portal.loadChildren();
    });
    effect(() => {
      const range = this.currentRange();
      const childId = this.portal.selectedChildId();
      if (range && (!this.isGuardian() || childId)) this.load(range, childId);
    });
  }

  onWeekRangeChange(range: { from: string; to: string }): void {
    this.currentRange.set(range);
  }

  private load(range: { from: string; to: string }, childId: number | null): void {
    const sequence = ++this.loadSequence;
    this.isLoading.set(true);
    const request = this.isGuardian() && childId
      ? this.scheduleService.getStudentSchedule(childId, range.from, range.to)
      : this.scheduleService.getMySchedule(range.from, range.to);
    request.subscribe({
      next: (lessons) => {
        if (sequence !== this.loadSequence) return;
        this.lessons.set(lessons);
        this.notFound.set(false);
        this.isLoading.set(false);
      },
      error: (error: any) => {
        if (sequence !== this.loadSequence) return;
        this.isLoading.set(false);
        if (error.status === 404) {
          this.notFound.set(true);
        } else {
          console.error('Error loading schedule:', error);
          this.snackBar.open(this.transloco.translate('schedule.loadFailed'), this.transloco.translate('common.close'), { duration: 5000 });
        }
      }
    });
  }
}
