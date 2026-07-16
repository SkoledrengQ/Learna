import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';

export interface GradeDialogData { title:string; category:string; score:number|null; maxScore:number; feedback:string; allowCategory:boolean; }
@Component({selector:'app-grade-dialog',standalone:true,imports:[CommonModule,FormsModule,MatDialogModule,MatButtonModule,MatFormFieldModule,MatInputModule,TranslocoModule],template:`
<h2 mat-dialog-title>{{data.title}}</h2><mat-dialog-content>
  <mat-form-field *ngIf="data.allowCategory"><mat-label>{{'grades.category'|transloco}}</mat-label><input matInput [(ngModel)]="data.category" maxlength="300"></mat-form-field>
  <ng-container *ngIf="!data.allowCategory"><div class="scores"><mat-form-field><mat-label>{{'grades.score'|transloco}}</mat-label><input matInput type="number" min="0" [(ngModel)]="data.score"></mat-form-field><span>/</span><mat-form-field><mat-label>{{'grades.maxScore'|transloco}}</mat-label><input matInput type="number" min="0.01" [(ngModel)]="data.maxScore"></mat-form-field></div>
  <mat-form-field><mat-label>{{'grades.feedback'|transloco}}</mat-label><textarea matInput rows="5" [(ngModel)]="data.feedback"></textarea></mat-form-field></ng-container>
</mat-dialog-content><mat-dialog-actions align="end"><button mat-button mat-dialog-close>{{'common.cancel'|transloco}}</button><button mat-flat-button color="primary" [disabled]="!data.category.trim()||(!data.allowCategory&&(data.score===null||data.score<0||data.maxScore<=0||data.score>data.maxScore))" (click)="save()">{{'common.save'|transloco}}</button></mat-dialog-actions>`,styles:[`mat-form-field{display:block}.scores{display:flex;align-items:center;gap:12px}.scores mat-form-field{width:150px}`]})
export class GradeDialogComponent { constructor(@Inject(MAT_DIALOG_DATA) public data:GradeDialogData,private readonly ref:MatDialogRef<GradeDialogComponent>){} save():void{this.ref.close({category:this.data.category.trim(),score:this.data.score,maxScore:this.data.maxScore,feedback:this.data.feedback.trim()||null})} }
