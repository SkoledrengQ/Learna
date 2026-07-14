import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { StudentGuardian } from '../../../../shared/models/guardian.model';

export interface GuardianFormDialogData {
  guardian?: StudentGuardian;
}

@Component({
  selector: 'app-guardian-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './guardian-form-dialog.component.html',
  styleUrl: './guardian-form-dialog.component.scss'
})
export class GuardianFormDialogComponent {
  private fb = inject(FormBuilder);

  isEditMode: boolean;
  guardianForm: FormGroup;

  constructor(
    public dialogRef: MatDialogRef<GuardianFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: GuardianFormDialogData
  ) {
    this.isEditMode = !!data.guardian;
    const g = data.guardian;

    this.guardianForm = this.fb.group({
      title: [g?.name.title ?? ''],
      firstName: [g?.name.firstName ?? '', [Validators.required, Validators.maxLength(100)]],
      lastName: [g?.name.lastName ?? '', [Validators.required, Validators.maxLength(100)]],
      firstNameEnglish: [g?.name.firstNameEnglish ?? ''],
      lastNameEnglish: [g?.name.lastNameEnglish ?? ''],
      nickname: [g?.name.nickname ?? ''],
      relationship: [g?.relationship ?? '', [Validators.required, Validators.maxLength(50)]],
      phoneNumber: [g?.phoneNumber ?? ''],
      email: [g?.email ?? '', [Validators.email]],
      isPrimaryContact: [g?.isPrimaryContact ?? false]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.guardianForm.invalid) {
      this.guardianForm.markAllAsTouched();
      return;
    }

    const value = this.guardianForm.value;
    this.dialogRef.close({
      name: {
        title: value.title || null,
        firstName: value.firstName,
        lastName: value.lastName,
        firstNameEnglish: value.firstNameEnglish || null,
        lastNameEnglish: value.lastNameEnglish || null,
        nickname: value.nickname || null
      },
      email: value.email || null,
      phoneNumber: value.phoneNumber || null,
      relationship: value.relationship,
      isPrimaryContact: value.isPrimaryContact
    });
  }
}
