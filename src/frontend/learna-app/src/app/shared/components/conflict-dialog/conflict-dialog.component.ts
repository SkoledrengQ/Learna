import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { TranslocoModule } from '@jsverse/transloco';
import { LessonConflict } from '../../models/lesson.model';

export interface ConflictDialogData {
  conflicts: LessonConflict[];
}

@Component({
  selector: 'app-conflict-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatListModule, TranslocoModule],
  templateUrl: './conflict-dialog.component.html',
  styleUrl: './conflict-dialog.component.scss'
})
export class ConflictDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConflictDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConflictDialogData
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onSaveAnyway(): void {
    this.dialogRef.close(true);
  }
}
