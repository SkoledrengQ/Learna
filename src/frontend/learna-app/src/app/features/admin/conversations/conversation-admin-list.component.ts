import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MessagesService } from '../../../core/services/messages.service';
import { LanguageService } from '../../../core/services/language.service';
import { AdminConversationSummary, ConversationDetail, ConversationMessage } from '../../../shared/models/messaging.model';
import { LocalizedDatePipe } from '../../../shared/pipes/localized-date.pipe';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'app-conversation-admin-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatSnackBarModule, TranslocoModule, LocalizedDatePipe, PageHeaderComponent, EmptyStateComponent, LoadingStateComponent],
  templateUrl: './conversation-admin-list.component.html',
  styleUrl: './conversation-admin-list.component.scss'
})
export class ConversationAdminListComponent implements OnInit {
  private readonly messagesService = inject(MessagesService);
  protected readonly language = inject(LanguageService);
  private readonly snack = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);

  protected readonly conversations = signal<AdminConversationSummary[]>([]);
  protected readonly loadingList = signal(false);
  protected searchText = '';

  protected readonly selectedId = signal<number | null>(null);
  protected readonly selectedDetail = signal<ConversationDetail | null>(null);
  protected readonly messages = signal<ConversationMessage[]>([]);
  protected readonly loadingThread = signal(false);

  ngOnInit(): void {
    this.search();
  }

  protected search(): void {
    this.loadingList.set(true);
    this.messagesService.adminSearch(this.searchText.trim() || undefined).subscribe({
      next: rows => { this.conversations.set(rows); this.loadingList.set(false); },
      error: () => { this.loadingList.set(false); this.message('admin.conversations.loadFailed'); }
    });
  }

  protected open(item: AdminConversationSummary): void {
    this.selectedId.set(item.id);
    this.loadingThread.set(true);
    this.messagesService.get(item.id).subscribe({
      next: detail => this.selectedDetail.set(detail),
      error: () => this.message('admin.conversations.loadFailed')
    });
    this.messagesService.messages(item.id, undefined, 50).subscribe({
      next: page => { this.messages.set(page.messages); this.loadingThread.set(false); },
      error: () => { this.loadingThread.set(false); this.message('admin.conversations.loadFailed'); }
    });
  }

  private message(key: string): void {
    this.snack.open(this.transloco.translate(key), this.transloco.translate('common.close'), { duration: 4000 });
  }
}
