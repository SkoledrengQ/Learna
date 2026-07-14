import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule,
    TranslocoModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  loginForm!: FormGroup;
  isLoading = signal(false);
  hidePassword = signal(true);
  returnUrl: string = '/students';

  ngOnInit(): void {
    // Create login form
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });

    // Get return URL from query params or default to students
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/students';

    // If already logged in, redirect to return URL
    if (this.authService.getCurrentUser()) {
      this.router.navigate([this.returnUrl]);
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading.set(true);

    const credentials = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password
    };

    this.authService.login(credentials).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.snackBar.open(
          this.transloco.translate('auth.welcomeBack', { email: response.user.email }),
          this.transloco.translate('common.close'),
          { duration: 3000 }
        );
        // Small delay to ensure auth state propagates
        setTimeout(() => {
          this.router.navigate([this.returnUrl]);
        }, 100);
      },
      error: (error) => {
        this.isLoading.set(false);
        const errorMessage = error.error?.message || this.transloco.translate('auth.invalidCredentials');
        this.snackBar.open(errorMessage, this.transloco.translate('common.close'), {
          duration: 5000
        });
      }
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }

  getEmailErrorMessage(): string {
    const emailControl = this.loginForm.get('email');
    if (emailControl?.hasError('required')) {
      return this.transloco.translate('auth.emailRequired');
    }
    if (emailControl?.hasError('email')) {
      return this.transloco.translate('auth.emailInvalid');
    }
    return '';
  }

  getPasswordErrorMessage(): string {
    const passwordControl = this.loginForm.get('password');
    if (passwordControl?.hasError('required')) {
      return this.transloco.translate('auth.passwordRequired');
    }
    if (passwordControl?.hasError('minlength')) {
      return this.transloco.translate('auth.passwordMinLength');
    }
    return '';
  }
}
