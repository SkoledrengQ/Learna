import { CommonModule } from '@angular/common';
import { Component, Inject, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AttendanceStatus, LessonAttendance } from '../../shared/models/attendance.model';
import { AttendanceService } from './attendance.service';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { LanguageService } from '../../core/services/language.service';

interface Row { studentId: number; name: string; nickname?: string | null; status: AttendanceStatus | null; note: string; recordedBy?: string; updatedAt?: string; }

@Component({
  selector: 'app-attendance-dialog', standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatSnackBarModule, TranslocoModule, LocalizedDatePipe],
  templateUrl: './attendance-dialog.component.html', styleUrl: './attendance-dialog.component.scss'
})
export class AttendanceDialogComponent {
  private service = inject(AttendanceService); private snack = inject(MatSnackBar); private transloco = inject(TranslocoService);
  protected language = inject(LanguageService);
  loading = signal(true); saving = signal(false); attendance = signal<LessonAttendance | null>(null); rows = signal<Row[]>([]);
  readonly statuses: AttendanceStatus[] = ['Present', 'Absent', 'Late', 'ExcusedAbsence', 'Sick', 'ApprovedLeave'];

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: { lessonId: number }, private ref: MatDialogRef<AttendanceDialogComponent>) { this.load(); }
  private load(): void { this.service.getLesson(this.data.lessonId).subscribe({ next: x => { this.setData(x); this.loading.set(false); }, error: () => { this.snack.open(this.transloco.translate('attendance.loadFailed')); this.ref.close(); } }); }
  private setData(x: LessonAttendance): void { this.attendance.set(x); this.rows.set(x.students.map(s => ({ studentId: s.studentId, name: `${s.firstName} ${s.lastName}`, nickname: s.nickname, status: s.record?.status ?? null, note: s.record?.note ?? '', recordedBy: s.record?.recordedByEmail, updatedAt: s.record?.updatedAt }))); }
  markAllPresent(): void { this.rows.update(rows => rows.map(r => ({ ...r, status: 'Present' }))); }
  invalid(): boolean { return this.rows().some(r => !r.status); }
  save(): void { if (this.invalid()) return; this.saving.set(true); this.service.saveLesson(this.data.lessonId, this.rows().map(r => ({ studentId: r.studentId, status: r.status!, note: r.note || null }))).subscribe({ next: x => { this.setData(x); this.saving.set(false); this.snack.open(this.transloco.translate('attendance.saved'), this.transloco.translate('common.close'), { duration: 2500 }); this.ref.close(true); }, error: e => { this.saving.set(false); this.snack.open(e.error || this.transloco.translate('attendance.saveFailed')); } }); }
}
