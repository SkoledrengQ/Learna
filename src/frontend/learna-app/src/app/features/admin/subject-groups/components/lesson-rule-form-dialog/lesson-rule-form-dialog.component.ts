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
import { Room } from '../../../../../shared/models/room.model';
import { DAYS_OF_WEEK, DayOfWeekName, LessonRule } from '../../../../../shared/models/lesson-rule.model';

export interface LessonRuleFormDialogData {
  rule?: LessonRule;
}

@Component({
  selector: 'app-lesson-rule-form-dialog',
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
  templateUrl: './lesson-rule-form-dialog.component.html',
  styleUrl: './lesson-rule-form-dialog.component.scss'
})
export class LessonRuleFormDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private roomService = inject(RoomService);

  readonly days = DAYS_OF_WEEK;

  isEditMode: boolean;
  ruleForm: FormGroup;
  rooms = signal<Room[]>([]);

  constructor(
    public dialogRef: MatDialogRef<LessonRuleFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LessonRuleFormDialogData
  ) {
    this.isEditMode = !!data.rule;
    const r = data.rule;

    this.ruleForm = this.fb.group({
      dayOfWeek: [r?.dayOfWeek ?? ('Monday' as DayOfWeekName), Validators.required],
      startTime: [r ? r.startTime.slice(0, 5) : '', Validators.required],
      endTime: [r ? r.endTime.slice(0, 5) : '', Validators.required],
      roomId: [r?.roomId ?? null],
      startDate: [r?.startDate ?? ''],
      endDate: [r?.endDate ?? '']
    });
  }

  ngOnInit(): void {
    this.roomService.getRooms().subscribe({ next: (rooms) => this.rooms.set(rooms) });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.ruleForm.invalid) {
      this.ruleForm.markAllAsTouched();
      return;
    }

    const v = this.ruleForm.value;
    this.dialogRef.close({
      dayOfWeek: v.dayOfWeek,
      startTime: `${v.startTime}:00`,
      endTime: `${v.endTime}:00`,
      roomId: v.roomId || null,
      startDate: v.startDate || null,
      endDate: v.endDate || null
    });
  }
}
