import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SchoolYearService } from '../../../services/school-year.service';
import { SchoolYear } from '../../../../../shared/models/school-year.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-school-year-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './school-year-list.component.html',
  styleUrl: './school-year-list.component.scss'
})
export class SchoolYearListComponent implements OnInit {
  private schoolYearService = inject(SchoolYearService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  schoolYears = signal<SchoolYear[]>([]);
  displayedColumns: string[] = ['name', 'startDate', 'endDate', 'isArchived', 'actions'];
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadSchoolYears();
  }

  loadSchoolYears(): void {
    this.isLoading.set(true);
    this.schoolYearService.getSchoolYears().subscribe({
      next: (schoolYears) => {
        this.schoolYears.set(schoolYears);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading school years:', error);
        this.snackBar.open('Failed to load school years', 'Close', { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString();
  }

  onAddSchoolYear(): void {
    this.router.navigate(['/admin/school-years/new']);
  }

  onEditSchoolYear(id: number): void {
    this.router.navigate(['/admin/school-years', id]);
  }

  onDeleteSchoolYear(schoolYear: SchoolYear): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete School Year',
        message: `Are you sure you want to delete ${schoolYear.name}? This will also delete its terms.`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.schoolYearService.deleteSchoolYear(schoolYear.id).subscribe({
          next: () => {
            this.snackBar.open('School year deleted successfully', 'Close', { duration: 3000 });
            this.loadSchoolYears();
          },
          error: (error) => {
            console.error('Error deleting school year:', error);
            this.snackBar.open('Failed to delete school year', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }
}
