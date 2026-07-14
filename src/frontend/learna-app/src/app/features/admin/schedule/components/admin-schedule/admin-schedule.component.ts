import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ScheduleService } from '../../../../schedule/services/schedule.service';
import { StudentService } from '../../../../students/services/student.service';
import { TeacherService } from '../../../services/teacher.service';
import { RoomService } from '../../../services/room.service';
import { Student, getDisplayName } from '../../../../../shared/models/student.model';
import { Teacher } from '../../../../../shared/models/teacher.model';
import { Room } from '../../../../../shared/models/room.model';
import { Lesson } from '../../../../../shared/models/lesson.model';
import { ScheduleViewComponent } from '../../../../../shared/components/schedule-view/schedule-view.component';

type ViewType = 'student' | 'teacher' | 'room';

interface EntityOption {
  id: number;
  label: string;
}

@Component({
  selector: 'app-admin-schedule',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonToggleModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatSnackBarModule,
    TranslocoModule,
    ScheduleViewComponent
  ],
  templateUrl: './admin-schedule.component.html',
  styleUrl: './admin-schedule.component.scss'
})
export class AdminScheduleComponent implements OnInit {
  private scheduleService = inject(ScheduleService);
  private studentService = inject(StudentService);
  private teacherService = inject(TeacherService);
  private roomService = inject(RoomService);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  viewType = signal<ViewType>('student');
  filterText = signal('');
  selectedEntityId = signal<number | null>(null);

  private allStudents = signal<Student[]>([]);
  private allTeachers = signal<Teacher[]>([]);
  private allRooms = signal<Room[]>([]);

  lessons = signal<Lesson[]>([]);
  isLoading = signal(false);

  private currentRange: { from: string; to: string } | null = null;

  readonly options = computed<EntityOption[]>(() => {
    const term = this.filterText().trim().toLowerCase();

    if (this.viewType() === 'student') {
      return this.allStudents()
        .filter(s => !term || getDisplayName(s.name).toLowerCase().includes(term) || s.studentId.toLowerCase().includes(term))
        .map(s => ({ id: s.id, label: `${getDisplayName(s.name)} (${s.studentId})` }));
    }

    if (this.viewType() === 'teacher') {
      return this.allTeachers()
        .filter(t => !term || getDisplayName(t.name).toLowerCase().includes(term))
        .map(t => ({ id: t.id, label: getDisplayName(t.name) }));
    }

    return this.allRooms()
      .filter(r => !term || r.name.toLowerCase().includes(term))
      .map(r => ({ id: r.id, label: r.name }));
  });

  ngOnInit(): void {
    this.studentService.getStudents().subscribe({ next: (students) => this.allStudents.set(students) });
    this.teacherService.getTeachers().subscribe({ next: (teachers) => this.allTeachers.set(teachers) });
    this.roomService.getRooms().subscribe({ next: (rooms) => this.allRooms.set(rooms) });
  }

  onViewTypeChange(type: ViewType): void {
    this.viewType.set(type);
    this.filterText.set('');
    this.selectedEntityId.set(null);
    this.lessons.set([]);
  }

  onFilterInput(value: string): void {
    this.filterText.set(value);
    if (this.selectedEntityId() !== null) {
      const selected = this.options().find(o => o.id === this.selectedEntityId());
      if (!selected || selected.label !== value) {
        this.selectedEntityId.set(null);
        this.lessons.set([]);
      }
    }
  }

  onOptionSelected(option: EntityOption): void {
    this.selectedEntityId.set(option.id);
    this.filterText.set(option.label);
    this.load();
  }

  onWeekRangeChange(range: { from: string; to: string }): void {
    this.currentRange = range;
    this.load();
  }

  private load(): void {
    const id = this.selectedEntityId();
    const range = this.currentRange;
    if (id === null || !range) {
      return;
    }

    this.isLoading.set(true);
    const request =
      this.viewType() === 'student'
        ? this.scheduleService.getStudentSchedule(id, range.from, range.to)
        : this.viewType() === 'teacher'
        ? this.scheduleService.getTeacherSchedule(id, range.from, range.to)
        : this.scheduleService.getRoomSchedule(id, range.from, range.to);

    request.subscribe({
      next: (lessons) => {
        this.lessons.set(lessons);
        this.isLoading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading schedule:', error);
        this.isLoading.set(false);
        this.snackBar.open(this.transloco.translate('schedule.loadFailed'), this.transloco.translate('common.close'), { duration: 5000 });
      }
    });
  }
}
