import { Component, signal, inject, computed } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { CommonModule } from '@angular/common';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private authService = inject(AuthService);

  protected readonly title = signal('Learna');
  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly currentUser = computed(() => this.authService.getCurrentUser());
  protected readonly isAdmin = computed(() => this.authService.hasAnyRole(['Admin']));

  onLogout(): void {
    this.authService.logout();
  }
}
