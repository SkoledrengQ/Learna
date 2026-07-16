import { CommonModule } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { TranslocoModule } from '@jsverse/transloco';
import { AttendanceSummary } from '../../shared/models/attendance.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { LanguageService } from '../../core/services/language.service';
import { AttendanceService } from './attendance.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../shared/components/status-chip/status-chip.component';
import { attendanceStatusVariant } from '../../shared/utils/status-variant.util';

@Component({ selector: 'app-attendance-summary', standalone: true, imports: [CommonModule, MatCardModule, MatTableModule, TranslocoModule, LocalizedDatePipe, PageHeaderComponent, LoadingStateComponent, StatusChipComponent], templateUrl: './attendance-summary.component.html', styleUrl: './attendance-summary.component.scss' })
export class AttendanceSummaryComponent {
  private service = inject(AttendanceService); protected language = inject(LanguageService);
  studentId = input<number | null>(null); guardianChildId = input<number | null>(null); summary = signal<AttendanceSummary | null>(null); loading = signal(true);
  groupColumns = ['subject', 'recorded', 'presence', 'excused', 'unexcused']; recentColumns = ['date', 'lesson', 'status', 'note'];
  readonly attendanceStatusVariant = attendanceStatusVariant;
  constructor() { effect(onCleanup => { this.loading.set(true); const guardianId = this.guardianChildId(); const id = this.studentId(); const request = guardianId ? this.service.getGuardianChild(guardianId) : id ? this.service.getStudent(id) : this.service.getMine(); const subscription = request.subscribe({ next: x => { this.summary.set(x); this.loading.set(false); }, error: () => { this.summary.set(null); this.loading.set(false); } }); onCleanup(() => subscription.unsubscribe()); }); }
}
