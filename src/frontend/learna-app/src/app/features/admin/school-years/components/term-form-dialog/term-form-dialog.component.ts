import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { TranslocoModule } from '@jsverse/transloco';
import { Term } from '../../../../../shared/models/school-year.model';

export interface TermFormDialogData {
  term?: Term;
}

@Component({
  selector: 'app-term-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    TranslocoModule
  ],
  templateUrl: './term-form-dialog.component.html',
  styleUrl: './term-form-dialog.component.scss'
})
export class TermFormDialogComponent {
  private fb = inject(FormBuilder);

  isEditMode: boolean;
  termForm: FormGroup;

  constructor(
    public dialogRef: MatDialogRef<TermFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TermFormDialogData
  ) {
    this.isEditMode = !!data.term;
    const t = data.term;

    this.termForm = this.fb.group({
      name: [t?.name ?? '', [Validators.required, Validators.maxLength(50)]],
      startDate: [t ? new Date(t.startDate) : '', Validators.required],
      endDate: [t ? new Date(t.endDate) : '', Validators.required]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.termForm.invalid) {
      this.termForm.markAllAsTouched();
      return;
    }

    const value = this.termForm.value;
    this.dialogRef.close({
      name: value.name,
      startDate: value.startDate,
      endDate: value.endDate
    });
  }
}
