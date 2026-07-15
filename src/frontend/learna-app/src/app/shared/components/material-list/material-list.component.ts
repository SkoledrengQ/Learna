import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MaterialsService } from '../../../core/services/materials.service';
import { FileResource } from '../../models/file-resource.model';
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-material-list', standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, TranslocoModule, LocalizedDatePipe],
  templateUrl: './material-list.component.html', styleUrl: './material-list.component.scss'
})
export class MaterialListComponent implements OnInit {
  @Input({ required: true }) targetType!: 'subject-group' | 'lesson' | 'assignment';
  @Input({ required: true }) targetId!: number;
  @Input() canUpload = false;
  private readonly materials = inject(MaterialsService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);
  readonly language = inject(LanguageService);
  readonly files = signal<FileResource[]>([]);
  readonly loading = signal(false);
  readonly uploading = signal(false);
  selectedFile: File | null = null;
  private fileInput: HTMLInputElement | null = null;
  description = '';

  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading.set(true);
    const request = this.targetType === 'lesson' ? this.materials.listLesson(this.targetId) : this.targetType === 'assignment' ? this.materials.listAssignment(this.targetId) : this.materials.listSubjectGroup(this.targetId);
    request.subscribe({ next: files => { this.files.set(files); this.loading.set(false); }, error: () => { this.loading.set(false); this.message('materials.loadFailed'); } });
  }
  choose(event: Event): void { this.fileInput = event.target as HTMLInputElement; this.selectedFile = this.fileInput.files?.[0] ?? null; }
  upload(): void {
    if (!this.selectedFile) return;
    this.uploading.set(true);
    const request = this.targetType === 'lesson' ? this.materials.uploadLesson(this.targetId, this.selectedFile, this.description) : this.targetType === 'assignment' ? this.materials.uploadAssignment(this.targetId, this.selectedFile, this.description) : this.materials.uploadSubjectGroup(this.targetId, this.selectedFile, this.description);
    request.subscribe({ next: () => { this.selectedFile = null; if (this.fileInput) this.fileInput.value = ''; this.description = ''; this.uploading.set(false); this.message('materials.uploaded'); this.load(); }, error: error => { this.uploading.set(false); this.message(error.error?.code === 'FILE_TOO_LARGE' ? 'materials.tooLarge' : error.error?.code === 'FILE_TYPE_NOT_ALLOWED' ? 'materials.wrongType' : 'materials.uploadFailed'); } });
  }
  download(file: FileResource): void { this.materials.download(file); }
  remove(file: FileResource): void {
    this.dialog.open(ConfirmDialogComponent, { data: { title: this.transloco.translate('materials.deleteTitle'), message: this.transloco.translate('materials.deleteMessage', { name: file.originalFileName }) } }).afterClosed().subscribe(ok => {
      if (ok) this.materials.delete(file.id).subscribe({ next: () => { this.message('materials.deleted'); this.load(); }, error: () => this.message('materials.deleteFailed') });
    });
  }
  size(bytes: number): string { if (bytes < 1024) return `${bytes} B`; if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`; return `${(bytes / 1048576).toFixed(1)} MB`; }
  private message(key: string): void { this.snack.open(this.transloco.translate(key), this.transloco.translate('common.close'), { duration: 4000 }); }
}
