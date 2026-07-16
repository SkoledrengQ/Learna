import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'users',
    loadComponent: () => import('./users/user-list.component').then(m => m.UserListComponent)
  },
  {
    path: 'teachers',
    children: [
      {
        path: '',
        loadComponent: () => import('./teachers/components/teacher-list/teacher-list.component').then(m => m.TeacherListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./teachers/components/teacher-form/teacher-form.component').then(m => m.TeacherFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./teachers/components/teacher-form/teacher-form.component').then(m => m.TeacherFormComponent)
      }
    ]
  },
  {
    path: 'subjects',
    children: [
      {
        path: '',
        loadComponent: () => import('./subjects/components/subject-list/subject-list.component').then(m => m.SubjectListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./subjects/components/subject-form/subject-form.component').then(m => m.SubjectFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./subjects/components/subject-form/subject-form.component').then(m => m.SubjectFormComponent)
      }
    ]
  },
  {
    path: 'school-years',
    children: [
      {
        path: '',
        loadComponent: () => import('./school-years/components/school-year-list/school-year-list.component').then(m => m.SchoolYearListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./school-years/components/school-year-form/school-year-form.component').then(m => m.SchoolYearFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./school-years/components/school-year-form/school-year-form.component').then(m => m.SchoolYearFormComponent)
      }
    ]
  },
  {
    path: 'classes',
    children: [
      {
        path: '',
        loadComponent: () => import('./classes/components/class-list/class-list.component').then(m => m.ClassListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./classes/components/class-form/class-form.component').then(m => m.ClassFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./classes/components/class-form/class-form.component').then(m => m.ClassFormComponent)
      }
    ]
  },
  {
    path: 'subject-groups',
    children: [
      {
        path: '',
        loadComponent: () => import('./subject-groups/components/subject-group-list/subject-group-list.component').then(m => m.SubjectGroupListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./subject-groups/components/subject-group-form/subject-group-form.component').then(m => m.SubjectGroupFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./subject-groups/components/subject-group-form/subject-group-form.component').then(m => m.SubjectGroupFormComponent)
      }
    ]
  },
  {
    path: 'rooms',
    children: [
      {
        path: '',
        loadComponent: () => import('./rooms/components/room-list/room-list.component').then(m => m.RoomListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./rooms/components/room-form/room-form.component').then(m => m.RoomFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./rooms/components/room-form/room-form.component').then(m => m.RoomFormComponent)
      }
    ]
  },
  {
    path: 'lessons',
    loadComponent: () => import('./lessons/components/lesson-list/lesson-list.component').then(m => m.LessonListComponent)
  },
  {
    path: 'schedule',
    loadComponent: () => import('./schedule/components/admin-schedule/admin-schedule.component').then(m => m.AdminScheduleComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./settings/school-settings.component').then(m => m.SchoolSettingsComponent)
  }
];
