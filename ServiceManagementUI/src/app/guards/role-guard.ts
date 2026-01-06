import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth-service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const expectedRoles: string[] = route.data['roles'];
  const userRole = (authService.userRole() || '').toString().trim();

 
  const normalizedExpected = (expectedRoles || []).map(r => (r || '').toString().trim().toLowerCase());
  const normalizedUser = userRole.toLowerCase();

  
  console.debug('roleGuard:', {
    isLoggedIn: authService.isLoggedIn(),
    userRole: userRole,
    expectedRoles: expectedRoles,
    normalizedUser,
    normalizedExpected
  });

  if (authService.isLoggedIn() && normalizedExpected.includes(normalizedUser)) {
    return true;
  }
  return router.createUrlTree(['/login']);
};