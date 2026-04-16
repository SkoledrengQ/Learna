import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.getCurrentUser();

  // Check if user is authenticated
  if (!user) {
    // Store the attempted URL for redirecting after login
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  // Check role-based access if roles are specified in route data
  const requiredRoles = route.data['roles'] as string[] | undefined;
  if (requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = authService.hasAnyRole(requiredRoles);
    if (!hasRequiredRole) {
      // User doesn't have required role, redirect to unauthorized page
      router.navigate(['/unauthorized']);
      return false;
    }
  }

  return true;
};
