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
import { SchoolYearService } from '../../../services/school-year.service';
import { CreateSchoolYearDto, UpdateSchoolYearDto, Term } from '../../../../../shared/models/school-year.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { TermFormDialogComponent } from '../term-form-dialog/term-form-dialog.component';

@Component({
  selector: 'app-school-year-form',
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
  templateUrl: './school-year-form.component.html',
  styleUrl: './school-year-form.component.scss'
})
export class SchoolYearFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private schoolYearService = inject(SchoolYearService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  schoolYearForm!: FormGroup;
  isEditMode = false;
  schoolYearId?: number;
  isLoading = signal(false);

  terms = signal<Term[]>([]);
  isLoadingTerms = signal(false);

  ngOnInit(): void {
    this.initializeForm();
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.schoolYearForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(50)]],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      isArchived: [false]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.schoolYearId = +id;
      this.loadSchoolYear(this.schoolYearId);
      this.loadTerms(this.schoolYearId);
    }
  }

  private loadSchoolYear(id: number): void {
    this.isLoading.set(true);
    this.schoolYearService.getSchoolYear(id).subscribe({
      next: (schoolYear) => {
        this.schoolYearForm.patchValue({
          name: schoolYear.name,
          startDate: new Date(schoolYear.startDate),
          endDate: new Date(schoolYear.endDate),
          isArchived: schoolYear.isArchived
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading school year:', error);
        this.snackBar.open('Failed to load school year', 'Close', { duration: 3000 });
        this.isLoading.set(false);
        this.router.navigate(['/admin/school-years']);
      }
    });
  }

  private loadTerms(schoolYearId: number): void {
    this.isLoadingTerms.set(true);
    this.schoolYearService.getTerms(schoolYearId).subscribe({
      next: (terms) => {
        this.terms.set(terms);
        this.isLoadingTerms.set(false);
      },
      error: (error) => {
        console.error('Error loading terms:', error);
        this.snackBar.open('Failed to load terms', 'Close', { duration: 3000 });
        this.isLoadingTerms.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.schoolYearForm.invalid) {
      this.schoolYearForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    if (this.isEditMode && this.schoolYearId) {
      this.updateSchoolYear();
    } else {
      this.createSchoolYear();
    }
  }

  private createSchoolYear(): void {
    const dto: CreateSchoolYearDto = {
      name: this.schoolYearForm.value.name,
      startDate: this.schoolYearForm.value.startDate,
      endDate: this.schoolYearForm.value.endDate
    };

    this.schoolYearService.createSchoolYear(dto).subscribe({
      next: (schoolYear) => {
        this.snackBar.open('School year created successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/admin/school-years', schoolYear.id]);
      },
      error: (error) => {
        console.error('Error creating school year:', error);
        this.snackBar.open(error.error || 'Failed to create school year', 'Close', { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private updateSchoolYear(): void {
    const dto: UpdateSchoolYearDto = {
      name: this.schoolYearForm.value.name,
      startDate: this.schoolYearForm.value.startDate,
      endDate: this.schoolYearForm.value.endDate,
      isArchived: this.schoolYearForm.value.isArchived
    };

    this.schoolYearService.updateSchoolYear(this.schoolYearId!, dto).subscribe({
      next: () => {
        this.snackBar.open('School year updated successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/admin/school-years']);
      },
      error: (error) => {
        console.error('Error updating school year:', error);
        this.snackBar.open(error.error || 'Failed to update school year', 'Close', { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/school-years']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.schoolYearForm.get(fieldName);
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

  onAddTerm(): void {
    if (!this.schoolYearId) return;

    const dialogRef = this.dialog.open(TermFormDialogComponent, {
      width: '500px',
      data: {}
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.schoolYearService.addTerm(this.schoolYearId!, result).subscribe({
          next: () => {
            this.snackBar.open('Term added', 'Close', { duration: 3000 });
            this.loadTerms(this.schoolYearId!);
          },
          error: (error) => {
            console.error('Error adding term:', error);
            this.snackBar.open(error.error || 'Failed to add term', 'Close', { duration: 5000 });
          }
        });
      }
    });
  }

  onEditTerm(term: Term): void {
    if (!this.schoolYearId) return;

    const dialogRef = this.dialog.open(TermFormDialogComponent, {
      width: '500px',
      data: { term }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.schoolYearService.updateTerm(this.schoolYearId!, term.id, result).subscribe({
          next: () => {
            this.snackBar.open('Term updated', 'Close', { duration: 3000 });
            this.loadTerms(this.schoolYearId!);
          },
          error: (error) => {
            console.error('Error updating term:', error);
            this.snackBar.open(error.error || 'Failed to update term', 'Close', { duration: 5000 });
          }
        });
      }
    });
  }

  onRemoveTerm(term: Term): void {
    if (!this.schoolYearId) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Remove Term',
        message: `Remove ${term.name} from this school year?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.schoolYearService.removeTerm(this.schoolYearId!, term.id).subscribe({
          next: () => {
            this.snackBar.open('Term removed', 'Close', { duration: 3000 });
            this.loadTerms(this.schoolYearId!);
          },
          error: (error) => {
            console.error('Error removing term:', error);
            this.snackBar.open('Failed to remove term', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }
}
