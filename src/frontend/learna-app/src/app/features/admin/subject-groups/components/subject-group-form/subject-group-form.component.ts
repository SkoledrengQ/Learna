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
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { SubjectGroupService } from '../../../services/subject-group.service';
import { SchoolYearService } from '../../../services/school-year.service';
import { SubjectService } from '../../../services/subject.service';
import { TeacherService } from '../../../services/teacher.service';
import { LessonRuleService } from '../../../services/lesson-rule.service';
import { CreateSubjectGroupDto, UpdateSubjectGroupDto, Enrollment } from '../../../../../shared/models/subject-group.model';
import { SchoolYear, Term } from '../../../../../shared/models/school-year.model';
import { Subject as SubjectModel } from '../../../../../shared/models/subject.model';
import { Teacher } from '../../../../../shared/models/teacher.model';
import { LessonRule } from '../../../../../shared/models/lesson-rule.model';
import { getDisplayName } from '../../../../../shared/models/student.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { ConflictDialogComponent } from '../../../../../shared/components/conflict-dialog/conflict-dialog.component';
import { EnrollClassDialogComponent } from '../enroll-class-dialog/enroll-class-dialog.component';
import { EnrollStudentsDialogComponent } from '../enroll-students-dialog/enroll-students-dialog.component';
import { LessonRuleFormDialogComponent } from '../lesson-rule-form-dialog/lesson-rule-form-dialog.component';
import { LanguageService } from '../../../../../core/services/language.service';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { Observable } from 'rxjs';
import { MaterialListComponent } from '../../../../../shared/components/material-list/material-list.component';

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
    MatTooltipModule,
    TranslocoModule,
    LocalizedDatePipe,
    MaterialListComponent
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
  private lessonRuleService = inject(LessonRuleService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);
  protected languageService = inject(LanguageService);

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

  rules = signal<LessonRule[]>([]);
  isLoadingRules = signal(false);

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
      this.loadRules(this.groupId);
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
        this.snackBar.open(this.transloco.translate('admin.subjectGroups.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
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
        this.snackBar.open(this.transloco.translate('admin.subjectGroups.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
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
        this.snackBar.open(this.transloco.translate('admin.subjectGroups.createSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/subject-groups', group.id]);
      },
      error: (error) => {
        console.error('Error creating subject group:', error);
        this.snackBar.open(error.error || this.transloco.translate('admin.subjectGroups.createFailed'), this.transloco.translate('common.close'), { duration: 5000 });
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
        this.snackBar.open(this.transloco.translate('admin.subjectGroups.updateSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/subject-groups']);
      },
      error: (error) => {
        console.error('Error updating subject group:', error);
        this.snackBar.open(error.error || this.transloco.translate('admin.subjectGroups.updateFailed'), this.transloco.translate('common.close'), { duration: 3000 });
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
      return this.transloco.translate('validation.required');
    }
    if (field?.hasError('maxlength')) {
      return this.transloco.translate('validation.maxLength');
    }
    return '';
  }

  onEnrollClass(): void {
    if (!this.groupId) return;

    const dialogRef = this.dialog.open(EnrollClassDialogComponent, { width: '450px' });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.subjectGroupService.enrollClass(this.groupId!, result.classId).subscribe({
          next: (summary) => {
            this.snackBar.open(
              this.transloco.translate('admin.subjectGroups.enrolledSummary', { enrolled: summary.enrolled, skipped: summary.skipped }),
              this.transloco.translate('common.close'),
              { duration: 4000 }
            );
            this.loadEnrollments(this.groupId!);
          },
          error: (error) => {
            console.error('Error enrolling class:', error);
            this.snackBar.open(error.error || this.transloco.translate('admin.subjectGroups.enrollClassFailed'), this.transloco.translate('common.close'), { duration: 5000 });
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
                this.snackBar.open(this.transloco.translate('admin.subjectGroups.studentsEnrolled'), this.transloco.translate('common.close'), { duration: 3000 });
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
        title: this.transloco.translate('admin.subjectGroups.unenrollTitle'),
        message: this.transloco.translate('admin.subjectGroups.unenrollMessage', { name: this.getDisplayName(enrollment.student.name) })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.subjectGroupService.removeEnrollment(this.groupId!, enrollment.student.id).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.subjectGroups.unenrolledSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadEnrollments(this.groupId!);
          },
          error: (error) => {
            console.error('Error unenrolling student:', error);
            this.snackBar.open(this.transloco.translate('admin.subjectGroups.unenrollFailed'), this.transloco.translate('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  loadRules(groupId: number): void {
    this.isLoadingRules.set(true);
    this.lessonRuleService.getRules(groupId).subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.isLoadingRules.set(false);
      },
      error: (error) => {
        console.error('Error loading rules:', error);
        this.snackBar.open(this.transloco.translate('admin.subjectGroups.rules.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoadingRules.set(false);
      }
    });
  }

  onAddRule(): void {
    if (!this.groupId) return;

    const dialogRef = this.dialog.open(LessonRuleFormDialogComponent, { width: '500px', data: {} });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.saveRuleWithConflictHandling(
        (force) => this.lessonRuleService.createRule(this.groupId!, { ...result, force }),
        'admin.subjectGroups.rules.addSuccess',
        'admin.subjectGroups.rules.addFailed'
      );
    });
  }

  onEditRule(rule: LessonRule): void {
    if (!this.groupId) return;

    const dialogRef = this.dialog.open(LessonRuleFormDialogComponent, { width: '500px', data: { rule } });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.saveRuleWithConflictHandling(
        (force) => this.lessonRuleService.updateRule(this.groupId!, rule.id, { ...result, force }),
        'admin.subjectGroups.rules.updateSuccess',
        'admin.subjectGroups.rules.updateFailed'
      );
    });
  }

  onDeleteRule(rule: LessonRule): void {
    if (!this.groupId) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.transloco.translate('admin.subjectGroups.rules.deleteTitle'),
        message: this.transloco.translate('admin.subjectGroups.rules.deleteMessage')
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.lessonRuleService.deleteRule(this.groupId!, rule.id).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.subjectGroups.rules.deleteSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadRules(this.groupId!);
          },
          error: (error) => {
            console.error('Error deleting rule:', error);
            this.snackBar.open(this.transloco.translate('admin.subjectGroups.rules.deleteFailed'), this.transloco.translate('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  private saveRuleWithConflictHandling(action: (force: boolean) => Observable<LessonRule>, successKey: string, failKey: string): void {
    action(false).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate(successKey), this.transloco.translate('common.close'), { duration: 3000 });
        this.loadRules(this.groupId!);
      },
      error: (error: any) => {
        if (error.status === 409 && error.error?.conflicts) {
          const dialogRef = this.dialog.open(ConflictDialogComponent, { width: '520px', data: { conflicts: error.error.conflicts } });
          dialogRef.afterClosed().subscribe(confirmed => {
            if (!confirmed) return;
            action(true).subscribe({
              next: () => {
                this.snackBar.open(this.transloco.translate(successKey), this.transloco.translate('common.close'), { duration: 3000 });
                this.loadRules(this.groupId!);
              },
              error: (forceError: any) => {
                console.error('Error force-saving rule:', forceError);
                this.snackBar.open(forceError.error || this.transloco.translate(failKey), this.transloco.translate('common.close'), { duration: 5000 });
              }
            });
          });
        } else {
          console.error('Error saving rule:', error);
          this.snackBar.open(error.error || this.transloco.translate(failKey), this.transloco.translate('common.close'), { duration: 5000 });
        }
      }
    });
  }
}
