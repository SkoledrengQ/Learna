import { Component, Inject, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { StudentService } from '../../../../students/services/student.service';
import { Student, getDisplayName } from '../../../../../shared/models/student.model';

export interface EnrollStudentsDialogData {
  excludeStudentIds: number[];
}

@Component({
  selector: 'app-enroll-students-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    TranslocoModule
  ],
  templateUrl: './enroll-students-dialog.component.html',
  styleUrl: './enroll-students-dialog.component.scss'
})
export class EnrollStudentsDialogComponent implements OnInit {
  private studentService = inject(StudentService);

  readonly getDisplayName = getDisplayName;

  allStudents = signal<Student[]>([]);
  filterText = signal('');
  selectedStudentIds = signal<Set<number>>(new Set());

  filteredStudents = computed(() => {
    const term = this.filterText().trim().toLowerCase();
    const excluded = new Set(this.data.excludeStudentIds);
    const candidates = this.allStudents().filter(s => !excluded.has(s.id));
    if (!term) return candidates;
    return candidates.filter(s =>
      getDisplayName(s.name).toLowerCase().includes(term) ||
      s.studentId.toLowerCase().includes(term)
    );
  });

  constructor(
    public dialogRef: MatDialogRef<EnrollStudentsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EnrollStudentsDialogData
  ) {}

  ngOnInit(): void {
    this.studentService.getStudents().subscribe({
      next: (students) => this.allStudents.set(students)
    });
  }

  isSelected(studentId: number): boolean {
    return this.selectedStudentIds().has(studentId);
  }

  toggleStudent(studentId: number): void {
    const next = new Set(this.selectedStudentIds());
    if (next.has(studentId)) {
      next.delete(studentId);
    } else {
      next.add(studentId);
    }
    this.selectedStudentIds.set(next);
  }

  onFilterInput(value: string): void {
    this.filterText.set(value);
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onEnroll(): void {
    const studentIds = Array.from(this.selectedStudentIds());
    if (studentIds.length === 0) return;
    this.dialogRef.close({ studentIds });
  }
}
