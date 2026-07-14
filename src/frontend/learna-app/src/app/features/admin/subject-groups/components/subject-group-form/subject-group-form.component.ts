import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SubjectGroupService } from '../../../services/subject-group.service';
import { SchoolYearService } from '../../../services/school-year.service';
import { SubjectService } from '../../../services/subject.service';
import { TeacherService } from '../../../services/teacher.service';
import { CreateSubjectGroupDto, UpdateSubjectGroupDto, Enrollment } from '../../../../../shared/models/subject-group.model';
import { SchoolYear, Term } from '../../../../../shared/models/school-year.model';
import { Subject as SubjectModel } from '../../../../../shared/models/subject.model';
import { Teacher } from '../../../../../shared/models/teacher.model';
import { getDisplayName } from '../../../../../shared/models/student.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EnrollClassDialogComponent } from '../enroll-class-dialog/enroll-class-dialog.component';
import { EnrollStudentsDialogComponent } from '../enroll-students-dialog/enroll-students-dialog.component';

@Component({
  selector: 'app-subject-group-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './subject-group-form.component.html',
  styleUrl: './subject-group-form.component.scss'
})
export class SubjectGroupFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private subjectGroupService = inject(SubjectGroupService);
  private schoolYearService = inject(SchoolYearService);
  private subjectService = inject(SubjectService);
  private teacherService = inject(TeacherService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  readonly getDisplayName = getDisplayName;

  groupForm!: FormGroup;
  isEditMode = false;
  groupId?: number;
  isLoading = signal(false);

  subjects = signal<SubjectModel[]>([]);
  teachers = signal<Teacher[]>([]);
  schoolYears = signal<SchoolYear[]>([]);
  terms = signal<Term[]>([]);
  selectedSchoolYearId: number | null = null;

  enrollments = signal<Enrollment[]>([]);
  showHistorical = signal(false);
  isLoadingEnrollments = signal(false);

  ngOnInit(): void {
    this.initializeForm();
    this.subjectService.getSubjects().subscribe({ next: (subjects) => this.subjects.set(subjects) });
    this.teacherService.getTeachers().subscribe({ next: (teachers) => this.teachers.set(teachers) });
    this.schoolYearService.getSchoolYears().subscribe({
      next: (years) => {
        this.schoolYears.set(years);
        if (!this.isEditMode && years.length > 0) {
          this.onSchoolYearChange(years[0].id);
        }
      }
    });
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.groupForm = this.fb.group({
      subjectId: [null, Validators.required],
      termId: [null, Validators.required],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      teacherId: [null]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.groupId = +id;
      this.groupForm.get('subjectId')?.disable();
      this.groupForm.get('termId')?.disable();
      this.loadGroup(this.groupId);
      this.loadEnrollments(this.groupId);
    }
  }

  onSchoolYearChange(schoolYearId: number): void {
    this.selectedSchoolYearId = schoolYearId;
    this.schoolYearService.getTerms(schoolYearId).subscribe({
      next: (terms) => this.terms.set(terms)
    });
  }

  private loadGroup(id: number): void {
    this.isLoading.set(true);
    this.subjectGroupService.getSubjectGroup(id).subscribe({
      next: (group) => {
        this.groupForm.patchValue({
          subjectId: group.subjectId,
          termId: group.termId,
          name: group.name,
          teacherId: group.teacherId
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading subject group:', error);
        this.snackBar.open('Failed to load subject group', 'Close', { duration: 3000 });
        this.isLoading.set(false);
        this.router.navigate(['/admin/subject-groups']);
      }
    });
  }

  private loadEnrollments(groupId: number): void {
    this.isLoadingEnrollments.set(true);
    this.subjectGroupService.getEnrollments(groupId, this.showHistorical()).subscribe({
      next: (enrollments) => {
        this.enrollments.set(enrollments);
        this.isLoadingEnrollments.set(false);
      },
      error: (error) => {
        console.error('Error loading enrollments:', error);
        this.snackBar.open('Failed to load enrollments', 'Close', { duration: 3000 });
        this.isLoadingEnrollments.set(false);
      }
    });
  }

  onToggleHistorical(): void {
    this.showHistorical.set(!this.showHistorical());
    if (this.groupId) {
      this.loadEnrollments(this.groupId);
    }
  }

  onSubmit(): void {
    if (this.groupForm.invalid) {
      this.groupForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    if (this.isEditMode && this.groupId) {
      this.updateGroup();
    } else {
      this.createGroup();
    }
  }

  private createGroup(): void {
    const dto: CreateSubjectGroupDto = {
      subjectId: this.groupForm.value.subjectId,
      termId: this.groupForm.value.termId,
      name: this.groupForm.value.name,
      teacherId: this.groupForm.value.teacherId || null
    };

    this.subjectGroupService.createSubjectGroup(dto).subscribe({
      next: (group) => {
        this.snackBar.open('Subject group created successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/admin/subject-groups', group.id]);
      },
      error: (error) => {
        console.error('Error creating subject group:', error);
        this.snackBar.open(error.error || 'Failed to create subject group', 'Close', { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private updateGroup(): void {
    const dto: UpdateSubjectGroupDto = {
      name: this.groupForm.value.name,
      teacherId: this.groupForm.value.teacherId || null
    };

    this.subjectGroupService.updateSubjectGroup(this.groupId!, dto).subscribe({
      next: () => {
        this.snackBar.open('Subject group updated successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/admin/subject-groups']);
      },
      error: (error) => {
        console.error('Error updating subject group:', error);
        this.snackBar.open(error.error || 'Failed to update subject group', 'Close', { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/subject-groups']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.groupForm.get(fieldName);
    if (field?.hasError('required')) {
      return 'This field is required';
    }
    if (field?.hasError('maxlength')) {
      return 'Maximum length exceeded';
    }
    return '';
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString();
  }

  onEnrollClass(): void {
    if (!this.groupId) return;

    const dialogRef = this.dialog.open(EnrollClassDialogComponent, { width: '450px' });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.subjectGroupService.enrollClass(this.groupId!, result.classId).subscribe({
          next: (summary) => {
            this.snackBar.open(`Enrolled ${summary.enrolled} student(s), skipped ${summary.skipped} already enrolled`, 'Close', { duration: 4000 });
            this.loadEnrollments(this.groupId!);
          },
          error: (error) => {
            console.error('Error enrolling class:', error);
            this.snackBar.open(error.error || 'Failed to enroll class', 'Close', { duration: 5000 });
          }
        });
      }
    });
  }

  onEnrollStudents(): void {
    if (!this.groupId) return;

    const dialogRef = this.dialog.open(EnrollStudentsDialogComponent, {
      width: '500px',
      data: { excludeStudentIds: this.enrollments().filter(e => !e.unenrolledDate).map(e => e.student.id) }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const studentIds: number[] = result.studentIds;
        let remaining = studentIds.length;
        studentIds.forEach(studentId => {
          this.subjectGroupService.addEnrollment(this.groupId!, { studentId, enrolledDate: null }).subscribe({
            next: () => {
              remaining--;
              if (remaining === 0) {
                this.snackBar.open('Students enrolled', 'Close', { duration: 3000 });
                this.loadEnrollments(this.groupId!);
              }
            },
            error: (error) => {
              console.error('Error enrolling student:', error);
              remaining--;
              if (remaining === 0) {
                this.loadEnrollments(this.groupId!);
              }
            }
          });
        });
      }
    });
  }

  onUnenroll(enrollment: Enrollment): void {
    if (!this.groupId) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Unenroll Student',
        message: `Unenroll ${this.getDisplayName(enrollment.student.name)} from this subject group? Enrollment history will be kept.`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.subjectGroupService.removeEnrollment(this.groupId!, enrollment.student.id).subscribe({
          next: () => {
            this.snackBar.open('Student unenrolled', 'Close', { duration: 3000 });
            this.loadEnrollments(this.groupId!);
          },
          error: (error) => {
            console.error('Error unenrolling student:', error);
            this.snackBar.open('Failed to unenroll student', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }
}
