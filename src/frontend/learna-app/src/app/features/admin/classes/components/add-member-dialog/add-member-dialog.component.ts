import { Component, Inject, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { TranslocoModule } from '@jsverse/transloco';
import { StudentService } from '../../../../students/services/student.service';
import { Student, getDisplayName } from '../../../../../shared/models/student.model';

export interface AddMemberDialogData {
  excludeStudentIds: number[];
}

@Component({
  selector: 'app-add-member-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatDatepickerModule,
    TranslocoModule
  ],
  templateUrl: './add-member-dialog.component.html',
  styleUrl: './add-member-dialog.component.scss'
})
export class AddMemberDialogComponent implements OnInit {
  private studentService = inject(StudentService);

  readonly getDisplayName = getDisplayName;

  allStudents = signal<Student[]>([]);
  filterText = signal('');
  selectedStudent = signal<Student | null>(null);
  joinedDate: Date = new Date();

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
    public dialogRef: MatDialogRef<AddMemberDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AddMemberDialogData
  ) {}

  ngOnInit(): void {
    this.studentService.getStudents().subscribe({
      next: (students) => this.allStudents.set(students)
    });
  }

  onStudentSelected(student: Student): void {
    this.selectedStudent.set(student);
    this.filterText.set(getDisplayName(student.name));
  }

  onFilterInput(value: string): void {
    this.filterText.set(value);
    if (this.selectedStudent() && getDisplayName(this.selectedStudent()!.name) !== value) {
      this.selectedStudent.set(null);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onAdd(): void {
    const student = this.selectedStudent();
    if (!student) return;

    this.dialogRef.close({
      studentId: student.id,
      joinedDate: this.joinedDate
    });
  }
}
