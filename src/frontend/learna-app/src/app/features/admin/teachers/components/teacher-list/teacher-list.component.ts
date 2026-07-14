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
import { TeacherService } from '../../../services/teacher.service';
import { Teacher } from '../../../../../shared/models/teacher.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-teacher-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './teacher-list.component.html',
  styleUrl: './teacher-list.component.scss'
})
export class TeacherListComponent implements OnInit {
  private teacherService = inject(TeacherService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  teachers = signal<Teacher[]>([]);
  displayedColumns: string[] = ['employeeId', 'name', 'email', 'phoneNumber', 'actions'];
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadTeachers();
  }

  loadTeachers(): void {
    this.isLoading.set(true);
    this.teacherService.getTeachers().subscribe({
      next: (teachers) => {
        this.teachers.set(teachers);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading teachers:', error);
        this.snackBar.open('Failed to load teachers', 'Close', { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  getDisplayName(teacher: Teacher): string {
    return `${teacher.name.firstName} ${teacher.name.lastName}`;
  }

  onAddTeacher(): void {
    this.router.navigate(['/admin/teachers/new']);
  }

  onEditTeacher(id: number): void {
    this.router.navigate(['/admin/teachers', id]);
  }

  onDeleteTeacher(teacher: Teacher): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Teacher',
        message: `Are you sure you want to delete ${this.getDisplayName(teacher)}?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.teacherService.deleteTeacher(teacher.id).subscribe({
          next: () => {
            this.snackBar.open('Teacher deleted successfully', 'Close', { duration: 3000 });
            this.loadTeachers();
          },
          error: (error) => {
            console.error('Error deleting teacher:', error);
            this.snackBar.open('Failed to delete teacher', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }
}
