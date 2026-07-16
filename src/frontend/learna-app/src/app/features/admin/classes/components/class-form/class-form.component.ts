import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { ClassService } from '../../../services/class.service';
import { SchoolYearService } from '../../../services/school-year.service';
import { TeacherService } from '../../../services/teacher.service';
import { CreateSchoolClassDto, UpdateSchoolClassDto, ClassMembership, SchoolClass } from '../../../../../shared/models/school-class.model';
import { SchoolYear } from '../../../../../shared/models/school-year.model';
import { Teacher } from '../../../../../shared/models/teacher.model';
import { getDisplayName } from '../../../../../shared/models/student.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AddMemberDialogComponent } from '../add-member-dialog/add-member-dialog.component';
import { MoveMemberDialogComponent } from '../move-member-dialog/move-member-dialog.component';
import { LanguageService } from '../../../../../core/services/language.service';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-class-form',
  standalone: true,
  imports: [
    CommonModule,
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
    PageHeaderComponent
  ],
  templateUrl: './class-form.component.html',
  styleUrl: './class-form.component.scss'
})
export class ClassFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private classService = inject(ClassService);
  private schoolYearService = inject(SchoolYearService);
  private teacherService = inject(TeacherService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);
  protected languageService = inject(LanguageService);

  readonly getDisplayName = getDisplayName;

  classForm!: FormGroup;
  isEditMode = false;
  classId?: number;
  schoolYearId?: number;
  isLoading = signal(false);

  schoolYears = signal<SchoolYear[]>([]);
  teachers = signal<Teacher[]>([]);
  allClasses = signal<SchoolClass[]>([]);

  members = signal<ClassMembership[]>([]);
  showHistorical = signal(false);
  isLoadingMembers = signal(false);

  ngOnInit(): void {
    this.initializeForm();
    this.schoolYearService.getSchoolYears().subscribe({ next: (years) => this.schoolYears.set(years) });
    this.teacherService.getTeachers().subscribe({ next: (teachers) => this.teachers.set(teachers) });
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.classForm = this.fb.group({
      schoolYearId: [null, Validators.required],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      homeroomTeacherId: [null]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.classId = +id;
      this.classForm.get('schoolYearId')?.disable();
      this.loadClass(this.classId);
      this.loadMembers(this.classId);
    }
  }

  private loadClass(id: number): void {
    this.isLoading.set(true);
    this.classService.getClass(id).subscribe({
      next: (schoolClass) => {
        this.schoolYearId = schoolClass.schoolYearId;
        this.classForm.patchValue({
          schoolYearId: schoolClass.schoolYearId,
          name: schoolClass.name,
          description: schoolClass.description,
          homeroomTeacherId: schoolClass.homeroomTeacherId
        });
        this.classService.getClasses(schoolClass.schoolYearId).subscribe({
          next: (classes) => this.allClasses.set(classes.filter(c => c.id !== id))
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading class:', error);
        this.snackBar.open(this.transloco.translate('admin.classes.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
        this.router.navigate(['/admin/classes']);
      }
    });
  }

  private loadMembers(classId: number): void {
    this.isLoadingMembers.set(true);
    this.classService.getMembers(classId, this.showHistorical()).subscribe({
      next: (members) => {
        this.members.set(members);
        this.isLoadingMembers.set(false);
      },
      error: (error) => {
        console.error('Error loading members:', error);
        this.snackBar.open(this.transloco.translate('admin.classes.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoadingMembers.set(false);
      }
    });
  }

  onToggleHistorical(): void {
    this.showHistorical.set(!this.showHistorical());
    if (this.classId) {
      this.loadMembers(this.classId);
    }
  }

  onSubmit(): void {
    if (this.classForm.invalid) {
      this.classForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    if (this.isEditMode && this.classId) {
      this.updateClass();
    } else {
      this.createClass();
    }
  }

  private createClass(): void {
    const dto: CreateSchoolClassDto = {
      schoolYearId: this.classForm.value.schoolYearId,
      name: this.classForm.value.name,
      description: this.classForm.value.description || null,
      homeroomTeacherId: this.classForm.value.homeroomTeacherId || null
    };

    this.classService.createClass(dto).subscribe({
      next: (schoolClass) => {
        this.snackBar.open(this.transloco.translate('admin.classes.createSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/classes', schoolClass.id]);
      },
      error: (error) => {
        console.error('Error creating class:', error);
        this.snackBar.open(error.error || this.transloco.translate('admin.classes.createFailed'), this.transloco.translate('common.close'), { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private updateClass(): void {
    const dto: UpdateSchoolClassDto = {
      name: this.classForm.value.name,
      description: this.classForm.value.description || null,
      homeroomTeacherId: this.classForm.value.homeroomTeacherId || null
    };

    this.classService.updateClass(this.classId!, dto).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.classes.updateSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/classes']);
      },
      error: (error) => {
        console.error('Error updating class:', error);
        this.snackBar.open(error.error || this.transloco.translate('admin.classes.updateFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/classes']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.classForm.get(fieldName);
    if (field?.hasError('required')) {
      return this.transloco.translate('validation.required');
    }
    if (field?.hasError('maxlength')) {
      return this.transloco.translate('validation.maxLength');
    }
    return '';
  }

  onAddMember(): void {
    if (!this.classId) return;

    const dialogRef = this.dialog.open(AddMemberDialogComponent, {
      width: '500px',
      data: { excludeStudentIds: this.members().filter(m => !m.leftDate).map(m => m.student.id) }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.classService.addMember(this.classId!, result).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.classes.addedToClass'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadMembers(this.classId!);
          },
          error: (error) => {
            console.error('Error adding member:', error);
            this.snackBar.open(error.error || this.transloco.translate('admin.classes.addMemberFailed'), this.transloco.translate('common.close'), { duration: 5000 });
          }
        });
      }
    });
  }

  onMoveMember(membership: ClassMembership): void {
    if (!this.classId) return;

    const dialogRef = this.dialog.open(MoveMemberDialogComponent, {
      width: '450px',
      data: { currentClassId: this.classId, otherClasses: this.allClasses() }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.classService.moveMember(this.classId!, membership.student.id, result).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.classes.movedSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadMembers(this.classId!);
          },
          error: (error) => {
            console.error('Error moving student:', error);
            this.snackBar.open(error.error || this.transloco.translate('admin.classes.moveFailed'), this.transloco.translate('common.close'), { duration: 5000 });
          }
        });
      }
    });
  }

  onRemoveMember(membership: ClassMembership): void {
    if (!this.classId) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.transloco.translate('admin.classes.removeMemberTitle'),
        message: this.transloco.translate('admin.classes.removeMemberMessage', { name: this.getDisplayName(membership.student.name) })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.classService.removeMember(this.classId!, membership.student.id).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.classes.removedFromClass'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadMembers(this.classId!);
          },
          error: (error) => {
            console.error('Error removing member:', error);
            this.snackBar.open(this.transloco.translate('admin.classes.removeMemberFailed'), this.transloco.translate('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }
}
