import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Coherent color language for all status chips app-wide (lesson cancelled/modified,
 * submission late, grade draft/published, attendance statuses). Semantic variants use
 * fixed hues so meaning stays legible regardless of the school's chosen brand color;
 * `brand` ties directly to the runtime primary color for non-semantic "this is the
 * important one" chips (e.g. published/active).
 */
export type StatusChipVariant = 'neutral' | 'info' | 'success' | 'warning' | 'danger' | 'brand';

@Component({
  selector: 'app-status-chip',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './status-chip.component.html',
  styleUrl: './status-chip.component.scss'
})
export class StatusChipComponent {
  variant = input<StatusChipVariant>('neutral');
}
