import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
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

@Component({selector:'app-groups-page',standalone:true,imports:[CommonModule,FormsModule,MatButtonModule,MatCardModule,MatChipsModule,MatFormFieldModule,MatIconModule,MatInputModule,MatSelectModule,MatTabsModule,TranslocoModule,MaterialListComponent,LocalizedDatePipe],template:`
<main class="page">
  <h1>{{'groups.title'|transloco}}</h1>
  <p *ngIf="loading()">{{'common.loading'|transloco}}</p><p class="notice" *ngIf="!loading()&&!groups().length">{{'groups.empty'|transloco}}</p>
  <div class="layout" *ngIf="groups().length">
    <mat-card class="group-list"><button mat-button *ngFor="let group of groups()" [class.active]="selectedGroup()?.id===group.id" (click)="selectGroup(group)"><mat-icon>groups</mat-icon>{{group.name}}</button></mat-card>
    <section class="detail" *ngIf="selectedGroup() as group"><h2>{{group.name}}</h2>
      <mat-tab-group>
        <mat-tab [label]="'groups.materials'|transloco"><app-material-list targetType="subject-group" [targetId]="group.id" [canUpload]="true"></app-material-list></mat-tab>
        <mat-tab [label]="'assignments.title'|transloco"><section class="assignments">
          <button mat-raised-button color="primary" (click)="beginCreate()"><mat-icon>add</mat-icon>{{'assignments.create'|transloco}}</button>
          <mat-card class="form" *ngIf="editing()"><h3>{{(editId()?'assignments.edit':'assignments.create')|transloco}}</h3>
            <mat-form-field><mat-label>{{'assignments.fields.title'|transloco}}</mat-label><input matInput [(ngModel)]="form.title" maxlength="300" required></mat-form-field>
            <mat-form-field><mat-label>{{'assignments.fields.description'|transloco}}</mat-label><textarea matInput [(ngModel)]="form.description" rows="4"></textarea></mat-form-field>
            <div class="form-row"><mat-form-field><mat-label>{{'assignments.fields.startDate'|transloco}}</mat-label><input matInput type="date" [(ngModel)]="form.startDate"></mat-form-field><mat-form-field><mat-label>{{'assignments.fields.deadlineDate'|transloco}}</mat-label><input matInput type="date" [(ngModel)]="form.deadlineDate" required></mat-form-field><mat-form-field><mat-label>{{'assignments.fields.deadlineTime'|transloco}}</mat-label><input matInput type="time" [(ngModel)]="form.deadlineTime" required></mat-form-field></div>
            <mat-form-field><mat-label>{{'assignments.fields.latePolicy'|transloco}}</mat-label><mat-select [(ngModel)]="form.latePolicy"><mat-option value="Block">{{'assignments.policies.Block'|transloco}}</mat-option><mat-option value="AllowMarkLate">{{'assignments.policies.AllowMarkLate'|transloco}}</mat-option></mat-select></mat-form-field>
            <label class="picker">{{'assignments.fields.attachments'|transloco}} <input type="file" multiple (change)="chooseAttachments($event)"></label><span *ngIf="queuedFiles.length">{{queuedFiles.length}} {{'assignments.filesSelected'|transloco}}</span>
            <div class="actions"><button mat-button (click)="cancelEdit()">{{'common.cancel'|transloco}}</button><button mat-raised-button color="primary" [disabled]="saving()||!form.title.trim()||!form.deadlineDate||!form.deadlineTime" (click)="save()">{{'common.save'|transloco}}</button></div>
          </mat-card>
          <p class="notice" *ngIf="!assignments().length">{{'assignments.emptyTeacher'|transloco}}</p>
          <mat-card class="assignment" *ngFor="let item of assignments()"><div class="assignment-main"><div><h3>{{item.title}}</h3><p>{{'assignments.deadline'|transloco}}: {{deadline(item)|localizedDate:language.activeLang():'d MMM y'}} {{item.deadlineTime.slice(0,5)}}</p></div><mat-chip>{{('assignments.statuses.'+item.status)|transloco}}</mat-chip><span>{{item.submittedCount}}/{{item.rosterCount}} {{'assignments.submitted'|transloco}}</span></div>
            <div class="actions"><button mat-button (click)="beginEdit(item)">{{'common.edit'|transloco}}</button><button mat-button *ngIf="item.status==='Draft'" (click)="confirmPublish(item)">{{'assignments.publish'|transloco}}</button><button mat-button *ngIf="item.status==='Published'" (click)="confirmClose(item)">{{'assignments.close'|transloco}}</button><button mat-button (click)="openSubmissions(item)">{{'assignments.viewSubmissions'|transloco}}</button><button mat-icon-button color="warn" *ngIf="item.status==='Draft'" (click)="remove(item)"><mat-icon>delete</mat-icon></button></div>
            <app-material-list *ngIf="activeAssignment()?.id===item.id" targetType="assignment" [targetId]="item.id" [canUpload]="item.status!=='Closed'"></app-material-list>
          </mat-card>
          <section class="submissions" *ngIf="activeAssignment() as active"><h2>{{'assignments.submissionsFor'|transloco:{title:active.title} }}</h2>
            <mat-form-field><mat-label>{{'assignments.filter'|transloco}}</mat-label><mat-select [ngModel]="statusFilter()" (ngModelChange)="statusFilter.set($event)"><mat-option value="All">{{'assignments.filters.All'|transloco}}</mat-option><mat-option value="Submitted">{{'assignments.filters.Submitted'|transloco}}</mat-option><mat-option value="Missing">{{'assignments.filters.Missing'|transloco}}</mat-option><mat-option value="Late">{{'assignments.filters.Late'|transloco}}</mat-option></mat-select></mat-form-field>
            <article class="student" *ngFor="let row of filteredRoster()"><div><strong>{{row.studentName}}</strong> <small>{{row.studentNumber}}</small><mat-chip>{{('assignments.filters.'+row.status)|transloco}}</mat-chip><p *ngIf="row.submission?.text">{{row.submission?.text}}</p><button mat-button *ngFor="let file of row.submission?.files" (click)="materials.download(file)"><mat-icon>download</mat-icon>{{file.originalFileName}}</button></div><button mat-button (click)="beginExtension(row)">{{'assignments.grantExtension'|transloco}}</button></article>
            <mat-card class="form" *ngIf="extensionStudent() as row"><h3>{{'assignments.extensionFor'|transloco:{name:row.studentName} }}</h3><div class="form-row"><mat-form-field><mat-label>{{'assignments.fields.deadlineDate'|transloco}}</mat-label><input matInput type="date" [(ngModel)]="extensionDate"></mat-form-field><mat-form-field><mat-label>{{'assignments.fields.deadlineTime'|transloco}}</mat-label><input matInput type="time" [(ngModel)]="extensionTime"></mat-form-field></div><mat-form-field><mat-label>{{'assignments.extensionNote'|transloco}}</mat-label><input matInput [(ngModel)]="extensionNote"></mat-form-field><div class="actions"><button mat-button (click)="extensionStudent.set(null)">{{'common.cancel'|transloco}}</button><button mat-raised-button (click)="saveExtension()">{{'common.save'|transloco}}</button></div></mat-card>
          </section>
        </section></mat-tab>
      </mat-tab-group>
    </section>
  </div>
</main>`,styles:[`.page{margin:24px}.layout{display:grid;grid-template-columns:260px 1fr;gap:20px}.group-list{padding:8px;height:max-content}.group-list button{justify-content:flex-start;width:100%}.group-list .active{background:#e8eaf6}.detail{min-width:0}.assignments{padding:20px 4px}.assignment,.form{padding:16px;margin:14px 0}.assignment-main,.actions,.form-row,.student{display:flex;align-items:center;gap:14px;flex-wrap:wrap}.assignment-main>div{flex:1}.form mat-form-field{display:block}.form-row mat-form-field{flex:1;min-width:170px}.actions{justify-content:flex-end}.student{justify-content:space-between;border-bottom:1px solid #ddd;padding:14px 4px}.student mat-chip{margin-left:10px}.picker{display:block;margin:10px 0}.notice{color:#666;padding:20px}@media(max-width:760px){.layout{grid-template-columns:1fr}.group-list{display:flex;overflow:auto}}`]})
export class GroupsPageComponent {
  private readonly service=inject(AssignmentsService); readonly materials=inject(MaterialsService); readonly language=inject(LanguageService); private readonly snack=inject(MatSnackBar); private readonly t=inject(TranslocoService); private readonly dialog=inject(MatDialog);
  readonly groups=signal<SubjectGroup[]>([]);readonly selectedGroup=signal<SubjectGroup|null>(null);readonly assignments=signal<Assignment[]>([]);readonly loading=signal(true);readonly saving=signal(false);readonly editing=signal(false);readonly editId=signal<number|null>(null);readonly activeAssignment=signal<Assignment|null>(null);readonly roster=signal<SubmissionRoster[]>([]);readonly statusFilter=signal('All');readonly extensionStudent=signal<SubmissionRoster|null>(null);
  readonly filteredRoster=computed(()=>this.statusFilter()==='All'?this.roster():this.roster().filter(r=>r.status===this.statusFilter()));
  form:AssignmentWrite=this.blank();queuedFiles:File[]=[];extensionDate='';extensionTime='';extensionNote='';
  constructor(){this.service.teacherGroups().subscribe({next:g=>{this.groups.set(g);this.loading.set(false);if(g[0])this.selectGroup(g[0])},error:()=>this.loading.set(false)})}
  selectGroup(g:SubjectGroup):void{this.selectedGroup.set(g);this.cancelEdit();this.activeAssignment.set(null);this.load()}
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
  beginExtension(r:SubmissionRoster):void{this.extensionStudent.set(r);this.extensionDate=r.extension?.extendedDeadlineDate??this.activeAssignment()!.deadlineDate;this.extensionTime=(r.extension?.extendedDeadlineTime??this.activeAssignment()!.deadlineTime).slice(0,5);this.extensionNote=r.extension?.note??''}
  saveExtension():void{const a=this.activeAssignment(),r=this.extensionStudent();if(!a||!r)return;this.service.extension(a.id,r.studentId,this.extensionDate,this.extensionTime,this.extensionNote).subscribe({next:()=>{this.extensionStudent.set(null);this.openSubmissions(a);this.message('assignments.extensionSaved')},error:()=>this.message('assignments.saveFailed')})}
  deadline(a:Assignment):string{return `${a.deadlineDate}T${a.deadlineTime}`}
  private confirm(title:string,message:string,label:string,yes:()=>void):void{this.dialog.open(ConfirmDialogComponent,{data:{title:this.t.translate(title),message:this.t.translate(message),confirmLabel:this.t.translate(label)}}).afterClosed().subscribe(ok=>{if(ok)yes()})}
  private message(key:string):void{this.snack.open(this.t.translate(key),this.t.translate('common.close'),{duration:3500})}
}
