import { Routes } from '@angular/router';
import { StudentListComponent } from './components/student-list/student-list.component';
import { StudentFormComponent } from './components/student-form/student-form.component';

export const STUDENT_ROUTES: Routes = [
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
];
