import { Injectable, inject, signal, computed } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { LoginRequest, LoginResponse, User, AUTH_USER_STORAGE_KEY } from '../../shared/models/auth.model';
import { environment } from '../../../environments/environment';
import { LanguageService } from './language.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private languageService = inject(LanguageService);

  private readonly API_URL = 'http://localhost:5157/api/auth';
  private readonly TOKEN_KEY = 'access_token';
  private readonly USER_KEY = AUTH_USER_STORAGE_KEY;

  // Signal-based authentication state (this app runs zoneless - state driving
  // templates must be signals; an RxJS BehaviorSubject's .value is not tracked
  // reactively by computed(), so it would only ever reflect the value at first read)
  private currentUserSignal = signal<User | null>(this.getUserFromStorage());
  public currentUser$ = toObservable(this.currentUserSignal);

  public isAuthenticated = computed(() => this.currentUserSignal() !== null);

  private tokenRefreshTimer?: number;

  constructor() {
    // Initialize user from storage on service creation
    const user = this.getUserFromStorage();
    if (user) {
      this.scheduleTokenRefresh();
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/login`, credentials, {
      withCredentials: true // Important for cookies
    }).pipe(
      tap(response => {
        this.setSession(response);
        this.scheduleTokenRefresh();
      }),
      catchError(error => {
        console.error('Login error:', error);
        return throwError(() => error);
      })
    );
  }

  logout(): void {
    // Call backend to revoke refresh token
    this.http.post(`${this.API_URL}/logout`, {}, {
      withCredentials: true
    }).subscribe({
      next: () => {
        this.clearSession();
      },
      error: (error) => {
        console.error('Logout error:', error);
        // Clear session anyway
        this.clearSession();
      }
    });
  }

  refreshToken(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/refresh`, {}, {
      withCredentials: true
    }).pipe(
      tap(response => {
        this.setSession(response);
        this.scheduleTokenRefresh();
      }),
      catchError(error => {
        console.error('Token refresh error:', error);
        this.clearSession();
        return throwError(() => error);
      })
    );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/password`, {
      currentPassword,
      newPassword
    });
  }

  getCurrentUser(): User | null {
    return this.currentUserSignal();
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user?.roles.includes(role) ?? false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.getCurrentUser();
    return roles.some(role => user?.roles.includes(role)) ?? false;
  }

  private setSession(response: LoginResponse): void {
    // Store access token in localStorage
    localStorage.setItem(this.TOKEN_KEY, response.accessToken);

    // Store user info
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));

    // Update current user signal
    this.currentUserSignal.set(response.user);

    // A logged-in user's preferred language wins over whatever was active pre-login
    this.languageService.applyUserPreference(response.user.preferredLanguage);
  }

  private clearSession(): void {
    // Clear tokens and user info
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);

    // Clear refresh timer
    if (this.tokenRefreshTimer) {
      window.clearTimeout(this.tokenRefreshTimer);
      this.tokenRefreshTimer = undefined;
    }

    // Update current user signal
    this.currentUserSignal.set(null);

    // Navigate to login
    this.router.navigate(['/login']);
  }

  private getUserFromStorage(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (!userJson) {
      return null;
    }

    try {
      return JSON.parse(userJson) as User;
    } catch {
      return null;
    }
  }

  private scheduleTokenRefresh(): void {
    // Clear existing timer
    if (this.tokenRefreshTimer) {
      window.clearTimeout(this.tokenRefreshTimer);
    }

    // Access tokens expire in 15 minutes, refresh 1 minute before
    const refreshTime = 14 * 60 * 1000; // 14 minutes in milliseconds

    this.tokenRefreshTimer = window.setTimeout(() => {
      this.refreshToken().subscribe({
        error: () => {
          // If refresh fails, user will be logged out
          console.error('Automatic token refresh failed');
        }
      });
    }, refreshTime);
  }
}
