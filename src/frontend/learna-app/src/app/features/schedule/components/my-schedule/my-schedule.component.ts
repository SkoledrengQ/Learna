import { Component, computed, inject, signal } from '@angular/core';
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

@Component({
  selector: 'app-my-schedule',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatSnackBarModule, TranslocoModule, ScheduleViewComponent],
  templateUrl: './my-schedule.component.html',
  styleUrl: './my-schedule.component.scss'
})
export class MyScheduleComponent {
  private scheduleService = inject(ScheduleService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  lessons = signal<Lesson[]>([]);
  isLoading = signal(false);
  notFound = signal(false);
  isAdmin = computed(() => this.authService.hasAnyRole(['Admin']));

  private currentRange: { from: string; to: string } | null = null;

  onWeekRangeChange(range: { from: string; to: string }): void {
    this.currentRange = range;
    this.load();
  }

  private load(): void {
    const range = this.currentRange;
    if (!range) return;

    this.isLoading.set(true);
    this.scheduleService.getMySchedule(range.from, range.to).subscribe({
      next: (lessons) => {
        this.lessons.set(lessons);
        this.notFound.set(false);
        this.isLoading.set(false);
      },
      error: (error: any) => {
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
