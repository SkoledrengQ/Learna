import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { GuardianPortalService } from '../../core/services/guardian-portal.service';
import { ChildSelectorComponent } from '../../shared/components/child-selector/child-selector.component';
import { AttendanceSummaryComponent } from './attendance-summary.component';

@Component({
  selector: 'app-attendance-page',
  standalone: true,
  imports: [CommonModule, TranslocoModule, ChildSelectorComponent, AttendanceSummaryComponent],
  template: `
    <section class="page">
      <ng-container *ngIf="portal.isGuardianLinked(); else personalAttendance">
        <p *ngIf="portal.loading()">{{ 'guardianPortal.loadingChildren' | transloco }}</p>
        <p class="notice" *ngIf="portal.loadFailed()">{{ 'guardianPortal.childrenLoadFailed' | transloco }}</p>
        <p class="notice" *ngIf="noChildren()">{{ 'guardianPortal.noChildren' | transloco }}</p>
        <app-child-selector *ngIf="portal.selectedChild()"></app-child-selector>
        <app-attendance-summary *ngIf="portal.selectedChildId() as childId" [guardianChildId]="childId"></app-attendance-summary>
      </ng-container>
      <ng-template #personalAttendance><app-attendance-summary></app-attendance-summary></ng-template>
    </section>
  `,
  styles: [`.notice { text-align:center; color:var(--mat-sys-on-surface-variant); padding:32px 16px; }`]
})
export class AttendancePageComponent {
  readonly portal = inject(GuardianPortalService);
  readonly noChildren = computed(() => !this.portal.loading() && !this.portal.loadFailed() && this.portal.children().length === 0);

  constructor() {
    effect(() => this.portal.loadChildren());
  }
}
