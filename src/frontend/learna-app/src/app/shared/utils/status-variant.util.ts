import { StatusChipVariant } from '../components/status-chip/status-chip.component';
import { AttendanceStatus } from '../models/attendance.model';
import { LessonStatus } from '../models/lesson.model';
import { GradeStatus } from '../models/grade.model';
import { AssignmentStatus } from '../models/assignment.model';

export function lessonStatusVariant(status: LessonStatus, isModified: boolean): StatusChipVariant {
  if (status === 'Cancelled') return 'danger';
  if (isModified) return 'warning';
  return 'neutral';
}

export function attendanceStatusVariant(status: AttendanceStatus): StatusChipVariant {
  switch (status) {
    case 'Present': return 'success';
    case 'Absent': return 'danger';
    case 'Late': return 'warning';
    case 'ExcusedAbsence':
    case 'Sick':
    case 'ApprovedLeave':
      return 'info';
  }
}

export function gradeStatusVariant(status: GradeStatus): StatusChipVariant {
  return status === 'Published' ? 'brand' : 'neutral';
}

export function submissionStatusVariant(status: 'Submitted' | 'Missing' | 'Late'): StatusChipVariant {
  switch (status) {
    case 'Submitted': return 'success';
    case 'Late': return 'warning';
    case 'Missing': return 'danger';
  }
}

export function assignmentStatusVariant(status: AssignmentStatus): StatusChipVariant {
  switch (status) {
    case 'Draft': return 'neutral';
    case 'Published': return 'brand';
    case 'Closed': return 'neutral';
    case 'Graded': return 'success';
    case 'Archived': return 'neutral';
  }
}
