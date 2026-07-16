import { CommonModule } from '@angular/common';
import { Component, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AssignmentsService } from '../../core/services/assignments.service';
import { LanguageService } from '../../core/services/language.service';
import { MaterialListComponent } from '../../shared/components/material-list/material-list.component';
import { Assignment } from '../../shared/models/assignment.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../shared/components/status-chip/status-chip.component';
import { assignmentStatusVariant, submissionStatusVariant } from '../../shared/utils/status-variant.util';
import { FilePickerComponent } from '../../shared/components/file-picker/file-picker.component';

@Component({selector:'app-assignment-detail',standalone:true,imports:[CommonModule,FormsModule,RouterLink,MatButtonModule,MatCardModule,MatFormFieldModule,MatIconModule,MatInputModule,TranslocoModule,MaterialListComponent,LocalizedDatePipe,LoadingStateComponent,StatusChipComponent,FilePickerComponent],template:`
<main class="page"><a mat-button routerLink="/assignments"><mat-icon>arrow_back</mat-icon>{{'assignments.back'|transloco}}</a><app-loading-state *ngIf="loading()" [message]="'common.loading'|transloco" />
  <ng-container *ngIf="assignment() as a"><mat-card class="detail"><div class="heading"><div><h1>{{a.title}}</h1><p>{{a.groupName}}</p></div><app-status-chip [variant]="assignmentStatusVariant(a.status)">{{('assignments.statuses.'+a.status)|transloco}}</app-status-chip></div><p class="deadline">{{'assignments.deadline'|transloco}}: {{deadline(a)|localizedDate:language.activeLang():'d MMM y'}} {{a.effectiveDeadlineTime.slice(0,5)}} <strong *ngIf="a.hasExtension">{{'assignments.extended'|transloco}}</strong></p><p class="description">{{a.description}}</p><app-material-list targetType="assignment" [targetId]="a.id" [canUpload]="false"></app-material-list></mat-card>
    <mat-card class="submission"><h2>{{'assignments.yourSubmission'|transloco}}</h2><app-status-chip *ngIf="a.ownSubmission" [variant]="submissionStatusVariant(a.ownSubmission.isLate?'Late':'Submitted')">{{(a.ownSubmission.isLate?'assignments.filters.Late':'assignments.onTime')|transloco}}</app-status-chip><p *ngIf="a.ownSubmission">{{'assignments.lastSubmitted'|transloco}}: {{a.ownSubmission.submittedAt|localizedDate:language.activeLang():'d MMM y'}}</p><p *ngIf="a.ownSubmission?.text">{{a.ownSubmission?.text}}</p>
      <section class="published-grade" *ngIf="a.ownSubmission?.grade as grade"><h3>{{'grades.publishedGrade'|transloco}}</h3><strong>{{grade.score}} / {{grade.maxScore}}</strong><p *ngIf="grade.feedback">{{grade.feedback}}</p></section>
      <ng-container *ngIf="a.status!=='Closed';else closed"><mat-form-field><mat-label>{{'assignments.submissionText'|transloco}}</mat-label><textarea matInput [(ngModel)]="text" rows="5"></textarea></mat-form-field><div class="picker"><label>{{'assignments.submissionFiles'|transloco}}</label><app-file-picker [multiple]="true" (filesSelected)="choose($event)"></app-file-picker></div><div><button mat-flat-button color="primary" [disabled]="submitting()||(!files.length&&!text.trim())" (click)="submit(a)">{{(a.ownSubmission?'assignments.resubmit':'assignments.submit')|transloco}}</button></div></ng-container><ng-template #closed><p class="blocked">{{'assignments.errors.ASSIGNMENT_CLOSED'|transloco}}</p></ng-template>
    </mat-card>
  </ng-container>
</main>`,styles:[`.page{max-width:900px}.detail,.submission{padding:20px;margin:14px 0}.heading{display:flex;justify-content:space-between;align-items:center;gap:12px}.heading h1{margin:0}.deadline strong{color:var(--mat-sys-primary)}.description{white-space:pre-wrap}.submission mat-form-field{display:block;margin-top:16px}.picker{display:block;margin:12px 0}.blocked{color:var(--mat-sys-error)}.published-grade{margin:16px 0;padding:14px;border-left:4px solid #1e6b34;background:#edf7ed}.published-grade p{white-space:pre-wrap}`]})
export class AssignmentDetailComponent {
  private readonly service=inject(AssignmentsService);private readonly route=inject(ActivatedRoute);private readonly snack=inject(MatSnackBar);private readonly t=inject(TranslocoService);readonly language=inject(LanguageService);
  readonly assignmentStatusVariant=assignmentStatusVariant;readonly submissionStatusVariant=submissionStatusVariant;
  readonly assignment=signal<Assignment|null>(null);readonly loading=signal(true);readonly submitting=signal(false);text='';files:File[]=[];private readonly id=Number(this.route.snapshot.paramMap.get('id'));
  @ViewChild(FilePickerComponent) private picker?:FilePickerComponent;
  constructor(){this.load()}
  load():void{this.service.get(this.id).subscribe({next:a=>{this.assignment.set(a);this.text=a.ownSubmission?.text??'';this.loading.set(false)},error:()=>this.loading.set(false)})}
  choose(files:File[]):void{this.files=files}
  submit(a:Assignment):void{this.submitting.set(true);this.service.submit(a.id,this.files,this.text).subscribe({next:()=>{this.submitting.set(false);this.files=[];this.picker?.clear();this.message('assignments.submittedSuccess');this.load()},error:e=>{this.submitting.set(false);const code=e.error?.code??'SUBMIT_FAILED';this.message(`assignments.errors.${code}`)}})}
  deadline(a:Assignment):string{return `${a.effectiveDeadlineDate}T${a.effectiveDeadlineTime}`}
  private message(key:string):void{this.snack.open(this.t.translate(key),this.t.translate('common.close'),{duration:4500})}
}
