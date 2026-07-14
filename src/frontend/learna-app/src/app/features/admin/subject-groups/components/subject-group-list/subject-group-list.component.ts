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
import { SubjectGroupService } from '../../../services/subject-group.service';
import { SchoolYearService } from '../../../services/school-year.service';
import { SubjectService } from '../../../services/subject.service';
import { SubjectGroup } from '../../../../../shared/models/subject-group.model';
import { SchoolYear, Term } from '../../../../../shared/models/school-year.model';
import { Subject as SubjectModel } from '../../../../../shared/models/subject.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-subject-group-list',
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
    MatTooltipModule
  ],
  templateUrl: './subject-group-list.component.html',
  styleUrl: './subject-group-list.component.scss'
})
export class SubjectGroupListComponent implements OnInit {
  private subjectGroupService = inject(SubjectGroupService);
  private schoolYearService = inject(SchoolYearService);
  private subjectService = inject(SubjectService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  groups = signal<SubjectGroup[]>([]);
  schoolYears = signal<SchoolYear[]>([]);
  terms = signal<Term[]>([]);
  subjects = signal<SubjectModel[]>([]);

  selectedSchoolYearId = signal<number | null>(null);
  selectedTermId = signal<number | null>(null);

  displayedColumns: string[] = ['name', 'subject', 'teacher', 'actions'];
  isLoading = signal(false);

  ngOnInit(): void {
    this.subjectService.getSubjects().subscribe({ next: (subjects) => this.subjects.set(subjects) });
    this.schoolYearService.getSchoolYears().subscribe({
      next: (years) => {
        this.schoolYears.set(years);
        if (years.length > 0) {
          this.selectedSchoolYearId.set(years[0].id);
          this.onSchoolYearChange(years[0].id);
        }
      }
    });
  }

  onSchoolYearChange(schoolYearId: number): void {
    this.selectedSchoolYearId.set(schoolYearId);
    this.selectedTermId.set(null);
    this.groups.set([]);
    this.schoolYearService.getTerms(schoolYearId).subscribe({
      next: (terms) => {
        this.terms.set(terms);
        if (terms.length > 0) {
          this.onTermChange(terms[0].id);
        }
      }
    });
  }

  onTermChange(termId: number | null): void {
    this.selectedTermId.set(termId);
    if (termId) {
      this.loadGroups(termId);
    } else {
      this.groups.set([]);
    }
  }

  loadGroups(termId: number): void {
    this.isLoading.set(true);
    this.subjectGroupService.getSubjectGroups(termId).subscribe({
      next: (groups) => {
        this.groups.set(groups);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading subject groups:', error);
        this.snackBar.open('Failed to load subject groups', 'Close', { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  subjectName(subjectId: number): string {
    const subject = this.subjects().find(s => s.id === subjectId);
    return subject ? subject.nameEnglish : '';
  }

  onAddGroup(): void {
    this.router.navigate(['/admin/subject-groups/new']);
  }

  onEditGroup(id: number): void {
    this.router.navigate(['/admin/subject-groups', id]);
  }

  onDeleteGroup(group: SubjectGroup): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Subject Group',
        message: `Are you sure you want to delete ${group.name}? This will also remove its enrollment history.`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.subjectGroupService.deleteSubjectGroup(group.id).subscribe({
          next: () => {
            this.snackBar.open('Subject group deleted successfully', 'Close', { duration: 3000 });
            if (this.selectedTermId()) this.loadGroups(this.selectedTermId()!);
          },
          error: (error) => {
            console.error('Error deleting subject group:', error);
            this.snackBar.open('Failed to delete subject group', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }
}
