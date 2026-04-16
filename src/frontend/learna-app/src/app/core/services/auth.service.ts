import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap, catchError, throwError } from 'rxjs';
import { LoginRequest, LoginResponse, User } from '../../shared/models/auth.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly API_URL = 'http://localhost:5157/api/auth';
  private readonly TOKEN_KEY = 'access_token';
  private readonly USER_KEY = 'current_user';

  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  // Signal-based authentication state
  public isAuthenticated = computed(() => this.currentUserSubject.value !== null);

  private tokenRefreshTimer?: number;

  constructor() {
    // Initialize user from storage on service creation
    const user = this.getUserFromStorage();
    if (user) {
      this.currentUserSubject.next(user);
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
    return this.http.post<void>(`${this.API_URL}/change-password`, {
      currentPassword,
      newPassword
    });
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
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

    // Update current user subject
    this.currentUserSubject.next(response.user);
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

    // Update current user subject
    this.currentUserSubject.next(null);

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
