import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

/**
 * Consistent chrome for a dashboard widget: icon + title, an optional "view all"
 * link to the card's full page, and a content slot the caller fills with its own
 * loading/empty/list state (so each card still follows the WO14 loading-state /
 * empty-state pattern internally).
 */
@Component({
  selector: 'app-dashboard-card',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatIconModule],
  templateUrl: './dashboard-card.component.html',
  styleUrl: './dashboard-card.component.scss'
})
export class DashboardCardComponent {
  title = input.required<string>();
  icon = input<string | null>(null);
  link = input<string | any[] | null>(null);
  linkLabel = input<string | null>(null);
}
