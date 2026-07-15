import { CommonModule } from '@angular/common';
import { Component, Inject, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LinkType, LinkablePerson, UserRole } from '../../../shared/models/user-management.model';
import { UserManagementService } from '../services/user-management.service';

export interface CreateUserDialogData {
  role?: UserRole;
  linkType?: LinkType;
  linkedId?: number;
  email?: string;
}

@Component({
  selector: 'app-create-user-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatAutocompleteModule, TranslocoModule],
  template: `
    <h2 mat-dialog-title>{{ 'users.createTitle' | transloco }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'users.role' | transloco }}</mat-label>
          <mat-select formControlName="role" (selectionChange)="onRoleChange($event.value)">
            <mat-option value="Teacher">{{ 'users.roles.teacher' | transloco }}</mat-option>
            <mat-option value="Student">{{ 'users.roles.student' | transloco }}</mat-option>
            <mat-option value="Parent">{{ 'users.roles.parent' | transloco }}</mat-option>
            <mat-option value="Admin">{{ 'users.roles.admin' | transloco }}</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" *ngIf="form.value.role !== 'Admin'">
          <mat-label>{{ 'users.linkedPerson' | transloco }}</mat-label>
          <input matInput formControlName="person" [matAutocomplete]="peopleAuto" [placeholder]="'users.personSearch' | transloco">
          <mat-autocomplete #peopleAuto="matAutocomplete" [displayWith]="displayPerson" requireSelection (optionSelected)="onPersonSelected($event.option.value)">
            <mat-option *ngFor="let person of filteredPeople()" [value]="person">{{ person.name }} <span *ngIf="person.email">· {{ person.email }}</span></mat-option>
          </mat-autocomplete>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>{{ 'users.email' | transloco }}</mat-label>
          <input matInput type="email" formControlName="email" autocomplete="off">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'users.temporaryPassword' | transloco }}</mat-label>
          <input matInput type="password" formControlName="password" autocomplete="new-password">
          <mat-hint>{{ 'users.passwordHint' | transloco }}</mat-hint>
        </mat-form-field>
        <p class="error" *ngIf="error()">{{ 'users.saveFailed' | transloco }}</p>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ 'common.cancel' | transloco }}</button>
      <button mat-raised-button color="primary" (click)="submit()" [disabled]="form.invalid || saving()">{{ 'users.createLogin' | transloco }}</button>
    </mat-dialog-actions>
  `,
  styles: [`.dialog-form{display:flex;flex-direction:column;min-width:min(480px,75vw);padding-top:8px}.error{color:var(--mat-sys-error)}`]
})
export class CreateUserDialogComponent {
  private fb = inject(FormBuilder);
  private users = inject(UserManagementService);
  private ref = inject(MatDialogRef<CreateUserDialogComponent>);
  readonly data = inject<CreateUserDialogData>(MAT_DIALOG_DATA);
  people = signal<LinkablePerson[]>([]);
  filteredPeople = signal<LinkablePerson[]>([]);
  saving = signal(false);
  error = signal(false);

  form = this.fb.group({
    role: [this.data.role || 'Teacher' as UserRole, Validators.required],
    person: [null as LinkablePerson | null],
    email: [this.data.email || '', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  constructor() {
    const personControl = this.form.controls.person;
    personControl.valueChanges.subscribe(value => {
      const raw: unknown = value;
      const query = typeof raw === 'string' ? raw.toLowerCase() : '';
      this.filteredPeople.set(this.people().filter(p => !query || `${p.name} ${p.email || ''}`.toLowerCase().includes(query)));
    });
    this.onRoleChange(this.form.controls.role.value!);
  }

  onRoleChange(role: UserRole): void {
    if (role === 'Admin') {
      this.form.controls.person.clearValidators();
      this.form.controls.person.setValue(null);
      this.people.set([]);
      this.filteredPeople.set([]);
    } else {
      this.form.controls.person.setValidators(Validators.required);
      const linkType = this.linkTypeForRole(role);
      this.users.getLinkable(linkType).subscribe(people => {
        this.people.set(people);
        this.filteredPeople.set(people);
        const preset = people.find(p => p.id === this.data.linkedId && p.linkType === this.data.linkType);
        if (preset) this.form.controls.person.setValue(preset);
      });
    }
    this.form.controls.person.updateValueAndValidity();
  }

  onPersonSelected(person: LinkablePerson): void {
    if (!this.form.controls.email.value && person.email) this.form.controls.email.setValue(person.email);
  }

  displayPerson(person: LinkablePerson | null): string { return person?.name || ''; }

  submit(): void {
    if (this.form.invalid) return;
    const role = this.form.controls.role.value!;
    const person = this.form.controls.person.value;
    this.saving.set(true);
    this.error.set(false);
    this.users.createUser({
      email: this.form.controls.email.value!, initialPassword: this.form.controls.password.value!, role,
      linkType: role === 'Admin' ? 'none' : this.linkTypeForRole(role), linkedId: role === 'Admin' ? null : person!.id
    }).subscribe({ next: user => this.ref.close(user), error: () => { this.saving.set(false); this.error.set(true); } });
  }

  private linkTypeForRole(role: UserRole): LinkType {
    return role === 'Teacher' ? 'teacher' : role === 'Student' ? 'student' : role === 'Parent' ? 'guardian' : 'none';
  }
}
