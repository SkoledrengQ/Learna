import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { BreakpointObserver } from '@angular/cdk/layout';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MessagesService } from '../../core/services/messages.service';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { ConversationDetail, ConversationSummary, ConversationMessage } from '../../shared/models/messaging.model';
import { LocalizedDatePipe } from '../../shared/pipes/localized-date.pipe';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { StatusChipComponent } from '../../shared/components/status-chip/status-chip.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { DirectoryPickerDialogComponent, DirectoryPickerResult } from './components/directory-picker-dialog/directory-picker-dialog.component';

const MOBILE_BREAKPOINT = '(max-width: 899.98px)';

@Component({
  selector: 'app-messages-page',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatDialogModule, MatIconModule, MatSnackBarModule, TranslocoModule, LocalizedDatePipe, PageHeaderComponent, EmptyStateComponent, LoadingStateComponent, StatusChipComponent],
  templateUrl: './messages-page.component.html',
  styleUrl: './messages-page.component.scss'
})
export class MessagesPageComponent implements OnInit {
  protected readonly messagesService = inject(MessagesService);
  protected readonly language = inject(LanguageService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  @ViewChild('threadScroll') private threadScroll?: ElementRef<HTMLDivElement>;

  protected readonly isMobile = toSignal(
    this.breakpointObserver.observe(MOBILE_BREAKPOINT).pipe(map(state => state.matches)),
    { initialValue: false }
  );

  protected readonly conversations = this.messagesService.conversations;
  protected readonly loadingList = signal(false);

  protected readonly selectedId = signal<number | null>(null);
  protected readonly selectedDetail = signal<ConversationDetail | null>(null);
  protected readonly messages = signal<ConversationMessage[]>([]);
  protected readonly hasMoreOlder = signal(false);
  protected readonly loadingThread = signal(false);
  protected readonly loadingOlder = signal(false);
  protected readonly sending = signal(false);
  protected readonly showGroupPanel = signal(false);
  protected composerText = '';

  protected readonly currentUserId = computed(() => this.auth.getCurrentUser()?.id ?? null);

  constructor() {
    effect(() => {
      // Auto-scroll to the newest message whenever the thread's message list changes.
      this.messages();
      queueMicrotask(() => this.scrollToBottom());
    });
  }

  ngOnInit(): void {
    this.loadList();
  }

  protected loadList(): void {
    this.loadingList.set(true);
    this.messagesService.loadMine().subscribe({
      next: () => this.loadingList.set(false),
      error: () => { this.loadingList.set(false); this.message('messages.loadFailed'); }
    });
  }

  protected open(conversation: ConversationSummary): void {
    this.selectedId.set(conversation.id);
    this.showGroupPanel.set(false);
    this.loadThread(conversation.id);
  }

  protected backToList(): void {
    this.selectedId.set(null);
  }

  protected refreshThread(): void {
    const id = this.selectedId();
    if (id != null) this.loadThread(id);
  }

  private loadThread(id: number): void {
    this.loadingThread.set(true);
    this.messagesService.get(id).subscribe({
      next: detail => this.selectedDetail.set(detail),
      error: () => this.message('messages.loadFailed')
    });
    this.messagesService.messages(id).subscribe({
      next: page => {
        this.messages.set(page.messages);
        this.hasMoreOlder.set(page.hasMore);
        this.loadingThread.set(false);
        this.markReadAndRefreshList(id);
      },
      error: () => { this.loadingThread.set(false); this.message('messages.loadFailed'); }
    });
  }

  protected loadOlder(): void {
    const id = this.selectedId();
    const oldest = this.messages()[0]?.id;
    if (id == null || oldest == null) return;
    this.loadingOlder.set(true);
    this.messagesService.messages(id, oldest).subscribe({
      next: page => {
        this.messages.update(current => [...page.messages, ...current]);
        this.hasMoreOlder.set(page.hasMore);
        this.loadingOlder.set(false);
      },
      error: () => { this.loadingOlder.set(false); this.message('messages.loadFailed'); }
    });
  }

  private markReadAndRefreshList(id: number): void {
    this.messagesService.markRead(id).subscribe({ next: () => this.loadList(), error: () => this.loadList() });
  }

  protected onComposerKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  protected send(): void {
    const id = this.selectedId();
    const body = this.composerText.trim();
    if (id == null || !body || this.sending()) return;
    this.sending.set(true);
    this.messagesService.send(id, body).subscribe({
      next: sent => {
        this.messages.update(current => [...current, sent]);
        this.composerText = '';
        this.sending.set(false);
        this.loadList();
      },
      error: err => {
        this.sending.set(false);
        this.showPolicyOrGenericError(err, 'messages.sendFailed');
      }
    });
  }

  protected startNewConversation(): void {
    const ref = this.dialog.open(DirectoryPickerDialogComponent, { width: '480px', data: { mode: 'new-conversation', excludeUserIds: [] } });
    ref.afterClosed().subscribe((result?: DirectoryPickerResult) => {
      if (!result) return;
      const payload = result.kind === 'direct'
        ? { type: 'Direct' as const, userId: result.userId }
        : { type: 'Group' as const, userIds: result.userIds, title: result.kind === 'group' ? result.title : '' };
      this.messagesService.create(payload).subscribe({
        next: created => { this.loadList(); this.selectedId.set(created.id); this.loadThread(created.id); },
        error: err => this.showPolicyOrGenericError(err, 'messages.createFailed')
      });
    });
  }

  protected toggleGroupPanel(): void {
    this.showGroupPanel.update(v => !v);
  }

  protected addParticipants(): void {
    const detail = this.selectedDetail();
    if (!detail) return;
    const ref = this.dialog.open(DirectoryPickerDialogComponent, {
      width: '480px',
      data: { mode: 'add-participants', excludeUserIds: detail.participants.map(p => p.userId) }
    });
    ref.afterClosed().subscribe((result?: DirectoryPickerResult) => {
      if (!result || result.kind !== 'add') return;
      this.messagesService.addParticipants(detail.id, result.userIds).subscribe({
        next: updated => { this.selectedDetail.set(updated); this.loadList(); },
        error: err => this.showPolicyOrGenericError(err, 'messages.addParticipantsFailed')
      });
    });
  }

  protected removeParticipant(userId: number): void {
    const detail = this.selectedDetail();
    if (!detail) return;
    const isSelf = userId === this.currentUserId();
    const dialogData = {
      title: this.transloco.translate(isSelf ? 'messages.leaveGroupTitle' : 'messages.removeParticipantTitle'),
      message: this.transloco.translate(isSelf ? 'messages.leaveGroupConfirm' : 'messages.removeParticipantConfirm')
    };
    this.dialog.open(ConfirmDialogComponent, { width: '400px', data: dialogData }).afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.messagesService.removeParticipant(detail.id, userId).subscribe({
        next: () => {
          if (isSelf) {
            this.selectedId.set(null);
            this.selectedDetail.set(null);
            this.loadList();
          } else {
            this.messagesService.get(detail.id).subscribe({ next: updated => this.selectedDetail.set(updated) });
          }
        },
        error: () => this.message('messages.removeParticipantFailed')
      });
    });
  }

  protected canManageGroup(): boolean {
    return this.selectedDetail()?.type === 'Group';
  }

  private showPolicyOrGenericError(err: unknown, genericKey: string): void {
    const code = (err as { error?: { code?: string } })?.error?.code;
    this.message(code === 'POLICY_BLOCKED' ? 'messages.errors.policyBlocked' : genericKey);
  }

  private scrollToBottom(): void {
    const el = this.threadScroll?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }

  private message(key: string): void {
    this.snack.open(this.transloco.translate(key), this.transloco.translate('common.close'), { duration: 4000 });
  }
}
