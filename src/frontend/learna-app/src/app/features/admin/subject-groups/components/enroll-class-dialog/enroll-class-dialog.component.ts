import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { ClassService } from '../../../services/class.service';
import { SchoolYearService } from '../../../services/school-year.service';
import { SchoolClass } from '../../../../../shared/models/school-class.model';
import { SchoolYear } from '../../../../../shared/models/school-year.model';

@Component({
  selector: 'app-enroll-class-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    TranslocoModule
  ],
  templateUrl: './enroll-class-dialog.component.html',
  styleUrl: './enroll-class-dialog.component.scss'
})
export class EnrollClassDialogComponent implements OnInit {
  private classService = inject(ClassService);
  private schoolYearService = inject(SchoolYearService);

  schoolYears = signal<SchoolYear[]>([]);
  classes = signal<SchoolClass[]>([]);
  selectedSchoolYearId: number | null = null;
  selectedClassId: number | null = null;

  constructor(public dialogRef: MatDialogRef<EnrollClassDialogComponent>) {}

  ngOnInit(): void {
    this.schoolYearService.getSchoolYears().subscribe({
      next: (years) => {
        this.schoolYears.set(years);
        if (years.length > 0) {
          this.onSchoolYearChange(years[0].id);
        }
      }
    });
  }

  onSchoolYearChange(schoolYearId: number): void {
    this.selectedSchoolYearId = schoolYearId;
    this.selectedClassId = null;
    this.classService.getClasses(schoolYearId).subscribe({
      next: (classes) => this.classes.set(classes)
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onEnroll(): void {
    if (!this.selectedClassId) return;
    this.dialogRef.close({ classId: this.selectedClassId });
  }
}
