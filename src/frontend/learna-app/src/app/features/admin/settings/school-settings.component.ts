import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { SchoolSettingsService } from '../../../core/services/school-settings.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../../shared/components/loading-state/loading-state.component';
import { pickOnColor } from '../../../shared/utils/theme-color.util';

const HEX_COLOR_PATTERN = /^#[0-9A-Fa-f]{6}$/;

@Component({
  selector: 'app-school-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
    TranslocoModule,
    PageHeaderComponent,
    LoadingStateComponent
  ],
  templateUrl: './school-settings.component.html',
  styleUrl: './school-settings.component.scss'
})
export class SchoolSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private schoolSettingsService = inject(SchoolSettingsService);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  isLoading = signal(true);
  isSaving = signal(false);

  form = this.fb.group({
    schoolName: ['', [Validators.required, Validators.maxLength(200)]],
    primaryColor: ['', [Validators.required, Validators.pattern(HEX_COLOR_PATTERN)]]
  });

  // Zoneless-safe: a FormControl's `.value` is not itself tracked reactively by computed(),
  // so the live preview is derived from valueChanges via toSignal instead (see CLAUDE.md).
  private readonly primaryColorInput = toSignal(this.form.controls.primaryColor.valueChanges, {
    initialValue: this.form.controls.primaryColor.value
  });
  readonly previewColor = computed(() => {
    const value = this.primaryColorInput() ?? '';
    return HEX_COLOR_PATTERN.test(value) ? value : this.schoolSettingsService.primaryColor();
  });
  readonly previewOnColor = computed(() => pickOnColor(this.previewColor()));

  ngOnInit(): void {
    this.schoolSettingsService.refresh().subscribe({
      next: settings => {
        this.form.patchValue(settings);
        this.isLoading.set(false);
      },
      error: () => {
        // Fall back to whatever is already cached/applied so the form still has values to edit.
        this.form.patchValue(this.schoolSettingsService.settings());
        this.snackBar.open(this.transloco.translate('admin.settings.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const { schoolName, primaryColor } = this.form.getRawValue();

    this.schoolSettingsService.update({ schoolName: schoolName!, primaryColor: primaryColor! }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.transloco.translate('admin.settings.saved'), this.transloco.translate('common.close'), { duration: 3000 });
      },
      error: () => {
        this.isSaving.set(false);
        this.snackBar.open(this.transloco.translate('admin.settings.saveFailed'), this.transloco.translate('common.close'), { duration: 5000 });
      }
    });
  }

  getColorErrorMessage(): string {
    const control = this.form.controls.primaryColor;
    if (control.hasError('required')) {
      return this.transloco.translate('validation.required');
    }
    if (control.hasError('pattern')) {
      return this.transloco.translate('admin.settings.invalidColor');
    }
    return '';
  }
}
