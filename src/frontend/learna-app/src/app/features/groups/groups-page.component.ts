import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AssignmentsService } from '../../core/services/assignments.service';
import { LanguageService } from '../../core/services/language.service';
import { MaterialsService } from '../../core/services/materials.service';
import { MaterialListComponent } from '../../shared/components/material-list/material-list.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { Assignment, AssignmentWrite, LatePolicy, SubmissionRoster } from '../../shared/models/assignment.model';
import { SubjectGroup } from '../../shared/models/subject-group.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { forkJoin, of } from 'rxjs';
import { GradesService } from '../../core/services/grades.service';
import { Grade } from '../../shared/models/grade.model';
import { Enrollment } from '../../shared/models/subject-group.model';
import { GradeDialogComponent } from '../grades/grade-dialog.component';
import { getDisplayName } from '../../shared/models/student.model';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../shared/components/status-chip/status-chip.component';
import { assignmentStatusVariant, submissionStatusVariant, gradeStatusVariant } from '../../shared/utils/status-variant.util';

@Component({selector:'app-groups-page',standalone:true,imports:[CommonModule,FormsModule,MatButtonModule,MatCardModule,MatFormFieldModule,MatIconModule,MatInputModule,MatSelectModule,MatTabsModule,TranslocoModule,MaterialListComponent,LocalizedDatePipe,PageHeaderComponent,EmptyStateComponent,LoadingStateComponent,StatusChipComponent],template:`
<main class="page">
  <app-page-header [title]="'groups.title'|transloco" />
  <app-loading-state *ngIf="loading()" [message]="'common.loading'|transloco" /><app-empty-state *ngIf="!loading()&&!groups().length" icon="groups" [message]="'groups.empty'|transloco" />
  <div class="layout" *ngIf="groups().length">
    <mat-card class="group-list"><button mat-button *ngFor="let group of groups()" [class.active]="selectedGroup()?.id===group.id" (click)="selectGroup(group)"><mat-icon>groups</mat-icon>{{group.name}}</button></mat-card>
    <section class="detail" *ngIf="selectedGroup() as group"><h2>{{group.name}}</h2>
      <mat-tab-group>
        <mat-tab [label]="'groups.materials'|transloco"><app-material-list targetType="subject-group" [targetId]="group.id" [canUpload]="true"></app-material-list></mat-tab>
        <mat-tab [label]="'assignments.title'|transloco"><section class="assignments">
          <button mat-flat-button color="primary" (click)="beginCreate()"><mat-icon>add</mat-icon>{{'assignments.create'|transloco}}</button>
          <mat-card class="form" *ngIf="editing()"><h3>{{(editId()?'assignments.edit':'assignments.create')|transloco}}</h3>
            <mat-form-field><mat-label>{{'assignments.fields.title'|transloco}}</mat-label><input matInput [(ngModel)]="form.title" maxlength="300" required></mat-form-field>
            <mat-form-field><mat-label>{{'assignments.fields.description'|transloco}}</mat-label><textarea matInput [(ngModel)]="form.description" rows="4"></textarea></mat-form-field>
            <div class="form-row"><mat-form-field><mat-label>{{'assignments.fields.startDate'|transloco}}</mat-label><input matInput type="date" [(ngModel)]="form.startDate"></mat-form-field><mat-form-field><mat-label>{{'assignments.fields.deadlineDate'|transloco}}</mat-label><input matInput type="date" [(ngModel)]="form.deadlineDate" required></mat-form-field><mat-form-field><mat-label>{{'assignments.fields.deadlineTime'|transloco}}</mat-label><input matInput type="time" [(ngModel)]="form.deadlineTime" required></mat-form-field></div>
            <mat-form-field><mat-label>{{'assignments.fields.latePolicy'|transloco}}</mat-label><mat-select [(ngModel)]="form.latePolicy"><mat-option value="Block">{{'assignments.policies.Block'|transloco}}</mat-option><mat-option value="AllowMarkLate">{{'assignments.policies.AllowMarkLate'|transloco}}</mat-option></mat-select></mat-form-field>
            <label class="picker">{{'assignments.fields.attachments'|transloco}} <input type="file" multiple (change)="chooseAttachments($event)"></label><span *ngIf="queuedFiles.length">{{queuedFiles.length}} {{'assignments.filesSelected'|transloco}}</span>
            <div class="actions"><button mat-button (click)="cancelEdit()">{{'common.cancel'|transloco}}</button><button mat-flat-button color="primary" [disabled]="saving()||!form.title.trim()||!form.deadlineDate||!form.deadlineTime" (click)="save()">{{'common.save'|transloco}}</button></div>
          </mat-card>
          <p class="notice" *ngIf="!assignments().length">{{'assignments.emptyTeacher'|transloco}}</p>
          <mat-card class="assignment" *ngFor="let item of assignments()"><div class="assignment-main"><div><h3>{{item.title}}</h3><p>{{'assignments.deadline'|transloco}}: {{deadline(item)|localizedDate:language.activeLang():'d MMM y'}} {{item.deadlineTime.slice(0,5)}}</p></div><app-status-chip [variant]="assignmentStatusVariant(item.status)">{{('assignments.statuses.'+item.status)|transloco}}</app-status-chip><span>{{item.submittedCount}}/{{item.rosterCount}} {{'assignments.submitted'|transloco}}</span></div>
            <div class="actions"><button mat-button (click)="beginEdit(item)">{{'common.edit'|transloco}}</button><button mat-button *ngIf="item.status==='Draft'" (click)="confirmPublish(item)">{{'assignments.publish'|transloco}}</button><button mat-button *ngIf="item.status==='Published'" (click)="confirmClose(item)">{{'assignments.close'|transloco}}</button><button mat-button (click)="openSubmissions(item)">{{'assignments.viewSubmissions'|transloco}}</button><button mat-icon-button color="warn" *ngIf="item.status==='Draft'" (click)="remove(item)"><mat-icon>delete</mat-icon></button></div>
            <app-material-list *ngIf="activeAssignment()?.id===item.id" targetType="assignment" [targetId]="item.id" [canUpload]="item.status!=='Closed'"></app-material-list>
          </mat-card>
          <section class="submissions" *ngIf="activeAssignment() as active"><h2>{{'assignments.submissionsFor'|transloco:{title:active.title} }}</h2>
            <mat-form-field><mat-label>{{'assignments.filter'|transloco}}</mat-label><mat-select [ngModel]="statusFilter()" (ngModelChange)="statusFilter.set($event)"><mat-option value="All">{{'assignments.filters.All'|transloco}}</mat-option><mat-option value="Submitted">{{'assignments.filters.Submitted'|transloco}}</mat-option><mat-option value="Missing">{{'assignments.filters.Missing'|transloco}}</mat-option><mat-option value="Late">{{'assignments.filters.Late'|transloco}}</mat-option></mat-select></mat-form-field>
            <div class="actions"><button mat-flat-button color="primary" (click)="confirmPublishGrades(active)">{{'grades.publishAll'|transloco}}</button></div>
            <article class="student" *ngFor="let row of filteredRoster()"><div><strong>{{row.studentName}}</strong> <small>{{row.studentNumber}}</small><app-status-chip [variant]="submissionStatusVariant(row.status)">{{('assignments.filters.'+row.status)|transloco}}</app-status-chip><app-status-chip [variant]="row.submission?.grade ? gradeStatusVariant(row.submission!.grade!.status) : 'neutral'">{{('grades.states.'+(row.submission?.grade?.status||'Ungraded'))|transloco}}</app-status-chip><p *ngIf="row.submission?.text">{{row.submission?.text}}</p><button mat-button *ngFor="let file of row.submission?.files" (click)="materials.download(file)"><mat-icon>download</mat-icon>{{file.originalFileName}}</button></div><div><button mat-button *ngIf="row.submission" (click)="editSubmissionGrade(row)">{{'grades.grade'|transloco}}</button><button mat-button *ngIf="row.submission?.grade?.status==='Draft'" (click)="publishGrade(row.submission!.grade!)">{{'grades.publish'|transloco}}</button><button mat-button (click)="beginExtension(row)">{{'assignments.grantExtension'|transloco}}</button></div></article>
            <mat-card class="form" *ngIf="extensionStudent() as row"><h3>{{'assignments.extensionFor'|transloco:{name:row.studentName} }}</h3><div class="form-row"><mat-form-field><mat-label>{{'assignments.fields.deadlineDate'|transloco}}</mat-label><input matInput type="date" [(ngModel)]="extensionDate"></mat-form-field><mat-form-field><mat-label>{{'assignments.fields.deadlineTime'|transloco}}</mat-label><input matInput type="time" [(ngModel)]="extensionTime"></mat-form-field></div><mat-form-field><mat-label>{{'assignments.extensionNote'|transloco}}</mat-label><input matInput [(ngModel)]="extensionNote"></mat-form-field><div class="actions"><button mat-button (click)="extensionStudent.set(null)">{{'common.cancel'|transloco}}</button><button mat-flat-button (click)="saveExtension()">{{'common.save'|transloco}}</button></div></mat-card>
          </section>
        </section></mat-tab>
        <mat-tab [label]="'grades.gradebook'|transloco"><section class="gradebook"><div class="actions"><button mat-flat-button color="primary" (click)="addManualColumn()"><mat-icon>add</mat-icon>{{'grades.addColumn'|transloco}}</button></div><p class="notice" *ngIf="!gradeRoster().length">{{'grades.noRoster'|transloco}}</p><div class="table-wrap" *ngIf="gradeRoster().length"><table><thead><tr><th>{{'grades.student'|transloco}}</th><th *ngFor="let category of gradeCategories()">{{category}}</th></tr></thead><tbody><tr *ngFor="let enrollment of gradeRoster()"><th>{{displayStudent(enrollment)}}</th><td *ngFor="let category of gradeCategories()" (click)="editGradeCell(enrollment,category)" [class.draft]="cell(enrollment.student.id,category)?.status==='Draft'" [class.published]="cell(enrollment.student.id,category)?.status==='Published'"><ng-container *ngIf="cell(enrollment.student.id,category) as grade;else emptyCell"><strong>{{grade.score}}/{{grade.maxScore}}</strong><small>{{('grades.states.'+grade.status)|transloco}}</small><button mat-button *ngIf="grade.status==='Draft'" (click)="$event.stopPropagation();publishGrade(grade)">{{'grades.publish'|transloco}}</button></ng-container><ng-template #emptyCell><span>—</span></ng-template></td></tr></tbody></table></div></section></mat-tab>
      </mat-tab-group>
    </section>
  </div>
</main>`,styles:[`.layout{display:grid;grid-template-columns:260px 1fr;gap:20px}.group-list{padding:8px;height:max-content}.group-list button{justify-content:flex-start;width:100%}.group-list .active{background:var(--app-brand-container);color:var(--app-on-brand-container)}.detail{min-width:0}.assignments,.gradebook{padding:20px 4px}.assignment,.form{padding:16px;margin:14px 0}.assignment-main,.actions,.form-row,.student{display:flex;align-items:center;gap:14px;flex-wrap:wrap}.assignment-main>div{flex:1}.form mat-form-field{display:block}.form-row mat-form-field{flex:1;min-width:170px}.actions{justify-content:flex-end}.student{justify-content:space-between;border-bottom:1px solid var(--mat-sys-outline-variant);padding:14px 4px}.student app-status-chip{margin-left:10px}.picker{display:block;margin:10px 0}.notice{color:var(--mat-sys-on-surface-variant);padding:20px}.table-wrap{overflow:auto}table{border-collapse:collapse;min-width:100%}th,td{padding:12px;border:1px solid var(--mat-sys-outline-variant);text-align:center}tbody th{text-align:left;white-space:nowrap}td{cursor:pointer;min-width:120px}td.draft{background:#fff8e1}td.published{background:#edf7ed}td small{display:block;color:var(--mat-sys-on-surface-variant);margin-top:4px}@media(max-width:760px){.layout{grid-template-columns:1fr}.group-list{display:flex;overflow:auto}}`]})
export class GroupsPageComponent {
  private readonly service=inject(AssignmentsService); private readonly gradesService=inject(GradesService); readonly materials=inject(MaterialsService); readonly language=inject(LanguageService); private readonly snack=inject(MatSnackBar); private readonly t=inject(TranslocoService); private readonly dialog=inject(MatDialog);
  readonly groups=signal<SubjectGroup[]>([]);readonly selectedGroup=signal<SubjectGroup|null>(null);readonly assignments=signal<Assignment[]>([]);readonly loading=signal(true);readonly saving=signal(false);readonly editing=signal(false);readonly editId=signal<number|null>(null);readonly activeAssignment=signal<Assignment|null>(null);readonly roster=signal<SubmissionRoster[]>([]);readonly statusFilter=signal('All');readonly extensionStudent=signal<SubmissionRoster|null>(null);
  readonly assignmentStatusVariant=assignmentStatusVariant;readonly submissionStatusVariant=submissionStatusVariant;readonly gradeStatusVariant=gradeStatusVariant;
  readonly filteredRoster=computed(()=>this.statusFilter()==='All'?this.roster():this.roster().filter(r=>r.status===this.statusFilter()));
  readonly groupGrades=signal<Grade[]>([]);readonly gradeRoster=signal<Enrollment[]>([]);readonly manualCategories=signal<string[]>([]);readonly gradeCategories=computed(()=>Array.from(new Set([...this.groupGrades().map(g=>g.category),...this.manualCategories()])));
  form:AssignmentWrite=this.blank();queuedFiles:File[]=[];extensionDate='';extensionTime='';extensionNote='';
  constructor(){this.service.teacherGroups().subscribe({next:g=>{this.groups.set(g);this.loading.set(false);if(g[0])this.selectGroup(g[0])},error:()=>this.loading.set(false)})}
  selectGroup(g:SubjectGroup):void{this.selectedGroup.set(g);this.cancelEdit();this.activeAssignment.set(null);this.manualCategories.set([]);this.load();this.loadGradebook()}
  load():void{const id=this.selectedGroup()?.id;if(!id)return;this.service.forGroup(id).subscribe({next:a=>this.assignments.set(a),error:()=>this.message('assignments.loadFailed')})}
  blank():AssignmentWrite{const d=new Date();d.setDate(d.getDate()+1);return{title:'',description:null,startDate:null,deadlineDate:d.toISOString().slice(0,10),deadlineTime:'16:00',latePolicy:'AllowMarkLate'}}
  beginCreate():void{this.form=this.blank();this.editId.set(null);this.queuedFiles=[];this.editing.set(true)}
  beginEdit(a:Assignment):void{this.form={title:a.title,description:a.description,startDate:a.startDate,deadlineDate:a.deadlineDate,deadlineTime:a.deadlineTime.slice(0,5),latePolicy:a.latePolicy};this.editId.set(a.id);this.queuedFiles=[];this.editing.set(true)}
  cancelEdit():void{this.editing.set(false);this.editId.set(null);this.queuedFiles=[]}
  chooseAttachments(e:Event):void{this.queuedFiles=Array.from((e.target as HTMLInputElement).files??[])}
  save():void{const gid=this.selectedGroup()?.id;if(!gid)return;this.saving.set(true);const request=this.editId()?this.service.update(this.editId()!,this.form):this.service.create(gid,this.form);request.subscribe({next:a=>{const uploads=this.queuedFiles.map(f=>this.materials.uploadAssignment(a.id,f,''));(uploads.length?forkJoin(uploads):of([])).subscribe({next:()=>{this.saving.set(false);this.cancelEdit();this.message('assignments.saved');this.load()},error:()=>{this.saving.set(false);this.message('materials.uploadFailed');this.load()}})},error:()=>{this.saving.set(false);this.message('assignments.saveFailed')}})}
  confirmPublish(a:Assignment):void{this.confirm('assignments.publishTitle','assignments.publishConfirm','assignments.publish',()=>this.service.publish(a.id).subscribe(()=>this.load()))}
  confirmClose(a:Assignment):void{this.confirm('assignments.closeTitle','assignments.closeConfirm','assignments.close',()=>this.service.close(a.id).subscribe(()=>this.load()))}
  remove(a:Assignment):void{this.confirm('assignments.deleteTitle','assignments.deleteConfirm','common.delete',()=>this.service.remove(a.id).subscribe(()=>this.load()))}
  openSubmissions(a:Assignment):void{this.activeAssignment.set(a);this.service.submissions(a.id).subscribe({next:r=>this.roster.set(r),error:()=>this.message('assignments.loadFailed')})}
  editSubmissionGrade(row:SubmissionRoster):void{const submission=row.submission;if(!submission)return;this.openGradeDialog(row.studentName,submission.grade,undefined,false).subscribe(value=>{if(!value)return;this.gradesService.gradeSubmission(submission.id,value).subscribe({next:()=>{this.openSubmissions(this.activeAssignment()!);this.loadGradebook();this.message('grades.saved')},error:()=>this.message('grades.saveFailed')})})}
  publishGrade(grade:Grade):void{this.gradesService.publish(grade.id).subscribe({next:()=>{if(this.activeAssignment())this.openSubmissions(this.activeAssignment()!);this.load();this.loadGradebook()},error:()=>this.message('grades.saveFailed')})}
  confirmPublishGrades(a:Assignment):void{this.confirm('grades.publishAllTitle','grades.publishAllConfirm','grades.publishAll',()=>this.gradesService.publishAssignment(a.id).subscribe({next:()=>{this.openSubmissions(a);this.load();this.loadGradebook()},error:()=>this.message('grades.saveFailed')}))}
  loadGradebook():void{const id=this.selectedGroup()?.id;if(!id)return;forkJoin({grades:this.gradesService.forGroup(id),roster:this.gradesService.roster(id)}).subscribe({next:x=>{this.groupGrades.set(x.grades);this.gradeRoster.set(x.roster)},error:()=>this.message('grades.loadFailed')})}
  addManualColumn():void{this.openGradeDialog(this.t.translate('grades.addColumn'),null,'',true).subscribe(value=>{if(value&&!this.gradeCategories().includes(value.category))this.manualCategories.update(x=>[...x,value.category])})}
  editGradeCell(enrollment:Enrollment,category:string):void{const existing=this.cell(enrollment.student.id,category);this.openGradeDialog(this.displayStudent(enrollment),existing,category,false).subscribe(value=>{if(!value)return;const request=existing?this.gradesService.update(existing.id,value):this.gradesService.createManual(this.selectedGroup()!.id,{...value,studentId:enrollment.student.id,category});request.subscribe({next:()=>{this.loadGradebook();this.message('grades.saved')},error:()=>this.message('grades.saveFailed')})})}
  cell(studentId:number,category:string):Grade|undefined{return this.groupGrades().find(g=>g.studentId===studentId&&g.category===category)}
  displayStudent(enrollment:Enrollment):string{return getDisplayName(enrollment.student.name)}
  private openGradeDialog(title:string,grade:Grade|null|undefined,category:string|undefined,allowCategory:boolean){return this.dialog.open(GradeDialogComponent,{width:'520px',data:{title,category:grade?.category??category??this.activeAssignment()?.title??'',score:grade?.score??null,maxScore:grade?.maxScore??100,feedback:grade?.feedback??'',allowCategory}}).afterClosed()}
  beginExtension(r:SubmissionRoster):void{this.extensionStudent.set(r);this.extensionDate=r.extension?.extendedDeadlineDate??this.activeAssignment()!.deadlineDate;this.extensionTime=(r.extension?.extendedDeadlineTime??this.activeAssignment()!.deadlineTime).slice(0,5);this.extensionNote=r.extension?.note??''}
  saveExtension():void{const a=this.activeAssignment(),r=this.extensionStudent();if(!a||!r)return;this.service.extension(a.id,r.studentId,this.extensionDate,this.extensionTime,this.extensionNote).subscribe({next:()=>{this.extensionStudent.set(null);this.openSubmissions(a);this.message('assignments.extensionSaved')},error:()=>this.message('assignments.saveFailed')})}
  deadline(a:Assignment):string{return `${a.deadlineDate}T${a.deadlineTime}`}
  private confirm(title:string,message:string,label:string,yes:()=>void):void{this.dialog.open(ConfirmDialogComponent,{data:{title:this.t.translate(title),message:this.t.translate(message),confirmLabel:this.t.translate(label)}}).afterClosed().subscribe(ok=>{if(ok)yes()})}
  private message(key:string):void{this.snack.open(this.t.translate(key),this.t.translate('common.close'),{duration:3500})}
}
