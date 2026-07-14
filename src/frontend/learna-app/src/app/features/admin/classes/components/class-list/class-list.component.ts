import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ClassService } from '../../../services/class.service';
import { SchoolYearService } from '../../../services/school-year.service';
import { SchoolClass } from '../../../../../shared/models/school-class.model';
import { SchoolYear } from '../../../../../shared/models/school-year.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-class-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatSelectModule,
    MatFormFieldModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule,
    TranslocoModule
  ],
  templateUrl: './class-list.component.html',
  styleUrl: './class-list.component.scss'
})
export class ClassListComponent implements OnInit {
  private classService = inject(ClassService);
  private schoolYearService = inject(SchoolYearService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  classes = signal<SchoolClass[]>([]);
  schoolYears = signal<SchoolYear[]>([]);
  selectedSchoolYearId = signal<number | null>(null);
  displayedColumns: string[] = ['name', 'description', 'actions'];
  isLoading = signal(true);

  ngOnInit(): void {
    this.schoolYearService.getSchoolYears().subscribe({
      next: (schoolYears) => {
        this.schoolYears.set(schoolYears);
        if (schoolYears.length > 0 && this.selectedSchoolYearId() === null) {
          this.selectedSchoolYearId.set(schoolYears[0].id);
        }
        this.loadClasses();
      },
      error: () => {
        this.loadClasses();
      }
    });
  }

  onSchoolYearFilterChange(schoolYearId: number | null): void {
    this.selectedSchoolYearId.set(schoolYearId);
    this.loadClasses();
  }

  loadClasses(): void {
    this.isLoading.set(true);
    const schoolYearId = this.selectedSchoolYearId() ?? undefined;
    this.classService.getClasses(schoolYearId).subscribe({
      next: (classes) => {
        this.classes.set(classes);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading classes:', error);
        this.snackBar.open(this.transloco.translate('admin.classes.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onAddClass(): void {
    this.router.navigate(['/admin/classes/new']);
  }

  onEditClass(id: number): void {
    this.router.navigate(['/admin/classes', id]);
  }

  onDeleteClass(schoolClass: SchoolClass): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.transloco.translate('admin.classes.deleteTitle'),
        message: this.transloco.translate('admin.classes.deleteMessage', { name: schoolClass.name })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.classService.deleteClass(schoolClass.id).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.classes.deleteSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadClasses();
          },
          error: (error) => {
            console.error('Error deleting class:', error);
            this.snackBar.open(this.transloco.translate('admin.classes.deleteFailed'), this.transloco.translate('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }
}
