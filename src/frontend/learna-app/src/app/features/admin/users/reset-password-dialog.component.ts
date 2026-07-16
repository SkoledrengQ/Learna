import { CommonModule } from '@angular/common';
import { Component, Inject, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoModule } from '@jsverse/transloco';
import { ManagedUser } from '../../../shared/models/user-management.model';
import { UserManagementService } from '../services/user-management.service';

@Component({
  selector: 'app-reset-password-dialog', standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, TranslocoModule],
  template: `
    <h2 mat-dialog-title>{{ 'users.resetPasswordTitle' | transloco }}</h2>
    <mat-dialog-content>
      <p>{{ 'users.resetPasswordMessage' | transloco:{ email: data.email } }}</p>
      <div class="password" *ngIf="temporaryPassword()"><code>{{ temporaryPassword() }}</code><p>{{ 'users.passwordShownOnce' | transloco }}</p></div>
      <p class="error" *ngIf="failed()">{{ 'users.resetFailed' | transloco }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.close' | transloco }}</button>
      <button mat-flat-button color="primary" *ngIf="!temporaryPassword()" (click)="reset()" [disabled]="loading()">{{ 'users.resetPassword' | transloco }}</button>
    </mat-dialog-actions>`,
  styles: [`.password{padding:16px;background:var(--mat-sys-surface-container);border-radius:8px;text-align:center}.password code{font-size:1.35rem;user-select:all}.error{color:var(--mat-sys-error)}`]
})
export class ResetPasswordDialogComponent {
  private users = inject(UserManagementService);
  temporaryPassword = signal(''); loading = signal(false); failed = signal(false);
  constructor(@Inject(MAT_DIALOG_DATA) public data: ManagedUser) {}
  reset(): void {
    this.loading.set(true); this.failed.set(false);
    this.users.resetPassword(this.data.id).subscribe({ next: r => { this.temporaryPassword.set(r.temporaryPassword); this.loading.set(false); }, error: () => { this.failed.set(true); this.loading.set(false); } });
  }
}
