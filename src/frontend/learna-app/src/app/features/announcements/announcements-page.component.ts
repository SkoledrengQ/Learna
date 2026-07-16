import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AnnouncementsService } from '../../core/services/announcements.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { MaterialsService } from '../../core/services/materials.service';
import { Announcement, AnnouncementAudienceType, AnnouncementTarget, AnnouncementWrite } from '../../shared/models/announcement.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../shared/components/status-chip/status-chip.component';

@Component({
  selector: 'app-announcements-page',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatSnackBarModule, TranslocoModule, LocalizedDatePipe, PageHeaderComponent, EmptyStateComponent, LoadingStateComponent, StatusChipComponent],
  template: `
    <main class="announcements-page">
      <app-page-header [title]="'announcements.title' | transloco" [subtitle]="'announcements.unreadCount' | transloco:{count: announcements.unreadCount()}">
        <button pageActions mat-flat-button color="primary" *ngIf="canCompose()" (click)="startCreate()">
          <mat-icon>add</mat-icon>{{ 'announcements.compose' | transloco }}
        </button>
      </app-page-header>

      <section class="layout">
        <div class="list">
          <app-loading-state *ngIf="loading()" [message]="'common.loading' | transloco" />
          <app-empty-state *ngIf="!loading() && feed().length === 0" icon="campaign" [message]="'announcements.empty' | transloco" />
          <button class="announcement-row" type="button" *ngFor="let item of feed()" [class.unread]="!item.isRead" [class.selected]="selected()?.id === item.id" (click)="open(item)">
            <span class="row-title">{{ item.title }}</span>
            <span class="meta">
              {{ audienceLabel(item) }} · {{ item.publishAt | localizedDate:language.activeLang():'mediumDate' }}
            </span>
            <span class="preview">{{ item.body }}</span>
            <span class="flags">
              <app-status-chip variant="brand" *ngIf="!item.isRead">{{ 'announcements.unread' | transloco }}</app-status-chip>
              <app-status-chip variant="info" *ngIf="item.isFuture">{{ 'announcements.future' | transloco }}</app-status-chip>
              <app-status-chip variant="neutral" *ngIf="item.isExpired">{{ 'announcements.expired' | transloco }}</app-status-chip>
            </span>
          </button>
        </div>

        <mat-card class="detail" *ngIf="selected() as item">
          <mat-card-header>
            <mat-card-title>{{ item.title }}</mat-card-title>
            <mat-card-subtitle>{{ audienceLabel(item) }} · {{ item.authorName }} · {{ item.publishAt | localizedDate:language.activeLang():'mediumDate' }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="detail-flags">
              <app-status-chip variant="info" *ngIf="item.isFuture">{{ 'announcements.future' | transloco }}</app-status-chip>
              <app-status-chip variant="neutral" *ngIf="item.isExpired">{{ 'announcements.expired' | transloco }}</app-status-chip>
            </div>
            <div class="body">{{ item.body }}</div>
            <h2>{{ 'announcements.attachments' | transloco }}</h2>
            <p class="empty" *ngIf="item.files.length === 0">{{ 'announcements.noAttachments' | transloco }}</p>
            <div class="file" *ngFor="let file of item.files">
              <span>{{ file.originalFileName }}</span>
              <button mat-icon-button [attr.aria-label]="'materials.download' | transloco" (click)="download(file)">
                <mat-icon>download</mat-icon>
              </button>
            </div>
          </mat-card-content>
          <mat-card-actions *ngIf="item.canEdit">
            <button mat-button (click)="startEdit(item)"><mat-icon>edit</mat-icon>{{ 'common.edit' | transloco }}</button>
            <button mat-button color="warn" (click)="remove(item)"><mat-icon>delete</mat-icon>{{ 'common.delete' | transloco }}</button>
          </mat-card-actions>
        </mat-card>

        <mat-card class="detail placeholder" *ngIf="!selected() && !editing()">
          <mat-card-content>{{ 'announcements.selectPrompt' | transloco }}</mat-card-content>
        </mat-card>
      </section>

      <mat-card class="editor" *ngIf="editing()">
        <mat-card-header><mat-card-title>{{ editId() ? ('announcements.edit' | transloco) : ('announcements.compose' | transloco) }}</mat-card-title></mat-card-header>
        <mat-card-content>
          <form (ngSubmit)="save()">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'announcements.fields.title' | transloco }}</mat-label>
              <input matInput name="title" [(ngModel)]="form.title" required maxlength="300">
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'announcements.fields.body' | transloco }}</mat-label>
              <textarea matInput name="body" rows="6" [(ngModel)]="form.body" required></textarea>
            </mat-form-field>
            <label class="select-label">
              <span>{{ 'announcements.fields.audience' | transloco }}</span>
              <select name="target" [(ngModel)]="targetValue" required>
                <option *ngFor="let target of targets()" [value]="targetValueFor(target)">{{ targetLabel(target) }}</option>
              </select>
            </label>
            <div class="dates">
              <mat-form-field appearance="outline">
                <mat-label>{{ 'announcements.fields.publishAt' | transloco }}</mat-label>
                <input matInput name="publishAt" type="datetime-local" [(ngModel)]="form.publishAt">
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>{{ 'announcements.fields.expiresAt' | transloco }}</mat-label>
                <input matInput name="expiresAt" type="datetime-local" [(ngModel)]="form.expiresAt">
              </mat-form-field>
            </div>
            <div class="upload">
              <label>{{ 'announcements.fields.attachment' | transloco }}</label>
              <input type="file" (change)="choose($event)">
            </div>
            <div class="actions">
              <button mat-button type="button" (click)="cancelEdit()">{{ 'common.cancel' | transloco }}</button>
              <button mat-flat-button color="primary" type="submit" [disabled]="saving()">{{ 'common.save' | transloco }}</button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </main>
  `,
  styles: [`
    .announcements-page { max-width: 1200px; }
    .empty { color: var(--mat-sys-on-surface-variant); margin: 4px 0 0; }
    .layout { display: grid; grid-template-columns: minmax(280px, 420px) 1fr; gap: 20px; align-items: start; }
    .list { display: grid; gap: 10px; }
    .announcement-row { text-align: left; border: 1px solid var(--mat-sys-outline-variant); border-radius: var(--mat-sys-corner-medium); background: var(--mat-sys-surface); padding: 14px; display: grid; gap: 6px; cursor: pointer; }
    .announcement-row.unread { border-color: var(--mat-sys-primary); box-shadow: inset 4px 0 0 var(--mat-sys-primary); }
    .announcement-row.selected { background: var(--app-brand-container); }
    .row-title { font-weight: 600; color: var(--mat-sys-on-surface); }
    .meta, .preview { color: var(--mat-sys-on-surface-variant); font-size: 0.9rem; }
    .preview { overflow: hidden; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; white-space: pre-line; }
    .flags, .detail-flags { display: flex; flex-wrap: wrap; gap: 6px; }
    .detail, .editor { border-radius: var(--mat-sys-corner-medium); }
    .detail .body { white-space: pre-wrap; margin: 20px 0; line-height: 1.55; }
    .detail h2 { font-size: 1rem; margin: 16px 0 8px; }
    .file { display: flex; align-items: center; justify-content: space-between; border-top: 1px solid var(--mat-sys-outline-variant); padding: 8px 0; }
    .editor { margin-top: 20px; }
    form { display: grid; gap: 12px; }
    .dates { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
    .upload { display: grid; gap: 6px; color: var(--mat-sys-on-surface-variant); }
    .select-label { display: grid; gap: 6px; color: var(--mat-sys-on-surface-variant); }
    .select-label select { min-height: 48px; border: 1px solid var(--mat-sys-outline); border-radius: 4px; padding: 0 12px; background: var(--mat-sys-surface); font: inherit; color: inherit; }
    .actions { display: flex; justify-content: flex-end; gap: 8px; }
    @media (max-width: 800px) { .layout, .dates { grid-template-columns: 1fr; } }
  `]
})
export class AnnouncementsPageComponent implements OnInit {
  protected readonly announcements = inject(AnnouncementsService);
  protected readonly language = inject(LanguageService);
  private readonly auth = inject(AuthService);
  private readonly materials = inject(MaterialsService);
  private readonly snack = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);

  protected readonly feed = this.announcements.feed;
  protected readonly selected = signal<Announcement | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly editing = signal(false);
  protected readonly editId = signal<number | null>(null);
  protected readonly targets = signal<AnnouncementTarget[]>([]);
  protected readonly canCompose = computed(() => this.targets().length > 0 && this.auth.hasAnyRole(['Admin', 'Teacher']));
  protected targetValue = '';
  protected selectedFile: File | null = null;
  protected form: { title: string; body: string; publishAt: string; expiresAt: string } = { title: '', body: '', publishAt: '', expiresAt: '' };

  ngOnInit(): void {
    this.load();
    this.announcements.targets().subscribe({ next: r => this.targets.set(r.targets), error: () => this.targets.set([]) });
  }

  protected load(): void {
    this.loading.set(true);
    this.announcements.loadMine().subscribe({
      next: feed => { this.loading.set(false); if (this.selected()) this.selected.set(feed.announcements.find(a => a.id === this.selected()!.id) ?? null); },
      error: () => { this.loading.set(false); this.message('announcements.loadFailed'); }
    });
  }

  protected open(item: Announcement, markRead = true): void {
    this.announcements.get(item.id).subscribe({
      next: detail => {
        this.selected.set(detail);
        if (markRead && !detail.isRead) {
          this.announcements.markRead(detail.id).subscribe({ next: () => this.selected.update(v => v ? { ...v, isRead: true } : v) });
        }
      },
      error: () => this.message('announcements.loadFailed')
    });
  }

  protected startCreate(): void {
    this.editing.set(true); this.editId.set(null); this.selected.set(null); this.selectedFile = null;
    this.form = { title: '', body: '', publishAt: '', expiresAt: '' };
    this.targetValue = this.targets()[0] ? this.targetValueFor(this.targets()[0]) : '';
  }

  protected startEdit(item: Announcement): void {
    this.editing.set(true); this.editId.set(item.id); this.selectedFile = null;
    this.form = { title: item.title, body: item.body, publishAt: this.toLocalInput(item.publishAt), expiresAt: item.expiresAt ? this.toLocalInput(item.expiresAt) : '' };
    this.targetValue = `${item.audienceType}:${item.targetId ?? ''}`;
  }

  protected cancelEdit(): void { this.editing.set(false); this.editId.set(null); this.selectedFile = null; }
  protected choose(event: Event): void { this.selectedFile = (event.target as HTMLInputElement).files?.[0] ?? null; }

  protected save(): void {
    const target = this.parseTarget();
    if (!target || !this.form.title.trim() || !this.form.body.trim()) return;
    this.saving.set(true);
    const payload: AnnouncementWrite = { title: this.form.title.trim(), body: this.form.body.trim(), audienceType: target.audienceType, targetId: target.targetId, publishAt: this.fromLocalInput(this.form.publishAt), expiresAt: this.fromLocalInput(this.form.expiresAt) };
    const id = this.editId();
    const request = id ? this.announcements.update(id, payload) : this.announcements.create(payload);
    request.subscribe({ next: saved => this.afterSave(saved), error: () => { this.saving.set(false); this.message('announcements.saveFailed'); } });
  }

  private afterSave(saved: Announcement): void {
    const finish = () => { this.saving.set(false); this.editing.set(false); this.editId.set(null); this.selectedFile = null; this.message('announcements.saved'); this.load(); this.open(saved, false); };
    if (!this.selectedFile) { finish(); return; }
    this.announcements.upload(saved.id, this.selectedFile).subscribe({ next: finish, error: () => { this.saving.set(false); this.message('materials.uploadFailed'); } });
  }

  protected remove(item: Announcement): void {
    this.announcements.delete(item.id).subscribe({ next: () => { this.selected.set(null); this.load(); this.message('announcements.deleted'); }, error: () => this.message('announcements.deleteFailed') });
  }

  protected download(file: { id: number; originalFileName: string }): void { this.materials.download(file); }
  protected audienceLabel(item: Announcement): string { return item.audienceType === 'School' ? this.transloco.translate('announcements.school') : item.audienceLabel; }
  protected targetLabel(target: AnnouncementTarget): string { return target.audienceType === 'School' ? this.transloco.translate('announcements.school') : target.label; }
  protected targetValueFor(target: AnnouncementTarget): string { return `${target.audienceType}:${target.targetId ?? ''}`; }

  private parseTarget(): { audienceType: AnnouncementAudienceType; targetId: number | null } | null {
    const [audienceType, rawId] = this.targetValue.split(':') as [AnnouncementAudienceType, string];
    if (!audienceType) return null;
    return { audienceType, targetId: rawId ? Number(rawId) : null };
  }

  private toLocalInput(value: string): string { const d = new Date(value); d.setMinutes(d.getMinutes() - d.getTimezoneOffset()); return d.toISOString().slice(0, 16); }
  private fromLocalInput(value: string): string | null { return value ? new Date(value).toISOString() : null; }
  private message(key: string): void { this.snack.open(this.transloco.translate(key), this.transloco.translate('common.close'), { duration: 4000 }); }
}
