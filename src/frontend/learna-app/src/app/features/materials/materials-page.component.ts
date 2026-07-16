import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { GuardianPortalService } from '../../core/services/guardian-portal.service';
import { MaterialsService } from '../../core/services/materials.service';
import { ChildSelectorComponent } from '../../shared/components/child-selector/child-selector.component';
import { FileResource } from '../../shared/models/file-resource.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { LanguageService } from '../../core/services/language.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

@Component({ selector:'app-materials-page', standalone:true, imports:[CommonModule, MatButtonModule, MatCardModule, MatIconModule, TranslocoModule, ChildSelectorComponent, LocalizedDatePipe, PageHeaderComponent, EmptyStateComponent, LoadingStateComponent], templateUrl:'./materials-page.component.html', styleUrl:'./materials-page.component.scss' })
export class MaterialsPageComponent {
  readonly portal=inject(GuardianPortalService); private readonly service=inject(MaterialsService);
  readonly language=inject(LanguageService);
  readonly files=signal<FileResource[]>([]); readonly loading=signal(true);
  readonly visible=computed(()=>this.portal.isGuardianLinked() ? this.files().filter(f=>f.childId===this.portal.selectedChildId()) : this.files());
  readonly groups=computed(()=>Array.from(new Map(this.visible().map(f=>[f.subjectGroupId,{id:f.subjectGroupId,name:f.subjectGroupName,files:this.visible().filter(x=>x.subjectGroupId===f.subjectGroupId)}])).values()));
  constructor(){ effect(()=>this.portal.loadChildren()); this.service.listMine().subscribe({next:f=>{this.files.set(f);this.loading.set(false)},error:()=>this.loading.set(false)}); }
  download(file:FileResource):void{this.service.download(file)}
  size(bytes:number):string{return bytes<1048576?`${(bytes/1024).toFixed(1)} KB`:`${(bytes/1048576).toFixed(1)} MB`}
}
