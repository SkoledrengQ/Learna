import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/schedule',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/components/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'students',
    canActivate: [authGuard],
    data: { roles: ['Admin', 'Teacher'] },
    children: [
      {
        path: '',
        loadComponent: () => import('./features/students/components/student-list/student-list.component').then(m => m.StudentListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./features/students/components/student-form/student-form.component').then(m => m.StudentFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/students/components/student-form/student-form.component').then(m => m.StudentFormComponent)
      }
    ]
  },
  {
    path: 'schedule',
    canActivate: [authGuard],
    loadComponent: () => import('./features/schedule/components/my-schedule/my-schedule.component').then(m => m.MyScheduleComponent)
  },
  {
    path: 'attendance',
    canActivate: [authGuard],
    data: { roles: ['Student', 'Parent'] },
    loadComponent: () => import('./features/attendance/attendance-page.component').then(m => m.AttendancePageComponent)
  },
  {
    path: 'materials', canActivate: [authGuard], data: { roles: ['Student', 'Parent'] },
    loadComponent: () => import('./features/materials/materials-page.component').then(m => m.MaterialsPageComponent)
  },
  {
    path: 'groups', canActivate: [authGuard], data: { roles: ['Teacher'] },
    loadComponent: () => import('./features/groups/groups-page.component').then(m => m.GroupsPageComponent)
  },
  {
    path: 'assignments', canActivate: [authGuard], data: { roles: ['Student', 'Parent'] },
    children: [
      { path: '', loadComponent: () => import('./features/assignments/assignments-page.component').then(m => m.AssignmentsPageComponent) },
      { path: ':id', data: { roles: ['Student'] }, loadComponent: () => import('./features/assignments/assignment-detail.component').then(m => m.AssignmentDetailComponent) }
    ]
  },
  {
    path: 'grades', canActivate: [authGuard], data: { roles: ['Student', 'Parent'] },
    loadComponent: () => import('./features/grades/grades-page.component').then(m => m.GradesPageComponent)
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    data: { roles: ['Admin'] },
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./shared/components/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  }
];
