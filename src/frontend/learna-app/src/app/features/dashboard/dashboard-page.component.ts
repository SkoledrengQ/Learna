import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { forkJoin, of } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { GuardianPortalService } from '../../core/services/guardian-portal.service';
import { AnnouncementsService } from '../../core/services/announcements.service';
import { AssignmentsService } from '../../core/services/assignments.service';
import { GradesService } from '../../core/services/grades.service';
import { AttendanceService } from '../../features/attendance/attendance.service';
import { ScheduleService } from '../schedule/services/schedule.service';
import { DashboardService } from './dashboard.service';

import { Lesson } from '../../shared/models/lesson.model';
import { Assignment, GuardianAssignment } from '../../shared/models/assignment.model';
import { Grade, GradeGroup } from '../../shared/models/grade.model';
import { AttendanceSummary } from '../../shared/models/attendance.model';
import { TeacherDashboard, AdminDashboard } from './dashboard.model';

import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../shared/components/status-chip/status-chip.component';
import { DashboardCardComponent } from '../../shared/components/dashboard-card/dashboard-card.component';
import { ChildSelectorComponent } from '../../shared/components/child-selector/child-selector.component';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { lessonStatusVariant, submissionStatusVariant, gradeStatusVariant } from '../../shared/utils/status-variant.util';
import { AttendanceDialogComponent } from '../attendance/attendance-dialog.component';

const UPCOMING_DAYS = 7;
const RECENT_TAKE = 5;

