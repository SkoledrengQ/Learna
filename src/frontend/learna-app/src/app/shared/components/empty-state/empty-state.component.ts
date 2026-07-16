import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

/**
 * Consistent empty state used everywhere a list/table has no rows: icon, localized
 * message, and an optional primary action. Caller owns translation of `message`/`actionLabel`.
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  icon = input('inbox');
  message = input.required<string>();
  actionLabel = input<string | null>(null);
  action = output<void>();
}
