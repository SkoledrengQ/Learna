import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  return control.get('newPassword')?.value === control.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-change-password-dialog', standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, TranslocoModule],
  template: `
    <h2 mat-dialog-title>{{ 'auth.changePassword' | transloco }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline"><mat-label>{{ 'auth.currentPassword' | transloco }}</mat-label><input matInput type="password" formControlName="currentPassword" autocomplete="current-password"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>{{ 'auth.newPassword' | transloco }}</mat-label><input matInput type="password" formControlName="newPassword" autocomplete="new-password"><mat-hint>{{ 'users.passwordHint' | transloco }}</mat-hint></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>{{ 'auth.confirmPassword' | transloco }}</mat-label><input matInput type="password" formControlName="confirmPassword" autocomplete="new-password"></mat-form-field>
        <p class="error" *ngIf="form.hasError('passwordMismatch') && form.controls.confirmPassword.touched">{{ 'auth.passwordsDoNotMatch' | transloco }}</p>
        <p class="error" *ngIf="wrongCurrent()">{{ 'auth.wrongCurrentPassword' | transloco }}</p>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end"><button mat-button [mat-dialog-close]="false">{{ 'common.cancel' | transloco }}</button><button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid || saving()">{{ 'auth.changePassword' | transloco }}</button></mat-dialog-actions>`,
  styles: [`.dialog-form{display:flex;flex-direction:column;min-width:min(420px,75vw);padding-top:8px}.error{color:var(--mat-sys-error)}`]
})
export class ChangePasswordDialogComponent {
  private fb = inject(FormBuilder); private auth = inject(AuthService); private ref = inject(MatDialogRef<ChangePasswordDialogComponent>);
  saving = signal(false); wrongCurrent = signal(false);
  form = this.fb.group({ currentPassword: ['', Validators.required], newPassword: ['', [Validators.required, Validators.minLength(8)]], confirmPassword: ['', Validators.required] }, { validators: passwordsMatch });
  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true); this.wrongCurrent.set(false);
    this.auth.changePassword(this.form.controls.currentPassword.value!, this.form.controls.newPassword.value!).subscribe({ next: () => this.ref.close(true), error: () => { this.saving.set(false); this.wrongCurrent.set(true); } });
  }
}
