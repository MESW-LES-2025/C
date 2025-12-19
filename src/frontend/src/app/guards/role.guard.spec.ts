import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { roleGuard } from './role.guard';
import { AuthService } from '../services/auth.service';

describe('roleGuard', () => {
    let authService: jasmine.SpyObj<AuthService>;
    let router: jasmine.SpyObj<Router>;
    let route: ActivatedRouteSnapshot;
    let state: RouterStateSnapshot;

    beforeEach(() => {
        const authSpy = jasmine.createSpyObj('AuthService', ['isLoggedIn', 'hasRole']);
        const routerSpy = jasmine.createSpyObj('Router', ['createUrlTree']);

        TestBed.configureTestingModule({
            providers: [
                provideZonelessChangeDetection(),
                { provide: AuthService, useValue: authSpy },
                { provide: Router, useValue: routerSpy }
            ]
        });

        authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

        route = {} as ActivatedRouteSnapshot;
        state = {} as RouterStateSnapshot;
    });

    it('should redirect to login if not logged in', () => {
        authService.isLoggedIn.and.returnValue(false);
        router.createUrlTree.and.returnValue({} as any);

        const result = TestBed.runInInjectionContext(() => roleGuard(route, state));

        expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
        expect(result).toBeTruthy();
    });

    it('should allow access if logged in and no roles required', () => {
        authService.isLoggedIn.and.returnValue(true);
        route.data = {};

        const result = TestBed.runInInjectionContext(() => roleGuard(route, state));

        expect(result).toBe(true);
    });

    it('should allow access if user has required role', () => {
        authService.isLoggedIn.and.returnValue(true);
        authService.hasRole.and.returnValue(true);
        route.data = { roles: ['Admin'] };

        const result = TestBed.runInInjectionContext(() => roleGuard(route, state));

        expect(result).toBe(true);
        expect(authService.hasRole).toHaveBeenCalledWith(['Admin']);
    });

    it('should redirect to home if user lacks required role', () => {
        authService.isLoggedIn.and.returnValue(true);
        authService.hasRole.and.returnValue(false);
        route.data = { roles: ['Admin'] };
        router.createUrlTree.and.returnValue({} as any);

        const result = TestBed.runInInjectionContext(() => roleGuard(route, state));

        expect(router.createUrlTree).toHaveBeenCalledWith(['/home']);
        expect(result).toBeTruthy();
    });
});
