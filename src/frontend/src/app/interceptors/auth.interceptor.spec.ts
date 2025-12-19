import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { Observable, of } from 'rxjs';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideZonelessChangeDetection()]
        });
        localStorage.clear();
    });

    it('should add Authorization header when token exists', () => {
        const token = 'test-token-123';
        localStorage.setItem('auth_token', token);

        const req = new HttpRequest('GET', '/api/test');
        const next: HttpHandlerFn = jasmine.createSpy('next').and.returnValue(of({} as HttpEvent<any>));

        TestBed.runInInjectionContext(() => {
            authInterceptor(req, next);
        });

        expect(next).toHaveBeenCalled();
        const modifiedReq = (next as jasmine.Spy).calls.mostRecent().args[0];
        expect(modifiedReq.headers.get('Authorization')).toBe(`Bearer ${token}`);
    });

    it('should not add Authorization header when token does not exist', () => {
        const req = new HttpRequest('GET', '/api/test');
        const next: HttpHandlerFn = jasmine.createSpy('next').and.returnValue(of({} as HttpEvent<any>));

        TestBed.runInInjectionContext(() => {
            authInterceptor(req, next);
        });

        expect(next).toHaveBeenCalledWith(req);
        const passedReq = (next as jasmine.Spy).calls.mostRecent().args[0];
        expect(passedReq.headers.has('Authorization')).toBeFalse();
    });
});
