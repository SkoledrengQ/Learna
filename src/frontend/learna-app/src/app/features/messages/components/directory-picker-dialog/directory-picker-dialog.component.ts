import { Component, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { MessagesService } from '../../../../core/services/messages.service';
import { DirectoryPerson } from '../../../../shared/models/messaging.model';

export type DirectoryPickerMode = 'new-conversation' | 'add-participants';

export interface DirectoryPickerDialogData {
  mode: DirectoryPickerMode;
  excludeUserIds: number[];
}

export type DirectoryPickerResult =
  | { kind: 'direct'; userId: number }
  | { kind: 'group'; userIds: number[]; title: string }
  | { kind: 'add'; userIds: number[] };

@Component({
  selector: 'app-directory-picker-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatButtonToggleModule, MatCheckboxModule, MatDialogModule, MatFormFieldModule, MatInputModule, TranslocoModule],
  templateUrl: './directory-picker-dialog.component.html',
  styleUrl: './directory-picker-dialog.component.scss'
})
export class DirectoryPickerDialogComponent implements OnInit {
  private readonly messagesService = inject(MessagesService);

  readonly isNewConversation: boolean;
  conversationKind: 'direct' | 'group' = 'direct';
  groupTitle = '';

  people = signal<DirectoryPerson[]>([]);
  filterText = signal('');
  loading = signal(true);
  selectedUserIds = signal<Set<number>>(new Set());

  filteredPeople = computed(() => {
    const term = this.filterText().trim().toLowerCase();
    const excluded = new Set(this.data.excludeUserIds);
    const candidates = this.people().filter(p => !excluded.has(p.userId));
    if (!term) return candidates;
    return candidates.filter(p => p.name.toLowerCase().includes(term));
  });

  constructor(
    public dialogRef: MatDialogRef<DirectoryPickerDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DirectoryPickerDialogData
  ) {
    this.isNewConversation = this.data.mode === 'new-conversation';
  }

  ngOnInit(): void {
    this.search('');
  }

  onModeChange(): void {
    this.selectedUserIds.set(new Set());
    this.groupTitle = '';
  }

  onFilterInput(value: string): void {
    this.filterText.set(value);
    this.search(value);
  }

  private search(term: string): void {
    this.loading.set(true);
    this.messagesService.directory(term || undefined).subscribe({
      next: people => { this.people.set(people); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  isSelected(userId: number): boolean {
    return this.selectedUserIds().has(userId);
  }

  toggle(userId: number): void {
    const next = new Set(this.selectedUserIds());
    if (next.has(userId)) next.delete(userId); else next.add(userId);
    this.selectedUserIds.set(next);
  }

  pickDirect(userId: number): void {
    this.dialogRef.close({ kind: 'direct', userId } as DirectoryPickerResult);
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onConfirm(): void {
    const userIds = Array.from(this.selectedUserIds());
    if (userIds.length === 0) return;
    if (this.data.mode === 'add-participants') {
      this.dialogRef.close({ kind: 'add', userIds } as DirectoryPickerResult);
      return;
    }
    if (!this.groupTitle.trim()) return;
    this.dialogRef.close({ kind: 'group', userIds, title: this.groupTitle.trim() } as DirectoryPickerResult);
  }
}
