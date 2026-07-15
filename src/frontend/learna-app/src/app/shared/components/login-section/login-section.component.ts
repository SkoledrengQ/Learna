import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { CreateUserDialogComponent } from '../../../features/admin/users/create-user-dialog.component';
import { ResetPasswordDialogComponent } from '../../../features/admin/users/reset-password-dialog.component';
import { UserManagementService } from '../../../features/admin/services/user-management.service';
import { LinkType, ManagedUser, UserRole } from '../../models/user-management.model';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-login-section', standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatIconModule, TranslocoModule],
  template: `
    <section class="login-section">
      <h3>{{ 'users.loginSection' | transloco }}</h3>
      <p class="hint" *ngIf="!linkedId">{{ 'users.savePersonFirst' | transloco }}</p>
      <p class="hint" *ngIf="linkedId && loading()">{{ 'common.loading' | transloco }}</p>
      <div class="account-row" *ngIf="linkedId && !loading()">
        <div *ngIf="account(); else noAccount">
          <strong>{{ account()!.email }}</strong>
          <span class="status" [class.inactive]="!account()!.isActive">{{ (account()!.isActive ? 'users.active' : 'users.inactive') | transloco }}</span>
        </div>
        <ng-template #noAccount><span>{{ 'users.noLogin' | transloco }}</span></ng-template>
        <div class="actions">
          <button mat-stroked-button type="button" *ngIf="!account()" (click)="create()"><mat-icon>person_add</mat-icon>{{ 'users.createLogin' | transloco }}</button>
          <button mat-button type="button" *ngIf="account()" (click)="reset()">{{ 'users.resetPassword' | transloco }}</button>
          <button mat-button type="button" *ngIf="account()" (click)="toggleActive()">{{ (account()!.isActive ? 'users.deactivate' : 'users.reactivate') | transloco }}</button>
        </div>
      </div>
    </section>`,
  styles: [`.login-section{margin:24px 0;padding:16px;border:1px solid var(--mat-sys-outline-variant);border-radius:8px}.account-row{display:flex;justify-content:space-between;align-items:center;gap:16px;flex-wrap:wrap}.status{margin-left:12px;color:var(--mat-sys-primary)}.status.inactive{color:var(--mat-sys-error)}.actions{display:flex;gap:8px;flex-wrap:wrap}.hint{opacity:.7}`]
})
export class LoginSectionComponent implements OnChanges {
  @Input() linkType!: Exclude<LinkType, 'none'>;
  @Input() linkedId?: number;
  @Input() email = '';
  private users = inject(UserManagementService); private dialog = inject(MatDialog); private transloco = inject(TranslocoService);
  account = signal<ManagedUser | null>(null); loading = signal(false);

  ngOnChanges(): void { if (this.linkedId) this.load(); }
  private load(): void {
    this.loading.set(true);
    this.users.getUsers().subscribe({ next: users => { this.account.set(users.find(u => u.linkType === this.linkType && u.linkedId === this.linkedId) || null); this.loading.set(false); }, error: () => this.loading.set(false) });
  }
  create(): void {
    const role: UserRole = this.linkType === 'teacher' ? 'Teacher' : 'Student';
    this.dialog.open(CreateUserDialogComponent, { width: '560px', data: { role, linkType: this.linkType, linkedId: this.linkedId, email: this.email } }).afterClosed().subscribe(result => { if (result) this.account.set(result); });
  }
  reset(): void { this.dialog.open(ResetPasswordDialogComponent, { width: '480px', data: this.account() }); }
  toggleActive(): void {
    const account = this.account(); if (!account) return;
    const activating = !account.isActive;
    this.dialog.open(ConfirmDialogComponent, { width: '420px', data: { title: this.transloco.translate(activating ? 'users.reactivateTitle' : 'users.deactivateTitle'), message: this.transloco.translate(activating ? 'users.reactivateMessage' : 'users.deactivateMessage', { email: account.email }), confirmLabel: this.transloco.translate(activating ? 'users.reactivate' : 'users.deactivate') } }).afterClosed().subscribe(confirmed => {
      if (confirmed) this.users.setActive(account.id, activating).subscribe(updated => this.account.set(updated));
    });
  }
}
