import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
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
  }
];
