import { Component, Inject, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LanguageService } from '../../../core/services/language.service';
import { Lesson } from '../../models/lesson.model';
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';
import { AttendanceDialogComponent } from '../../../features/attendance/attendance-dialog.component';

export interface LessonDetailDialogData {
  lesson: Lesson;
}

@Component({
  selector: 'app-lesson-detail-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, TranslocoModule, LocalizedDatePipe],
  templateUrl: './lesson-detail-dialog.component.html',
  styleUrl: './lesson-detail-dialog.component.scss'
})
export class LessonDetailDialogComponent {
  protected readonly languageService = inject(LanguageService);
  private readonly dialog = inject(MatDialog);

  readonly lesson: Lesson;

  readonly subjectName = computed(() => {
    const lang = this.languageService.activeLang();
    if (lang === 'th' && this.lesson.subjectNameThai) {
      return this.lesson.subjectNameThai;
    }
    return this.lesson.subjectNameEnglish;
  });

  constructor(
    public dialogRef: MatDialogRef<LessonDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LessonDetailDialogData
  ) {
    this.lesson = data.lesson;
  }

  onClose(): void {
    this.dialogRef.close();
  }

  openAttendance(): void {
    this.dialog.open(AttendanceDialogComponent, { width: '860px', maxWidth: '96vw', data: { lessonId: this.lesson.id } });
  }
}
