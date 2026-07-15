import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { GuardianPortalService } from '../../../core/services/guardian-portal.service';
import { LanguageService } from '../../../core/services/language.service';
import { GuardianChild } from '../../models/guardian.model';

@Component({
  selector: 'app-child-selector',
  standalone: true,
  imports: [CommonModule, MatFormFieldModule, MatSelectModule, TranslocoModule],
  template: `
    <section class="child-context" *ngIf="portal.selectedChild() as child">
      <span class="context-label">{{ 'guardianPortal.viewingFor' | transloco }}</span>
      <strong>{{ displayName(child) }}</strong>
      <span class="details">{{ relationship(child.relationship) }}<ng-container *ngIf="child.activeClassName"> · {{ child.activeClassName }}</ng-container></span>

      <mat-form-field appearance="outline" *ngIf="portal.children().length > 1">
        <mat-label>{{ 'guardianPortal.selectChild' | transloco }}</mat-label>
        <mat-select [value]="portal.selectedChildId()" (selectionChange)="portal.selectChild($event.value)">
          <mat-option *ngFor="let option of portal.children()" [value]="option.id">{{ displayName(option) }}</mat-option>
        </mat-select>
      </mat-form-field>
    </section>
  `,
  styles: [`
    .child-context { display:flex; align-items:center; gap:10px; flex-wrap:wrap; padding:14px 16px; margin-bottom:16px; border-radius:8px; background:#eef4ff; border-left:4px solid #3f51b5; }
    .context-label,.details { color:#555; }
    mat-form-field { min-width:240px; margin-left:auto; }
    @media(max-width:600px){ mat-form-field { width:100%; margin-left:0; } }
  `]
})
export class ChildSelectorComponent {
  readonly portal = inject(GuardianPortalService);
  private readonly language = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);
  private readonly relationshipKeys = new Set(['mother', 'father', 'guardian']);
  readonly activeLanguage = computed(() => this.language.activeLang());

  displayName(child: GuardianChild): string {
    const name = child.name;
    const first = this.activeLanguage() === 'en' && name.firstNameEnglish ? name.firstNameEnglish : name.firstName;
    const last = this.activeLanguage() === 'en' && name.lastNameEnglish ? name.lastNameEnglish : name.lastName;
    const nickname = name.nickname ? ` (${name.nickname})` : '';
    return `${first} ${last}${nickname}`.trim();
  }

  relationship(value: string): string {
    const key = value.trim().toLowerCase();
    return this.relationshipKeys.has(key) ? this.transloco.translate(`guardianPortal.relationships.${key}`) : value;
  }
}
