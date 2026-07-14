import { Component, effect, inject, input, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { TranslocoModule } from '@jsverse/transloco';
import { LanguageService } from '../../../core/services/language.service';
import { Lesson } from '../../models/lesson.model';
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';
import { addDays, getMonday, toDateOnlyString } from '../../utils/week.util';
import { layoutDayLessons, toMinutes } from './schedule-layout.util';
import { LessonDetailDialogComponent } from '../lesson-detail-dialog/lesson-detail-dialog.component';

const PIXELS_PER_MINUTE = 1.2;
const MIN_BLOCK_HEIGHT_PX = 32;
const DAY_KEYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

interface DesktopLessonItem {
  lesson: Lesson;
  top: number;
  height: number;
  leftPercent: number;
  widthPercent: number;
}

interface DesktopDayColumn {
  day: Date;
  items: DesktopLessonItem[];
}

interface MobileDayGroup {
  day: Date;
  lessons: Lesson[];
}

@Component({
  selector: 'app-schedule-view',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatDatepickerModule, TranslocoModule, LocalizedDatePipe],
  templateUrl: './schedule-view.component.html',
  styleUrl: './schedule-view.component.scss'
})
export class ScheduleViewComponent {
  protected readonly languageService = inject(LanguageService);
  private dialog = inject(MatDialog);

  lessons = input<Lesson[]>([]);
  loading = input<boolean>(false);
  weekRangeChange = output<{ from: string; to: string }>();

  protected readonly currentWeekStart = signal<Date>(getMonday(new Date()));
  protected readonly weekEnd = computed(() => addDays(this.currentWeekStart(), 6));
  private readonly weekDays = computed(() => Array.from({ length: 7 }, (_, i) => addDays(this.currentWeekStart(), i)));

  protected readonly hasLessons = computed(() => this.lessons().length > 0);

  private readonly lessonsByDate = computed(() => {
    const map = new Map<string, Lesson[]>();
    for (const lesson of this.lessons()) {
      const arr = map.get(lesson.date) ?? [];
      arr.push(lesson);
      map.set(lesson.date, arr);
    }
    for (const arr of map.values()) {
      arr.sort((a, b) => toMinutes(a.startTime) - toMinutes(b.startTime));
    }
    return map;
  });

  private readonly weekendHasLessons = computed(() => {
    const map = this.lessonsByDate();
    return this.weekDays().slice(5).some(d => (map.get(toDateOnlyString(d))?.length ?? 0) > 0);
  });

  protected readonly visibleDays = computed(() =>
    this.weekendHasLessons() ? this.weekDays() : this.weekDays().slice(0, 5)
  );

  private readonly minutesRange = computed(() => {
    const all = this.lessons();
    if (all.length === 0) return null;
    let min = Infinity;
    let max = -Infinity;
    for (const lesson of all) {
      min = Math.min(min, toMinutes(lesson.startTime));
      max = Math.max(max, toMinutes(lesson.endTime));
    }
    return { start: Math.floor(min / 60) * 60, end: Math.ceil(max / 60) * 60 };
  });

  protected readonly gridHeightPx = computed(() => {
    const range = this.minutesRange();
    return range ? (range.end - range.start) * PIXELS_PER_MINUTE : 0;
  });

  protected readonly axisMarks = computed(() => {
    const range = this.minutesRange();
    if (!range) return [];
    const marks: { offsetPx: number; label: string }[] = [];
    for (let m = range.start; m <= range.end; m += 60) {
      marks.push({ offsetPx: (m - range.start) * PIXELS_PER_MINUTE, label: this.formatHourLabel(m) });
    }
    return marks;
  });

  protected readonly desktopDayColumns = computed<DesktopDayColumn[]>(() => {
    const range = this.minutesRange();
    if (!range) return [];
    return this.visibleDays().map(day => {
      const dateStr = toDateOnlyString(day);
      const dayLessons = this.lessons().filter(l => l.date === dateStr);
      const layout = layoutDayLessons(dayLessons);
      const items: DesktopLessonItem[] = layout.map(item => ({
        lesson: item.lesson,
        top: (item.startMinutes - range.start) * PIXELS_PER_MINUTE,
        height: Math.max((item.endMinutes - item.startMinutes) * PIXELS_PER_MINUTE, MIN_BLOCK_HEIGHT_PX),
        leftPercent: (item.colIndex / item.colCount) * 100,
        widthPercent: 100 / item.colCount
      }));
      return { day, items };
    });
  });

  protected readonly mobileDayGroups = computed<MobileDayGroup[]>(() =>
    this.visibleDays().map(day => ({
      day,
      lessons: this.lessonsByDate().get(toDateOnlyString(day)) ?? []
    }))
  );

  constructor() {
    effect(() => {
      const start = this.currentWeekStart();
      this.weekRangeChange.emit({ from: toDateOnlyString(start), to: toDateOnlyString(addDays(start, 6)) });
    });
  }

  protected dayKey(day: Date): string {
    return DAY_KEYS[day.getDay()];
  }

  protected onPrevWeek(): void {
    this.currentWeekStart.set(addDays(this.currentWeekStart(), -7));
  }

  protected onNextWeek(): void {
    this.currentWeekStart.set(addDays(this.currentWeekStart(), 7));
  }

  protected onToday(): void {
    this.currentWeekStart.set(getMonday(new Date()));
  }

  protected onDateJump(date: Date | null): void {
    if (!date) return;
    this.currentWeekStart.set(getMonday(date));
  }

  protected onLessonClick(lesson: Lesson): void {
    this.dialog.open(LessonDetailDialogComponent, { width: '480px', data: { lesson } });
  }

  private formatHourLabel(minutes: number): string {
    const hours = Math.floor(minutes / 60) % 24;
    return `${hours.toString().padStart(2, '0')}:00`;
  }
}
