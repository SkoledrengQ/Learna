import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { TranslocoModule } from '@jsverse/transloco';
import { AssignmentsService } from '../../core/services/assignments.service';
import { AuthService } from '../../core/services/auth.service';
import { GuardianPortalService } from '../../core/services/guardian-portal.service';
import { LanguageService } from '../../core/services/language.service';
import { ChildSelectorComponent } from '../../shared/components/child-selector/child-selector.component';
import { Assignment, GuardianAssignment } from '../../shared/models/assignment.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../shared/components/status-chip/status-chip.component';
import { submissionStatusVariant } from '../../shared/utils/status-variant.util';

@Component({selector:'app-assignments-page',standalone:true,imports:[CommonModule,RouterLink,MatCardModule,TranslocoModule,ChildSelectorComponent,LocalizedDatePipe,PageHeaderComponent,EmptyStateComponent,LoadingStateComponent,StatusChipComponent],template:`
<main class="page"><app-page-header [title]="'assignments.title'|transloco" />
  <ng-container *ngIf="isGuardian();else studentView"><app-child-selector></app-child-selector><app-loading-state *ngIf="loading()" [message]="'common.loading'|transloco" /><app-empty-state *ngIf="!loading()&&!guardianRows().length" icon="assignment" [message]="'assignments.empty'|transloco" />
    <mat-card class="item" *ngFor="let a of guardianRows()"><div><h2>{{a.title}}</h2><p>{{a.groupName}}</p><p>{{'assignments.deadline'|transloco}}: {{deadline(a)|localizedDate:language.activeLang():'d MMM y'}} {{a.effectiveDeadlineTime.slice(0,5)}} <strong *ngIf="a.hasExtension">{{'assignments.extended'|transloco}}</strong></p></div><app-status-chip [variant]="submissionStatusVariant(a.status)">{{('assignments.filters.'+a.status)|transloco}}</app-status-chip></mat-card>
  </ng-container>
  <ng-template #studentView><app-loading-state *ngIf="loading()" [message]="'common.loading'|transloco" /><section *ngIf="upcoming().length"><h2>{{'assignments.upcoming'|transloco}}</h2><ng-container *ngFor="let a of upcoming()"><ng-container *ngTemplateOutlet="card;context:{$implicit:a}"></ng-container></ng-container></section><section *ngIf="past().length"><h2>{{'assignments.past'|transloco}}</h2><ng-container *ngFor="let a of past()"><ng-container *ngTemplateOutlet="card;context:{$implicit:a}"></ng-container></ng-container></section><app-empty-state *ngIf="!loading()&&!rows().length" icon="assignment" [message]="'assignments.empty'|transloco" /></ng-template>
  <ng-template #card let-a><mat-card class="item" [routerLink]="['/assignments',a.id]"><div><h2>{{a.title}}</h2><p>{{a.groupName}}</p><p>{{'assignments.deadline'|transloco}}: {{deadline(a)|localizedDate:language.activeLang():'d MMM y'}} {{a.effectiveDeadlineTime.slice(0,5)}} <strong *ngIf="a.hasExtension">{{'assignments.extended'|transloco}}</strong></p></div><app-status-chip [variant]="submissionStatusVariant(statusKey(a))">{{status(a)|transloco}}</app-status-chip></mat-card></ng-template>
</main>`,styles:[`.page{max-width:980px}.item{padding:16px;margin:12px 0;display:flex;align-items:center;justify-content:space-between;gap:12px}.item[routerLink]{cursor:pointer}.item h2{margin:0 0 4px}.item p{margin:3px 0}.item strong{color:var(--mat-sys-primary)}`]})
export class AssignmentsPageComponent {
  private readonly service=inject(AssignmentsService);private readonly auth=inject(AuthService);readonly portal=inject(GuardianPortalService);readonly language=inject(LanguageService);
  readonly rows=signal<Assignment[]>([]);readonly guardianRows=signal<GuardianAssignment[]>([]);readonly loading=signal(true);readonly isGuardian=computed(()=>!!this.auth.getCurrentUser()?.guardianId);
  readonly upcoming=computed(()=>this.rows().filter(a=>new Date(this.deadline(a))>=new Date()));readonly past=computed(()=>this.rows().filter(a=>new Date(this.deadline(a))<new Date()));
  readonly submissionStatusVariant=submissionStatusVariant;
  constructor(){if(this.isGuardian()){this.portal.loadChildren();effect(()=>{const id=this.portal.selectedChildId();if(id)this.loadGuardian(id)})}else this.service.mine().subscribe({next:a=>{this.rows.set(a);this.loading.set(false)},error:()=>this.loading.set(false)})}
  loadGuardian(id:number):void{this.loading.set(true);this.service.guardian(id).subscribe({next:a=>{this.guardianRows.set(a);this.loading.set(false)},error:()=>this.loading.set(false)})}
  deadline(a:Assignment|GuardianAssignment):string{return `${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`}
  statusKey(a:Assignment):'Submitted'|'Missing'|'Late'{return a.ownSubmission?(a.ownSubmission.isLate?'Late':'Submitted'):'Missing'}
  status(a:Assignment):string{return 'assignments.filters.'+this.statusKey(a)}
}
