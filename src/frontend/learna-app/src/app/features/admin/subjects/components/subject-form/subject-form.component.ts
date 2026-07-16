import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { SubjectService } from '../../../services/subject.service';
import { CreateSubjectDto, UpdateSubjectDto } from '../../../../../shared/models/subject.model';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-subject-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
    TranslocoModule,
    PageHeaderComponent
  ],
  templateUrl: './subject-form.component.html',
  styleUrl: './subject-form.component.scss'
})
export class SubjectFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private subjectService = inject(SubjectService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  subjectForm!: FormGroup;
  isEditMode = false;
  subjectId?: number;
  isLoading = signal(false);

  ngOnInit(): void {
    this.initializeForm();
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.subjectForm = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(50)]],
      nameEnglish: ['', [Validators.required, Validators.maxLength(200)]],
      nameThai: ['', Validators.maxLength(200)],
      description: ['', Validators.maxLength(1000)]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.subjectId = +id;
      this.loadSubject(this.subjectId);
    }
  }

  private loadSubject(id: number): void {
    this.isLoading.set(true);
    this.subjectService.getSubject(id).subscribe({
      next: (subject) => {
        this.subjectForm.patchValue({
          code: subject.code,
          nameEnglish: subject.nameEnglish,
          nameThai: subject.nameThai,
          description: subject.description
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading subject:', error);
        this.snackBar.open(this.transloco.translate('admin.subjects.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
        this.router.navigate(['/admin/subjects']);
      }
    });
  }

  onSubmit(): void {
    if (this.subjectForm.invalid) {
      this.subjectForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    if (this.isEditMode && this.subjectId) {
      this.updateSubject();
    } else {
      this.createSubject();
    }
  }

  private createSubject(): void {
    const dto: CreateSubjectDto = {
      code: this.subjectForm.value.code,
      nameEnglish: this.subjectForm.value.nameEnglish,
      nameThai: this.subjectForm.value.nameThai || null,
      description: this.subjectForm.value.description || null
    };

    this.subjectService.createSubject(dto).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.subjects.createSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/subjects']);
      },
      error: (error) => {
        console.error('Error creating subject:', error);
        const message = error.error?.includes?.('already exists') ? error.error : this.transloco.translate('admin.subjects.createFailed');
        this.snackBar.open(message, this.transloco.translate('common.close'), { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private updateSubject(): void {
    const dto: UpdateSubjectDto = {
      code: this.subjectForm.value.code,
      nameEnglish: this.subjectForm.value.nameEnglish,
      nameThai: this.subjectForm.value.nameThai || null,
      description: this.subjectForm.value.description || null
    };

    this.subjectService.updateSubject(this.subjectId!, dto).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.subjects.updateSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/subjects']);
      },
      error: (error) => {
        console.error('Error updating subject:', error);
        const message = error.error?.includes?.('already exists') ? error.error : this.transloco.translate('admin.subjects.updateFailed');
        this.snackBar.open(message, this.transloco.translate('common.close'), { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/subjects']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.subjectForm.get(fieldName);
    if (field?.hasError('required')) {
      return this.transloco.translate('validation.required');
    }
    if (field?.hasError('maxlength')) {
      return this.transloco.translate('validation.maxLength');
    }
    return '';
  }
}
