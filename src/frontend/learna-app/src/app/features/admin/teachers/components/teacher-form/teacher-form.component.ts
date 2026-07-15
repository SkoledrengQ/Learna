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
import { TeacherService } from '../../../services/teacher.service';
import { LoginSectionComponent } from '../../../../../shared/components/login-section/login-section.component';
import { CreateTeacherDto, UpdateTeacherDto } from '../../../../../shared/models/teacher.model';

@Component({
  selector: 'app-teacher-form',
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
    LoginSectionComponent
  ],
  templateUrl: './teacher-form.component.html',
  styleUrl: './teacher-form.component.scss'
})
export class TeacherFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private teacherService = inject(TeacherService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  teacherForm!: FormGroup;
  isEditMode = false;
  teacherId?: number;
  isLoading = signal(false);

  ngOnInit(): void {
    this.initializeForm();
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.teacherForm = this.fb.group({
      title: [''],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      firstNameEnglish: ['', Validators.maxLength(100)],
      lastNameEnglish: ['', Validators.maxLength(100)],
      nickname: ['', Validators.maxLength(100)],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      phoneNumber: ['', Validators.maxLength(20)],
      employeeId: ['', Validators.maxLength(50)]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.teacherId = +id;
      this.loadTeacher(this.teacherId);
    }
  }

  private loadTeacher(id: number): void {
    this.isLoading.set(true);
    this.teacherService.getTeacher(id).subscribe({
      next: (teacher) => {
        this.teacherForm.patchValue({
          title: teacher.name.title,
          firstName: teacher.name.firstName,
          lastName: teacher.name.lastName,
          firstNameEnglish: teacher.name.firstNameEnglish,
          lastNameEnglish: teacher.name.lastNameEnglish,
          nickname: teacher.name.nickname,
          email: teacher.email,
          phoneNumber: teacher.phoneNumber,
          employeeId: teacher.employeeId
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading teacher:', error);
        this.snackBar.open(this.transloco.translate('admin.teachers.loadFailedSingle'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
        this.router.navigate(['/admin/teachers']);
      }
    });
  }

  onSubmit(): void {
    if (this.teacherForm.invalid) {
      this.teacherForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    if (this.isEditMode && this.teacherId) {
      this.updateTeacher();
    } else {
      this.createTeacher();
    }
  }

  private buildName() {
    const v = this.teacherForm.value;
    return {
      title: v.title || null,
      firstName: v.firstName,
      lastName: v.lastName,
      firstNameEnglish: v.firstNameEnglish || null,
      lastNameEnglish: v.lastNameEnglish || null,
      nickname: v.nickname || null
    };
  }

  private createTeacher(): void {
    const dto: CreateTeacherDto = {
      name: this.buildName(),
      email: this.teacherForm.value.email,
      phoneNumber: this.teacherForm.value.phoneNumber || null,
      employeeId: this.teacherForm.value.employeeId || null
    };

    this.teacherService.createTeacher(dto).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.teachers.createSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/teachers']);
      },
      error: (error) => {
        console.error('Error creating teacher:', error);
        const message = error.error?.includes?.('already exists') ? error.error : this.transloco.translate('admin.teachers.createFailed');
        this.snackBar.open(message, this.transloco.translate('common.close'), { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private updateTeacher(): void {
    const dto: UpdateTeacherDto = {
      name: this.buildName(),
      email: this.teacherForm.value.email,
      phoneNumber: this.teacherForm.value.phoneNumber || null,
      employeeId: this.teacherForm.value.employeeId || null
    };

    this.teacherService.updateTeacher(this.teacherId!, dto).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.teachers.updateSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/teachers']);
      },
      error: (error) => {
        console.error('Error updating teacher:', error);
        this.snackBar.open(this.transloco.translate('admin.teachers.updateFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/teachers']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.teacherForm.get(fieldName);
    if (field?.hasError('required')) {
      return this.transloco.translate('validation.required');
    }
    if (field?.hasError('email')) {
      return this.transloco.translate('validation.email');
    }
    if (field?.hasError('maxlength')) {
      return this.transloco.translate('validation.maxLength');
    }
    return '';
  }
}
