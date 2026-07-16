import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LanguageService } from '../../../core/services/language.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { ManagedUser } from '../../../shared/models/user-management.model';
import { LocalizedDatePipe } from '../../../shared/pipes/localized-date.pipe';
import { UserManagementService } from '../services/user-management.service';
import { CreateUserDialogComponent } from './create-user-dialog.component';
import { ResetPasswordDialogComponent } from './reset-password-dialog.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../../shared/components/status-chip/status-chip.component';

@Component({
  selector: 'app-user-list', standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatTableModule, TranslocoModule, LocalizedDatePipe, PageHeaderComponent, EmptyStateComponent, LoadingStateComponent, StatusChipComponent],
  template: `
    <app-page-header [title]="'users.title' | transloco">
      <mat-form-field pageActions appearance="outline" subscriptSizing="dynamic"><mat-label>{{ 'users.search' | transloco }}</mat-label><input matInput [(ngModel)]="search" (keyup.enter)="load()"><button mat-icon-button matSuffix (click)="load()"><mat-icon>search</mat-icon></button></mat-form-field>
      <button pageActions mat-flat-button color="primary" (click)="create()"><mat-icon>person_add</mat-icon>{{ 'users.createLogin' | transloco }}</button>
    </app-page-header>

    <app-loading-state *ngIf="loading()" [message]="'common.loading' | transloco" />

    <mat-card *ngIf="!loading()">
      <mat-card-content>
        <app-empty-state *ngIf="users().length === 0" icon="manage_accounts" [message]="'users.noUsers' | transloco" />
        <div class="table-wrap" *ngIf="users().length > 0">
          <table mat-table [dataSource]="users()" class="app-table">
            <ng-container matColumnDef="email"><th mat-header-cell *matHeaderCellDef>{{ 'users.email' | transloco }}</th><td mat-cell *matCellDef="let user">{{ user.email }}</td></ng-container>
            <ng-container matColumnDef="role"><th mat-header-cell *matHeaderCellDef>{{ 'users.role' | transloco }}</th><td mat-cell *matCellDef="let user">{{ user.roles.join(', ') }}</td></ng-container>
            <ng-container matColumnDef="person"><th mat-header-cell *matHeaderCellDef>{{ 'users.linkedPerson' | transloco }}</th><td mat-cell *matCellDef="let user"><span *ngIf="user.linkedName; else none">{{ user.linkedName }} ({{ ('users.linkTypes.' + user.linkType) | transloco }})</span><ng-template #none>—</ng-template></td></ng-container>
            <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>{{ 'users.status' | transloco }}</th><td mat-cell *matCellDef="let user"><app-status-chip [variant]="user.isActive ? 'success' : 'neutral'">{{ (user.isActive ? 'users.active' : 'users.inactive') | transloco }}</app-status-chip></td></ng-container>
            <ng-container matColumnDef="lastLogin"><th mat-header-cell *matHeaderCellDef>{{ 'users.lastLogin' | transloco }}</th><td mat-cell *matCellDef="let user">{{ user.lastLoginAt ? (user.lastLoginAt | localizedDate:language.activeLang():'mediumDate') : ('users.never' | transloco) }}</td></ng-container>
            <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>{{ 'common.actions' | transloco }}</th><td mat-cell *matCellDef="let user" class="actions-cell"><button mat-button (click)="reset(user)">{{ 'users.resetPassword' | transloco }}</button><button mat-button (click)="toggleActive(user)">{{ (user.isActive ? 'users.deactivate' : 'users.reactivate') | transloco }}</button></td></ng-container>
            <tr mat-header-row *matHeaderRowDef="columns"></tr><tr mat-row *matRowDef="let row; columns: columns"></tr>
          </table>
        </div>
      </mat-card-content>
    </mat-card>`,
  styles: [`.table-wrap{overflow:auto}table{width:100%}td:last-child{white-space:nowrap}`]
})
export class UserListComponent {
  private service = inject(UserManagementService); private dialog = inject(MatDialog); private transloco = inject(TranslocoService);
  protected language = inject(LanguageService);
  users = signal<ManagedUser[]>([]); loading = signal(false); search = ''; columns = ['email', 'role', 'person', 'status', 'lastLogin', 'actions'];
  constructor() { this.load(); }
  load(): void { this.loading.set(true); this.service.getUsers(this.search).subscribe({ next: users => { this.users.set(users); this.loading.set(false); }, error: () => this.loading.set(false) }); }
  create(): void { this.dialog.open(CreateUserDialogComponent, { width: '560px', data: {} }).afterClosed().subscribe(result => { if (result) this.load(); }); }
  reset(user: ManagedUser): void { this.dialog.open(ResetPasswordDialogComponent, { width: '480px', data: user }); }
  toggleActive(user: ManagedUser): void {
    const activating = !user.isActive;
    this.dialog.open(ConfirmDialogComponent, { width: '420px', data: { title: this.transloco.translate(activating ? 'users.reactivateTitle' : 'users.deactivateTitle'), message: this.transloco.translate(activating ? 'users.reactivateMessage' : 'users.deactivateMessage', { email: user.email }), confirmLabel: this.transloco.translate(activating ? 'users.reactivate' : 'users.deactivate') } }).afterClosed().subscribe(ok => { if (ok) this.service.setActive(user.id, activating).subscribe(() => this.load()); });
  }
}
