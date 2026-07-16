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
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { SchoolYearService } from '../../../services/school-year.service';
import { SchoolYear } from '../../../../../shared/models/school-year.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { LanguageService } from '../../../../../core/services/language.service';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../../../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip.component';

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
    MatTooltipModule,
    TranslocoModule,
    LocalizedDatePipe,
    PageHeaderComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    StatusChipComponent
  ],
  templateUrl: './school-year-list.component.html',
  styleUrl: './school-year-list.component.scss'
})
export class SchoolYearListComponent implements OnInit {
  private schoolYearService = inject(SchoolYearService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);
  protected languageService = inject(LanguageService);

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
        this.snackBar.open(this.transloco.translate('admin.schoolYears.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
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
        title: this.transloco.translate('admin.schoolYears.deleteTitle'),
        message: this.transloco.translate('admin.schoolYears.deleteMessage', { name: schoolYear.name })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.schoolYearService.deleteSchoolYear(schoolYear.id).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.schoolYears.deleteSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadSchoolYears();
          },
          error: (error) => {
            console.error('Error deleting school year:', error);
            this.snackBar.open(this.transloco.translate('admin.schoolYears.deleteFailed'), this.transloco.translate('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }
}