type Role = 'student' | 'teacher' | 'parent' | 'admin' | 'none';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslocoModule,
    PageHeaderComponent,
    EmptyStateComponent,
    LoadingStateComponent,
    StatusChipComponent,
    DashboardCardComponent,
    ChildSelectorComponent,
    LocalizedDatePipe
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent {
  private readonly auth = inject(AuthService);
  private readonly schedule = inject(ScheduleService);
  private readonly assignments = inject(AssignmentsService);
  private readonly grades = inject(GradesService);
  private readonly attendance = inject(AttendanceService);
  private readonly dashboard = inject(DashboardService);
  private readonly dialog = inject(MatDialog);
  protected readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);
  protected readonly announcements = inject(AnnouncementsService);
  protected readonly portal = inject(GuardianPortalService);

  protected readonly lessonStatusVariant = lessonStatusVariant;
  protected readonly submissionStatusVariant = submissionStatusVariant;
  protected readonly gradeStatusVariant = gradeStatusVariant;

  private readonly user = computed(() => this.auth.getCurrentUser());
  protected readonly role = computed<Role>(() => {
    const u = this.user();
    if (!u) return 'none';
    if (u.studentId) return 'student';
    if (u.teacherId) return 'teacher';
    if (u.guardianId) return 'parent';
    if (u.roles.includes('Admin')) return 'admin';
    return 'none';
  });

  // Student
  protected readonly studentLoading = signal(false);
  protected readonly todayLessons = signal<Lesson[]>([]);
  protected readonly upcomingAssignments = signal<Assignment[]>([]);
  protected readonly missingAssignments = signal<Assignment[]>([]);
  protected readonly recentGrades = signal<Grade[]>([]);
  protected readonly attendanceSummary = signal<AttendanceSummary | null>(null);

  // Teacher
  protected readonly teacherLoading = signal(false);
  protected readonly teacherDashboard = signal<TeacherDashboard | null>(null);
  protected readonly teacherTodayLessons = computed(() => this.teacherDashboard()?.todayLessons ?? []);
  protected readonly teacherMissingAttendanceLessons = computed(() => this.teacherDashboard()?.missingAttendanceLessons ?? []);
  protected readonly teacherAwaitingGradingCount = computed(() => this.teacherDashboard()?.awaitingGradingCount ?? 0);
  protected readonly teacherRecentSubmissions = computed(() => this.teacherDashboard()?.recentSubmissions ?? []);

  // Parent
  protected readonly parentLoading = signal(false);
  protected readonly childTodayLessons = signal<Lesson[]>([]);
  protected readonly childUpcomingAssignments = signal<GuardianAssignment[]>([]);
  protected readonly childMissingAssignments = signal<GuardianAssignment[]>([]);
  protected readonly childRecentGrades = signal<Grade[]>([]);
  protected readonly childAttendance = signal<AttendanceSummary | null>(null);

  // Admin
  protected readonly adminLoading = signal(false);
  protected readonly adminDashboard = signal<AdminDashboard | null>(null);

  protected readonly recentAnnouncements = computed(() => this.announcements.feed().slice(0, RECENT_TAKE));

  constructor() {
    effect(() => {
      const role = this.role();
      if (role === 'student') this.loadStudent();
      else if (role === 'teacher') this.loadTeacher();
      else if (role === 'parent') this.portal.loadChildren();
      else if (role === 'admin') this.loadAdmin();
    });

    effect(() => {
      if (this.role() !== 'parent') return;
      const childId = this.portal.selectedChildId();
      if (childId) this.loadParent(childId);
    });
  }

  private loadStudent(): void {
    this.studentLoading.set(true);
    const today = this.dateStr(0);
    const horizon = this.dateStr(UPCOMING_DAYS);
    forkJoin({
      lessons: this.schedule.getMySchedule(today, horizon),
      assignments: this.assignments.mine(),
      grades: this.grades.mine(),
      attendance: this.attendance.getMine()
    }).subscribe({
      next: ({ lessons, assignments, grades, attendance }) => {
        this.todayLessons.set(lessons.filter(l => l.date === today));
        const now = new Date();
        const horizonDate = new Date(horizon + 'T23:59:59');
        this.upcomingAssignments.set(assignments
          .filter(a => new Date(`${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`) >= now && new Date(`${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`) <= horizonDate)
          .sort((a, b) => a.effectiveDeadlineDate.localeCompare(b.effectiveDeadlineDate)));
        this.missingAssignments.set(assignments.filter(a => !a.ownSubmission && new Date(`${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`) < now));
        this.recentGrades.set(this.flattenGrades(grades));
        this.attendanceSummary.set(attendance);
        this.studentLoading.set(false);
      },
      error: () => this.studentLoading.set(false)
    });
  }

  private loadTeacher(): void {
    this.teacherLoading.set(true);
    this.dashboard.teacher().subscribe({
      next: dto => { this.teacherDashboard.set(dto); this.teacherLoading.set(false); },
      error: () => this.teacherLoading.set(false)
    });
  }

  private loadParent(childId: number): void {
    this.parentLoading.set(true);
    const today = this.dateStr(0);
    const horizon = this.dateStr(UPCOMING_DAYS);
    forkJoin({
      lessons: this.schedule.getStudentSchedule(childId, today, horizon),
      assignments: this.assignments.guardian(childId),
      grades: this.grades.guardian(childId),
      attendance: this.attendance.getGuardianChild(childId)
    }).subscribe({
      next: ({ lessons, assignments, grades, attendance }) => {
        this.childTodayLessons.set(lessons.filter(l => l.date === today));
        const now = new Date();
        const horizonDate = new Date(horizon + 'T23:59:59');
        this.childUpcomingAssignments.set(assignments
          .filter(a => new Date(`${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`) >= now && new Date(`${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`) <= horizonDate)
          .sort((a, b) => a.effectiveDeadlineDate.localeCompare(b.effectiveDeadlineDate)));
        this.childMissingAssignments.set(assignments.filter(a => a.status === 'Missing' && a.isPast));
        this.childRecentGrades.set(this.flattenGrades(grades));
        this.childAttendance.set(attendance);
        this.parentLoading.set(false);
      },
      error: () => this.parentLoading.set(false)
    });
  }

  private loadAdmin(): void {
    this.adminLoading.set(true);
    this.dashboard.admin().subscribe({
      next: dto => { this.adminDashboard.set(dto); this.adminLoading.set(false); },
      error: () => this.adminLoading.set(false)
    });
  }

  protected openAttendance(lesson: Lesson): void {
    this.dialog.open(AttendanceDialogComponent, { width: '860px', maxWidth: '96vw', data: { lessonId: lesson.id } })
      .afterClosed().subscribe(saved => { if (saved) this.loadTeacher(); });
  }

  protected deadline(a: Assignment | GuardianAssignment): string {
    return `${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`;
  }

  private flattenGrades(groups: GradeGroup[]): Grade[] {
    return groups
      .flatMap(g => g.grades)
      .sort((a, b) => (b.publishedAt ?? b.updatedAt).localeCompare(a.publishedAt ?? a.updatedAt))
      .slice(0, RECENT_TAKE);
  }

  private dateStr(daysFromToday: number): string {
    const d = new Date();
    d.setDate(d.getDate() + daysFromToday);
    return d.toISOString().slice(0, 10);
  }
}
