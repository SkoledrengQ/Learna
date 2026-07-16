import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Consistent page header used across every screen: title, optional subtitle, and a slot
 * for page-level actions (buttons) on the right. Purely presentational.
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss'
})
export class PageHeaderComponent {
  title = input.required<string>();
  subtitle = input<string | null>(null);
}
