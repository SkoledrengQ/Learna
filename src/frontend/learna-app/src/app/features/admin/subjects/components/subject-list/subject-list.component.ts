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
import { SubjectService } from '../../../services/subject.service';
import { Subject as SubjectModel } from '../../../../../shared/models/subject.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-subject-list',
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
  templateUrl: './subject-list.component.html',
  styleUrl: './subject-list.component.scss'
})
export class SubjectListComponent implements OnInit {
  private subjectService = inject(SubjectService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  subjects = signal<SubjectModel[]>([]);
  displayedColumns: string[] = ['code', 'nameEnglish', 'nameThai', 'actions'];
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadSubjects();
  }

  loadSubjects(): void {
    this.isLoading.set(true);
    this.subjectService.getSubjects().subscribe({
      next: (subjects) => {
        this.subjects.set(subjects);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading subjects:', error);
        this.snackBar.open('Failed to load subjects', 'Close', { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onAddSubject(): void {
    this.router.navigate(['/admin/subjects/new']);
  }

  onEditSubject(id: number): void {
    this.router.navigate(['/admin/subjects', id]);
  }

  onDeleteSubject(subject: SubjectModel): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Subject',
        message: `Are you sure you want to delete ${subject.nameEnglish}?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.subjectService.deleteSubject(subject.id).subscribe({
          next: () => {
            this.snackBar.open('Subject deleted successfully', 'Close', { duration: 3000 });
            this.loadSubjects();
          },
          error: (error) => {
            console.error('Error deleting subject:', error);
            this.snackBar.open('Failed to delete subject', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }
}
