import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  const expectedRoles = route.data['roles'] as Array<string>;

  if (!expectedRoles || expectedRoles.length === 0) {
    return true; // No specific roles required, just logged in
  }

  if (authService.hasRole(expectedRoles)) {
    return true;
  }

  // User is logged in but doesn't have permission
  // Redirect to home
  return router.createUrlTree(['/home']);
};
