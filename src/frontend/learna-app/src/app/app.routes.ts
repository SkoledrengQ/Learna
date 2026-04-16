import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { StudentListComponent } from './features/students/components/student-list/student-list.component';
import { StudentFormComponent } from './features/students/components/student-form/student-form.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/students',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/components/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'students',
    canActivate: [authGuard],
    data: { roles: ['Admin', 'Teacher', 'Student'] },
    children: [
      {
        path: '',
        component: StudentListComponent
      },
      {
        path: 'new',
        component: StudentFormComponent
      },
      {
        path: ':id',
        component: StudentFormComponent
      }
    ]
  }
];
