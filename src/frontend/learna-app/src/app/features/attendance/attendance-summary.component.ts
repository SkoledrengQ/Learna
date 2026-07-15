import { CommonModule } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { TranslocoModule } from '@jsverse/transloco';
import { AttendanceSummary } from '../../shared/models/attendance.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { LanguageService } from '../../core/services/language.service';
import { AttendanceService } from './attendance.service';

@Component({ selector: 'app-attendance-summary', standalone: true, imports: [CommonModule, MatCardModule, MatTableModule, TranslocoModule, LocalizedDatePipe], templateUrl: './attendance-summary.component.html', styleUrl: './attendance-summary.component.scss' })
export class AttendanceSummaryComponent {
  private service = inject(AttendanceService); protected language = inject(LanguageService);
  studentId = input<number | null>(null); summary = signal<AttendanceSummary | null>(null); loading = signal(true);
  groupColumns = ['subject', 'recorded', 'presence', 'excused', 'unexcused']; recentColumns = ['date', 'lesson', 'status', 'note'];
  constructor() { effect(() => { this.loading.set(true); const id = this.studentId(); const request = id ? this.service.getStudent(id) : this.service.getMine(); request.subscribe({ next: x => { this.summary.set(x); this.loading.set(false); }, error: () => this.loading.set(false) }); }); }
}
