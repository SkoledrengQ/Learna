import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { TranslocoModule } from '@jsverse/transloco';
import { AssignmentsService } from '../../core/services/assignments.service';
import { AuthService } from '../../core/services/auth.service';
import { GuardianPortalService } from '../../core/services/guardian-portal.service';
import { LanguageService } from '../../core/services/language.service';
import { ChildSelectorComponent } from '../../shared/components/child-selector/child-selector.component';
import { Assignment, GuardianAssignment } from '../../shared/models/assignment.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';

@Component({selector:'app-assignments-page',standalone:true,imports:[CommonModule,RouterLink,MatCardModule,MatChipsModule,TranslocoModule,ChildSelectorComponent,LocalizedDatePipe],template:`
<main class="page"><h1>{{'assignments.title'|transloco}}</h1>
  <ng-container *ngIf="isGuardian();else studentView"><app-child-selector></app-child-selector><p *ngIf="loading()">{{'common.loading'|transloco}}</p><p class="notice" *ngIf="!loading()&&!guardianRows().length">{{'assignments.empty'|transloco}}</p>
    <mat-card class="item" *ngFor="let a of guardianRows()"><div><h2>{{a.title}}</h2><p>{{a.groupName}}</p><p>{{'assignments.deadline'|transloco}}: {{deadline(a)|localizedDate:language.activeLang():'d MMM y'}} {{a.effectiveDeadlineTime.slice(0,5)}} <strong *ngIf="a.hasExtension">{{'assignments.extended'|transloco}}</strong></p></div><mat-chip>{{('assignments.filters.'+a.status)|transloco}}</mat-chip></mat-card>
  </ng-container>
  <ng-template #studentView><p *ngIf="loading()">{{'common.loading'|transloco}}</p><section *ngIf="upcoming().length"><h2>{{'assignments.upcoming'|transloco}}</h2><ng-container *ngFor="let a of upcoming()"><ng-container *ngTemplateOutlet="card;context:{$implicit:a}"></ng-container></ng-container></section><section *ngIf="past().length"><h2>{{'assignments.past'|transloco}}</h2><ng-container *ngFor="let a of past()"><ng-container *ngTemplateOutlet="card;context:{$implicit:a}"></ng-container></ng-container></section><p class="notice" *ngIf="!loading()&&!rows().length">{{'assignments.empty'|transloco}}</p></ng-template>
  <ng-template #card let-a><mat-card class="item" [routerLink]="['/assignments',a.id]"><div><h2>{{a.title}}</h2><p>{{a.groupName}}</p><p>{{'assignments.deadline'|transloco}}: {{deadline(a)|localizedDate:language.activeLang():'d MMM y'}} {{a.effectiveDeadlineTime.slice(0,5)}} <strong *ngIf="a.hasExtension">{{'assignments.extended'|transloco}}</strong></p></div><mat-chip>{{status(a)|transloco}}</mat-chip></mat-card></ng-template>
</main>`,styles:[`.page{max-width:980px;margin:24px auto;padding:0 16px}.item{padding:16px;margin:12px 0;display:flex;align-items:center;justify-content:space-between;gap:12px}.item[routerLink]{cursor:pointer}.item h2{margin:0 0 4px}.item p{margin:3px 0}.item strong{color:#3f51b5}.notice{color:#666;padding:24px;text-align:center}`]})
export class AssignmentsPageComponent {
  private readonly service=inject(AssignmentsService);private readonly auth=inject(AuthService);readonly portal=inject(GuardianPortalService);readonly language=inject(LanguageService);
  readonly rows=signal<Assignment[]>([]);readonly guardianRows=signal<GuardianAssignment[]>([]);readonly loading=signal(true);readonly isGuardian=computed(()=>!!this.auth.getCurrentUser()?.guardianId);
  readonly upcoming=computed(()=>this.rows().filter(a=>new Date(this.deadline(a))>=new Date()));readonly past=computed(()=>this.rows().filter(a=>new Date(this.deadline(a))<new Date()));
  constructor(){if(this.isGuardian()){this.portal.loadChildren();effect(()=>{const id=this.portal.selectedChildId();if(id)this.loadGuardian(id)})}else this.service.mine().subscribe({next:a=>{this.rows.set(a);this.loading.set(false)},error:()=>this.loading.set(false)})}
  loadGuardian(id:number):void{this.loading.set(true);this.service.guardian(id).subscribe({next:a=>{this.guardianRows.set(a);this.loading.set(false)},error:()=>this.loading.set(false)})}
  deadline(a:Assignment|GuardianAssignment):string{return `${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`}
  status(a:Assignment):string{return a.ownSubmission?(a.ownSubmission.isLate?'assignments.filters.Late':'assignments.filters.Submitted'):'assignments.filters.Missing'}
}
