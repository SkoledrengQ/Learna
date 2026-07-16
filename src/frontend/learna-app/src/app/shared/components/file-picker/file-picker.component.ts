import { Component, ElementRef, ViewChild, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';

/**
 * Styled replacement for a raw `<input type="file">`: a button + selected-file
 * name(s), with the native input visually hidden but still keyboard/screen-reader
 * accessible. Callers own upload/submit logic; this only surfaces File[] selection.
 */
@Component({
  selector: 'app-file-picker',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, TranslocoModule],
  templateUrl: './file-picker.component.html',
  styleUrl: './file-picker.component.scss'
})
export class FilePickerComponent {
  multiple = input(false);
  accept = input<string | null>(null);
  disabled = input(false);
  buttonLabel = input<string | null>(null);
  filesSelected = output<File[]>();

  @ViewChild('nativeInput') private nativeInput!: ElementRef<HTMLInputElement>;
  protected readonly selectedNames = signal<string[]>([]);

  onChange(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    this.selectedNames.set(files.map(f => f.name));
    this.filesSelected.emit(files);
  }

  /** Clears the picker (both the displayed name(s) and the underlying input value). */
  clear(): void {
    this.selectedNames.set([]);
    if (this.nativeInput) this.nativeInput.nativeElement.value = '';
  }
}
