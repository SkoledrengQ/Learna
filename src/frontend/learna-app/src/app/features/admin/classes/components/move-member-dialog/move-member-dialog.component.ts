import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { TranslocoModule } from '@jsverse/transloco';
import { SchoolClass } from '../../../../../shared/models/school-class.model';

export interface MoveMemberDialogData {
  currentClassId: number;
  otherClasses: SchoolClass[];
}

@Component({
  selector: 'app-move-member-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatDatepickerModule,
    TranslocoModule
  ],
  templateUrl: './move-member-dialog.component.html',
  styleUrl: './move-member-dialog.component.scss'
})
export class MoveMemberDialogComponent {
  toClassId: number | null = null;
  moveDate: Date = new Date();

  constructor(
    public dialogRef: MatDialogRef<MoveMemberDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: MoveMemberDialogData
  ) {}

  onCancel(): void {
    this.dialogRef.close();
  }

  onMove(): void {
    if (!this.toClassId) return;

    this.dialogRef.close({
      toClassId: this.toClassId,
      moveDate: this.moveDate
    });
  }
}
