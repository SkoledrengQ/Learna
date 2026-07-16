import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { TranslocoModule } from '@jsverse/transloco';

@Component({ selector: 'app-unauthorized', standalone: true, imports: [RouterLink, MatButtonModule, MatCardModule, TranslocoModule], template: `<mat-card><mat-card-header><mat-card-title>{{ 'auth.accessDenied' | transloco }}</mat-card-title></mat-card-header><mat-card-content><p>{{ 'auth.accessDeniedMessage' | transloco }}</p><a mat-flat-button color="primary" routerLink="/schedule">{{ 'nav.schedule' | transloco }}</a></mat-card-content></mat-card>` })
export class UnauthorizedComponent {}
