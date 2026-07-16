import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { LessonService } from '../../../services/lesson.service';
import { RoomService } from '../../../services/room.service';
import { TeacherService } from '../../../services/teacher.service';
import { SubjectGroupService } from '../../../services/subject-group.service';
import { Lesson, UpdateLessonDto } from '../../../../../shared/models/lesson.model';
import { Room } from '../../../../../shared/models/room.model';
import { Teacher } from '../../../../../shared/models/teacher.model';
import { SubjectGroup } from '../../../../../shared/models/subject-group.model';
import { getDisplayName } from '../../../../../shared/models/student.model';
import { ConflictDialogComponent } from '../../../../../shared/components/conflict-dialog/conflict-dialog.component';
import { LessonFormDialogComponent } from '../lesson-form-dialog/lesson-form-dialog.component';
import { AttendanceDialogComponent } from '../../../../attendance/attendance-dialog.component';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../../../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip.component';
import { FilterPanelComponent } from '../../../../../shared/components/filter-panel/filter-panel.component';
import { lessonStatusVariant } from '../../../../../shared/utils/status-variant.util';

@Component({
  selector: 'app-lesson-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule,
    TranslocoModule,
    PageHeaderComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    StatusChipComponent,
    FilterPanelComponent
  ],
  templateUrl: './lesson-list.component.html',
  styleUrl: './lesson-list.component.scss'
})
export class LessonListComponent implements OnInit {
  private lessonService = inject(LessonService);
  private roomService = inject(RoomService);
  private teacherService = inject(TeacherService);
  private subjectGroupService = inject(SubjectGroupService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  readonly getDisplayName = getDisplayName;
  readonly lessonStatusVariant = lessonStatusVariant;

  lessons = signal<Lesson[]>([]);
  rooms = signal<Room[]>([]);
  teachers = signal<Teacher[]>([]);
  subjectGroups = signal<SubjectGroup[]>([]);
  displayedColumns: string[] = ['date', 'time', 'group', 'teacher', 'room', 'status', 'modified', 'actions'];
  isLoading = signal(true);

  fromDate: string | null = null;
  toDate: string | null = null;
  subjectGroupId: number | null = null;
  roomId: number | null = null;
  teacherId: number | null = null;

  ngOnInit(): void {
    this.roomService.getRooms().subscribe({ next: (rooms) => this.rooms.set(rooms) });
    this.teacherService.getTeachers().subscribe({ next: (teachers) => this.teachers.set(teachers) });
    this.subjectGroupService.getSubjectGroups().subscribe({ next: (groups) => this.subjectGroups.set(groups) });
    this.loadLessons();
  }

  loadLessons(): void {
    this.isLoading.set(true);
    this.lessonService.getLessons({
      from: this.fromDate ?? undefined,
      to: this.toDate ?? undefined,
      subjectGroupId: this.subjectGroupId ?? undefined,
      roomId: this.roomId ?? undefined,
      teacherId: this.teacherId ?? undefined
    }).subscribe({
      next: (lessons) => {
        this.lessons.set(lessons);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading lessons:', error);
        this.snackBar.open(this.transloco.translate('admin.lessons.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onFilterChange(): void {
    this.loadLessons();
  }

  onCreateLesson(): void {
    const dialogRef = this.dialog.open(LessonFormDialogComponent, { width: '520px', data: {} });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.saveWithConflictHandling(
        (force) => this.lessonService.createLesson({ ...result, force }),
        'admin.lessons.createSuccess',
        'admin.lessons.createFailed'
      );
    });
  }

  onEditLesson(lesson: Lesson): void {
    const dialogRef = this.dialog.open(LessonFormDialogComponent, { width: '520px', data: { lesson } });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.saveWithConflictHandling(
        (force) => this.lessonService.updateLesson(lesson.id, { ...result, force } as UpdateLessonDto),
        'admin.lessons.updateSuccess',
        'admin.lessons.updateFailed'
      );
    });
  }

  onCancelLesson(lesson: Lesson): void {
    const dto: UpdateLessonDto = {
      date: lesson.date,
      startTime: lesson.startTime,
      endTime: lesson.endTime,
      roomId: lesson.roomId,
      teacherId: lesson.teacherId,
      note: lesson.note,
      status: 'Cancelled'
    };

    this.lessonService.updateLesson(lesson.id, dto).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.lessons.cancelSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.loadLessons();
      },
      error: (error) => {
        console.error('Error cancelling lesson:', error);
        this.snackBar.open(this.transloco.translate('admin.lessons.cancelFailed'), this.transloco.translate('common.close'), { duration: 3000 });
      }
    });
  }

  onAttendance(lesson: Lesson): void {
    this.dialog.open(AttendanceDialogComponent, { width: '860px', maxWidth: '96vw', data: { lessonId: lesson.id } });
  }

  private saveWithConflictHandling(action: (force: boolean) => Observable<Lesson>, successKey: string, failKey: string): void {
    action(false).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate(successKey), this.transloco.translate('common.close'), { duration: 3000 });
        this.loadLessons();
      },
      error: (error: any) => {
        if (error.status === 409 && error.error?.conflicts) {
          const dialogRef = this.dialog.open(ConflictDialogComponent, { width: '520px', data: { conflicts: error.error.conflicts } });
          dialogRef.afterClosed().subscribe(confirmed => {
            if (!confirmed) return;
            action(true).subscribe({
              next: () => {
                this.snackBar.open(this.transloco.translate(successKey), this.transloco.translate('common.close'), { duration: 3000 });
                this.loadLessons();
              },
              error: (forceError: any) => {
                console.error('Error force-saving lesson:', forceError);
                this.snackBar.open(forceError.error || this.transloco.translate(failKey), this.transloco.translate('common.close'), { duration: 5000 });
              }
            });
          });
        } else {
          console.error('Error saving lesson:', error);
          this.snackBar.open(error.error || this.transloco.translate(failKey), this.transloco.translate('common.close'), { duration: 5000 });
        }
      }
    });
  }
}
