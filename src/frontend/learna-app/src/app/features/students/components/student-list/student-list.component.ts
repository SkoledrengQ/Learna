import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { StudentService } from '../../services/student.service';
import { Student, getDisplayName } from '../../../../shared/models/student.model';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { LanguageService } from '../../../../core/services/language.service';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule,
    TranslocoModule,
    LocalizedDatePipe,
    PageHeaderComponent,
    EmptyStateComponent,
    LoadingStateComponent
  ],
  templateUrl: './student-list.component.html',
  styleUrl: './student-list.component.scss'
})
export class StudentListComponent implements OnInit {
  private studentService = inject(StudentService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);
  protected languageService = inject(LanguageService);

  students = signal<Student[]>([]);
  displayedColumns: string[] = ['studentId', 'displayName', 'nickname', 'email', 'enrollmentDate', 'actions'];
  isLoading = signal(true);

  readonly getDisplayName = getDisplayName;

  ngOnInit(): void {
    this.loadStudents();
  }

  loadStudents(): void {
    this.isLoading.set(true);
    this.studentService.getStudents().subscribe({
      next: (students) => {
        this.students.set(students);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading students:', error);
        this.snackBar.open(this.transloco.translate('students.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onAddStudent(): void {
    this.router.navigate(['/students/new']);
  }

  onEditStudent(id: number): void {
    this.router.navigate(['/students', id]);
  }

  onDeleteStudent(student: Student): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.transloco.translate('students.deleteTitle'),
        message: this.transloco.translate('students.deleteMessage', { name: getDisplayName(student.name) })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.studentService.deleteStudent(student.id).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('students.deleteSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadStudents();
          },
          error: (error) => {
            console.error('Error deleting student:', error);
            this.snackBar.open(this.transloco.translate('students.deleteFailed'), this.transloco.translate('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }
}
