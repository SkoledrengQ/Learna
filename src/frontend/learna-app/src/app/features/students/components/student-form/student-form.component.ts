import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { StudentService } from '../../services/student.service';
import { CreateStudentDto, UpdateStudentDto } from '../../../../shared/models/student.model';

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
    MatDatepickerModule,
    MatNativeDateModule,
    MatSnackBarModule
  ],
  templateUrl: './student-form.component.html',
  styleUrl: './student-form.component.scss'
})
export class StudentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private studentService = inject(StudentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  studentForm!: FormGroup;
  isEditMode = false;
  studentId?: number;
  isLoading = false;

  ngOnInit(): void {
    this.initializeForm();
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.studentForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      idCardNumber: ['', [Validators.required, Validators.maxLength(50)]],
      dateOfBirth: ['', Validators.required],
      parentPhoneNumber: ['', [Validators.required, Validators.maxLength(20)]],
      enrollmentDate: [new Date(), Validators.required],
      gradeLevel: ['', [Validators.required, Validators.min(1), Validators.max(12)]]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.studentId = +id;
      this.loadStudent(this.studentId);
      // Disable fields that can't be changed in edit mode
      this.studentForm.get('enrollmentDate')?.disable();
    }
  }

  private loadStudent(id: number): void {
    this.isLoading = true;
    this.studentService.getStudent(id).subscribe({
      next: (student) => {
        this.studentForm.patchValue({
          firstName: student.firstName,
          lastName: student.lastName,
          email: student.email,
          idCardNumber: student.idCardNumber,
          dateOfBirth: new Date(student.dateOfBirth),
          parentPhoneNumber: student.parentPhoneNumber,
          enrollmentDate: new Date(student.enrollmentDate),
          gradeLevel: student.gradeLevel
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading student:', error);
        this.snackBar.open('Failed to load student', 'Close', { duration: 3000 });
        this.isLoading = false;
        this.router.navigate(['/students']);
      }
    });
  }

  onSubmit(): void {
    if (this.studentForm.invalid) {
      this.studentForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    if (this.isEditMode && this.studentId) {
      this.updateStudent();
    } else {
      this.createStudent();
    }
  }

  private createStudent(): void {
    const dto: CreateStudentDto = {
      firstName: this.studentForm.value.firstName,
      lastName: this.studentForm.value.lastName,
      email: this.studentForm.value.email,
      idCardNumber: this.studentForm.value.idCardNumber,
      dateOfBirth: this.studentForm.value.dateOfBirth,
      parentPhoneNumber: this.studentForm.value.parentPhoneNumber,
      enrollmentDate: this.studentForm.value.enrollmentDate,
      gradeLevel: +this.studentForm.value.gradeLevel
    };

    this.studentService.createStudent(dto).subscribe({
      next: () => {
        this.snackBar.open('Student created successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/students']);
      },
      error: (error) => {
        console.error('Error creating student:', error);
        this.snackBar.open('Failed to create student', 'Close', { duration: 5000 });
        this.isLoading = false;
      }
    });
  }

  private updateStudent(): void {
    const dto: UpdateStudentDto = {
      firstName: this.studentForm.value.firstName,
      lastName: this.studentForm.value.lastName,
      email: this.studentForm.value.email,
      idCardNumber: this.studentForm.value.idCardNumber,
      dateOfBirth: this.studentForm.value.dateOfBirth,
      parentPhoneNumber: this.studentForm.value.parentPhoneNumber,
      gradeLevel: +this.studentForm.value.gradeLevel
    };

    this.studentService.updateStudent(this.studentId!, dto).subscribe({
      next: () => {
        this.snackBar.open('Student updated successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/students']);
      },
      error: (error) => {
        console.error('Error updating student:', error);
        this.snackBar.open('Failed to update student', 'Close', { duration: 3000 });
        this.isLoading = false;
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
    if (field?.hasError('maxLength')) {
      return `Maximum length exceeded`;
    }
    if (field?.hasError('min')) {
      return 'Grade level must be at least 1';
    }
    if (field?.hasError('max')) {
      return 'Grade level must not exceed 12';
    }
    return '';
  }
}
