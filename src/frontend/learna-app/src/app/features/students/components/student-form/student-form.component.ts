import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { StudentService } from '../../services/student.service';
import { CreateStudentDto, UpdateStudentDto, getDisplayName } from '../../../../shared/models/student.model';
import { StudentGuardian } from '../../../../shared/models/guardian.model';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { GuardianFormDialogComponent } from '../guardian-form-dialog/guardian-form-dialog.component';

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './student-form.component.html',
  styleUrl: './student-form.component.scss'
})
export class StudentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private studentService = inject(StudentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  studentForm!: FormGroup;
  isEditMode = false;
  studentId?: number;
  isLoading = signal(false);

  guardians = signal<StudentGuardian[]>([]);
  isLoadingGuardians = signal(false);

  readonly getDisplayName = getDisplayName;

  ngOnInit(): void {
    this.initializeForm();
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.studentForm = this.fb.group({
      title: [''],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      firstNameEnglish: ['', Validators.maxLength(100)],
      lastNameEnglish: ['', Validators.maxLength(100)],
      nickname: ['', Validators.maxLength(100)],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      idCardNumber: ['', [Validators.required, Validators.maxLength(50)]],
      dateOfBirth: ['', Validators.required],
      enrollmentDate: [new Date(), Validators.required],
      phoneNumber: ['', [Validators.required, Validators.maxLength(20)]],
      address: ['', [Validators.required, Validators.maxLength(500)]],
      height: ['', [Validators.min(0), Validators.max(999)]],
      weight: ['', [Validators.min(0), Validators.max(999)]]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.studentId = +id;
      this.loadStudent(this.studentId);
      this.loadGuardians(this.studentId);
      // Disable fields that can't be changed in edit mode
      this.studentForm.get('enrollmentDate')?.disable();
    }
  }

  private loadStudent(id: number): void {
    this.isLoading.set(true);
    this.studentService.getStudent(id).subscribe({
      next: (student) => {
        this.studentForm.patchValue({
          title: student.name.title,
          firstName: student.name.firstName,
          lastName: student.name.lastName,
          firstNameEnglish: student.name.firstNameEnglish,
          lastNameEnglish: student.name.lastNameEnglish,
          nickname: student.name.nickname,
          email: student.email,
          idCardNumber: student.idCardNumber,
          dateOfBirth: new Date(student.dateOfBirth),
          enrollmentDate: new Date(student.enrollmentDate),
          phoneNumber: student.phoneNumber,
          address: student.address,
          height: student.height,
          weight: student.weight
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading student:', error);
        this.snackBar.open('Failed to load student', 'Close', { duration: 3000 });
        this.isLoading.set(false);
        this.router.navigate(['/students']);
      }
    });
  }

  private loadGuardians(studentId: number): void {
    this.isLoadingGuardians.set(true);
    this.studentService.getGuardians(studentId).subscribe({
      next: (guardians) => {
        this.guardians.set(guardians);
        this.isLoadingGuardians.set(false);
      },
      error: (error) => {
        console.error('Error loading guardians:', error);
        this.snackBar.open('Failed to load guardians', 'Close', { duration: 3000 });
        this.isLoadingGuardians.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.studentForm.invalid) {
      this.studentForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    if (this.isEditMode && this.studentId) {
      this.updateStudent();
    } else {
      this.createStudent();
    }
  }

  private buildName() {
    const v = this.studentForm.value;
    return {
      title: v.title || null,
      firstName: v.firstName,
      lastName: v.lastName,
      firstNameEnglish: v.firstNameEnglish || null,
      lastNameEnglish: v.lastNameEnglish || null,
      nickname: v.nickname || null
    };
  }

  private createStudent(): void {
    const dto: CreateStudentDto = {
      name: this.buildName(),
      email: this.studentForm.value.email,
      idCardNumber: this.studentForm.value.idCardNumber,
      dateOfBirth: this.studentForm.value.dateOfBirth,
      enrollmentDate: this.studentForm.value.enrollmentDate,
      phoneNumber: this.studentForm.value.phoneNumber,
      address: this.studentForm.value.address,
      height: this.studentForm.value.height || null,
      weight: this.studentForm.value.weight || null
    };

    this.studentService.createStudent(dto).subscribe({
      next: (student) => {
        this.snackBar.open('Student created successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/students', student.id]);
      },
      error: (error) => {
        console.error('Error creating student:', error);
        this.snackBar.open('Failed to create student', 'Close', { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private updateStudent(): void {
    const dto: UpdateStudentDto = {
      name: this.buildName(),
      email: this.studentForm.value.email,
      idCardNumber: this.studentForm.value.idCardNumber,
      dateOfBirth: this.studentForm.value.dateOfBirth,
      phoneNumber: this.studentForm.value.phoneNumber,
      address: this.studentForm.value.address,
      height: this.studentForm.value.height || null,
      weight: this.studentForm.value.weight || null
    };

    this.studentService.updateStudent(this.studentId!, dto).subscribe({
      next: () => {
        this.snackBar.open('Student updated successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/students']);
      },
      error: (error) => {
        console.error('Error updating student:', error);
        this.snackBar.open('Failed to update student', 'Close', { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/students']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.studentForm.get(fieldName);
    if (field?.hasError('required')) {
      return 'This field is required';
    }
    if (field?.hasError('email')) {
      return 'Please enter a valid email';
    }
    if (field?.hasError('maxlength')) {
      return 'Maximum length exceeded';
    }
    if (field?.hasError('min') || field?.hasError('max')) {
      return 'Please enter a realistic value';
    }
    return '';
  }

  onAddGuardian(): void {
    if (!this.studentId) return;

    const dialogRef = this.dialog.open(GuardianFormDialogComponent, {
      width: '500px',
      data: {}
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.studentService.addGuardian(this.studentId!, result).subscribe({
          next: () => {
            this.snackBar.open('Guardian added', 'Close', { duration: 3000 });
            this.loadGuardians(this.studentId!);
          },
          error: (error) => {
            console.error('Error adding guardian:', error);
            this.snackBar.open('Failed to add guardian', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  onEditGuardian(guardian: StudentGuardian): void {
    if (!this.studentId) return;

    const dialogRef = this.dialog.open(GuardianFormDialogComponent, {
      width: '500px',
      data: { guardian }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.studentService.updateGuardian(this.studentId!, guardian.guardianId, result).subscribe({
          next: () => {
            this.snackBar.open('Guardian updated', 'Close', { duration: 3000 });
            this.loadGuardians(this.studentId!);
          },
          error: (error) => {
            console.error('Error updating guardian:', error);
            this.snackBar.open('Failed to update guardian', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  onRemoveGuardian(guardian: StudentGuardian): void {
    if (!this.studentId) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Remove Guardian',
        message: `Remove ${getDisplayName(guardian.name)} as a guardian for this student?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.studentService.removeGuardian(this.studentId!, guardian.guardianId).subscribe({
          next: () => {
            this.snackBar.open('Guardian removed', 'Close', { duration: 3000 });
            this.loadGuardians(this.studentId!);
          },
          error: (error) => {
            console.error('Error removing guardian:', error);
            this.snackBar.open('Failed to remove guardian', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }
}
