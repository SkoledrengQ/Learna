import { Lesson } from '../../models/lesson.model';

export interface DayLayoutItem {
  lesson: Lesson;
  colIndex: number;
  colCount: number;
  startMinutes: number;
  endMinutes: number;
}

export function toMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

/**
 * Assigns each lesson a side-by-side column within its day so overlapping
 * lessons (e.g. a force-saved double-booking) render next to each other
 * instead of stacked on top of one another.
 */
export function layoutDayLessons(lessons: Lesson[]): DayLayoutItem[] {
  const sorted = [...lessons].sort(
    (a, b) => toMinutes(a.startTime) - toMinutes(b.startTime) || toMinutes(a.endTime) - toMinutes(b.endTime)
  );

  const result: DayLayoutItem[] = [];
  let columns: Lesson[][] = [];
  let clusterItems: { lesson: Lesson; startMinutes: number; endMinutes: number }[] = [];
  let clusterEnd = -Infinity;

  const flushCluster = () => {
    for (const item of clusterItems) {
      const colIndex = columns.findIndex(col => col.includes(item.lesson));
      result.push({ lesson: item.lesson, colIndex, colCount: columns.length, startMinutes: item.startMinutes, endMinutes: item.endMinutes });
    }
    columns = [];
    clusterItems = [];
    clusterEnd = -Infinity;
  };

  for (const lesson of sorted) {
    const startMinutes = toMinutes(lesson.startTime);
    const endMinutes = toMinutes(lesson.endTime);

    if (columns.length > 0 && startMinutes >= clusterEnd) {
      flushCluster();
    }

    let placed = false;
    for (const col of columns) {
      const last = col[col.length - 1];
      if (toMinutes(last.endTime) <= startMinutes) {
        col.push(lesson);
        placed = true;
        break;
      }
    }
    if (!placed) {
      columns.push([lesson]);
    }

    clusterItems.push({ lesson, startMinutes, endMinutes });
    clusterEnd = Math.max(clusterEnd, endMinutes);
  }
  flushCluster();

  return result;
}
