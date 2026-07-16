import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { BreakpointObserver } from '@angular/cdk/layout';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';

const MOBILE_BREAKPOINT = '(max-width: 899.98px)';

/**
 * Wraps filter controls so a narrow viewport collapses them into an expandable
 * panel (data stays visible without scrolling past a wall of inputs); a wide
 * viewport renders the same content flat, unchanged from the pre-WO15 layout.
 */
@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [CommonModule, MatExpansionModule, MatIconModule, TranslocoModule],
  templateUrl: './filter-panel.component.html',
  styleUrl: './filter-panel.component.scss'
})
export class FilterPanelComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);
  protected readonly isMobile = toSignal(
    this.breakpointObserver.observe(MOBILE_BREAKPOINT).pipe(map(state => state.matches)),
    { initialValue: false }
  );
}
