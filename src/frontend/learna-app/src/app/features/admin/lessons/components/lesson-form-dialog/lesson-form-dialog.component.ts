import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { RoomService } from '../../../services/room.service';
import { TeacherService } from '../../../services/teacher.service';
import { SubjectGroupService } from '../../../services/subject-group.service';
import { Room } from '../../../../../shared/models/room.model';
import { Teacher } from '../../../../../shared/models/teacher.model';
import { SubjectGroup } from '../../../../../shared/models/subject-group.model';
import { Lesson, LessonStatus } from '../../../../../shared/models/lesson.model';
import { getDisplayName } from '../../../../../shared/models/student.model';

export interface LessonFormDialogData {
  lesson?: Lesson;
}

@Component({
  selector: 'app-lesson-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    TranslocoModule
  ],
  templateUrl: './lesson-form-dialog.component.html',
  styleUrl: './lesson-form-dialog.component.scss'
})
export class LessonFormDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private roomService = inject(RoomService);
  private teacherService = inject(TeacherService);
  private subjectGroupService = inject(SubjectGroupService);

  readonly getDisplayName = getDisplayName;

  isEditMode: boolean;
  lessonForm: FormGroup;
  rooms = signal<Room[]>([]);
  teachers = signal<Teacher[]>([]);
  subjectGroups = signal<SubjectGroup[]>([]);

  constructor(
    public dialogRef: MatDialogRef<LessonFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LessonFormDialogData
  ) {
    this.isEditMode = !!data.lesson;
    const l = data.lesson;

    this.lessonForm = this.fb.group({
      subjectGroupId: [l?.subjectGroupId ?? null, Validators.required],
      date: [l?.date ?? '', Validators.required],
      startTime: [l ? l.startTime.slice(0, 5) : '', Validators.required],
      endTime: [l ? l.endTime.slice(0, 5) : '', Validators.required],
      roomId: [l?.roomId ?? null],
      teacherId: [l?.teacherId ?? null],
      note: [l?.note ?? ''],
      status: [l?.status ?? ('Scheduled' as LessonStatus)]
    });

    if (this.isEditMode) {
      this.lessonForm.get('subjectGroupId')?.disable();
    }
  }

  ngOnInit(): void {
    this.roomService.getRooms().subscribe({ next: (rooms) => this.rooms.set(rooms) });
    this.teacherService.getTeachers().subscribe({ next: (teachers) => this.teachers.set(teachers) });
    if (!this.isEditMode) {
      this.subjectGroupService.getSubjectGroups().subscribe({ next: (groups) => this.subjectGroups.set(groups) });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.lessonForm.invalid) {
      this.lessonForm.markAllAsTouched();
      return;
    }

    const v = this.lessonForm.getRawValue();
    this.dialogRef.close({
      subjectGroupId: v.subjectGroupId,
      date: v.date,
      startTime: `${v.startTime}:00`,
      endTime: `${v.endTime}:00`,
      roomId: v.roomId || null,
      teacherId: v.teacherId || null,
      note: v.note || null,
      status: v.status
    });
  }
}
