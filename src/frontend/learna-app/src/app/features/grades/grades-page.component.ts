import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { TranslocoModule } from '@jsverse/transloco';
import { GradesService } from '../../core/services/grades.service';
import { AuthService } from '../../core/services/auth.service';
import { GuardianPortalService } from '../../core/services/guardian-portal.service';
import { ChildSelectorComponent } from '../../shared/components/child-selector/child-selector.component';
import { GradeGroup } from '../../shared/models/grade.model';
import { LanguageService } from '../../core/services/language.service';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';

@Component({selector:'app-grades-page',standalone:true,imports:[CommonModule,MatCardModule,MatChipsModule,TranslocoModule,ChildSelectorComponent,LocalizedDatePipe],template:`
<main class="page"><h1>{{'grades.title'|transloco}}</h1><app-child-selector *ngIf="isGuardian()"></app-child-selector><p *ngIf="loading()">{{'common.loading'|transloco}}</p><p class="empty" *ngIf="!loading()&&!groups().length">{{'grades.empty'|transloco}}</p>
  <mat-card class="group" *ngFor="let group of groups()"><header><div><h2>{{group.groupName}}</h2><p>{{group.subjectName}}</p></div><strong>{{'grades.average'|transloco}}: {{group.averagePercentage|number:'1.0-2'}}%</strong></header>
    <article *ngFor="let grade of group.grades"><div><h3>{{grade.category}}</h3><p>{{grade.publishedAt|localizedDate:language.activeLang():'d MMM y'}}</p><p class="feedback" *ngIf="grade.feedback">{{grade.feedback}}</p></div><mat-chip>{{grade.score}} / {{grade.maxScore}}</mat-chip></article>
  </mat-card></main>`,styles:[`.page{max-width:900px;margin:24px auto;padding:0 16px}.group{padding:20px;margin:14px 0}.group header,.group article{display:flex;justify-content:space-between;align-items:center;gap:18px}.group header{border-bottom:1px solid #ddd}.group article{padding:14px 0;border-bottom:1px solid #eee}.group h2,.group h3{margin:0}.group p{margin:4px 0}.feedback{white-space:pre-wrap}.empty{text-align:center;color:#666;padding:30px}`]})
export class GradesPageComponent { private readonly service=inject(GradesService);private readonly auth=inject(AuthService);readonly portal=inject(GuardianPortalService);readonly language=inject(LanguageService);readonly groups=signal<GradeGroup[]>([]);readonly loading=signal(true);readonly isGuardian=computed(()=>!!this.auth.getCurrentUser()?.guardianId);constructor(){if(this.isGuardian()){this.portal.loadChildren();effect(()=>{const id=this.portal.selectedChildId();if(id)this.loadGuardian(id)})}else this.service.mine().subscribe({next:g=>{this.groups.set(g);this.loading.set(false)},error:()=>this.loading.set(false)})}private loadGuardian(id:number):void{this.loading.set(true);this.service.guardian(id).subscribe({next:g=>{this.groups.set(g);this.loading.set(false)},error:()=>{this.groups.set([]);this.loading.set(false)}})} }
