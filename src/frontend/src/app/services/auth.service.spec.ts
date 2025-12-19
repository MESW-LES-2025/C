import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { AuthService } from './auth.service';

describe('AuthService', () => {
    let service: AuthService;
    let store: { [key: string]: string } = {};

    beforeEach(() => {
        // Mock localStorage
        spyOn(localStorage, 'getItem').and.callFake((key: string) => {
            return store[key] || null;
        });
        spyOn(localStorage, 'setItem').and.callFake((key: string, value: string) => {
            store[key] = value;
        });
        spyOn(localStorage, 'removeItem').and.callFake((key: string) => {
            delete store[key];
        });
        store = {};

        TestBed.configureTestingModule({
            providers: [provideZonelessChangeDetection()]
        });
        service = TestBed.inject(AuthService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should set, get and remove token', () => {
        const token = 'abc';
        service.setToken(token);
        expect(localStorage.setItem).toHaveBeenCalledWith('auth_token', token);
        expect(service.getToken()).toBe(token);

        service.removeToken();
        expect(localStorage.removeItem).toHaveBeenCalledWith('auth_token');
        expect(service.getToken()).toBeNull();
    });

    it('should return default user name and email when no token', () => {
        expect(service.getUserName()).toBe('User');
        expect(service.getUserEmail()).toBe('user@example.com');
    });

    // Mock token decoding would require a fake JWT structure. 
    // Since we can't easily generate a valid JWT without a library, we'll verify basic behavior 
    // or mock decodeToken if it was public. Since it's private, we rely on full token behavior.
    // We can construct a fake base64 token.
    // Header: {"typ":"JWT","alg":"HS256"} -> eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9
    // Payload: {"sub":"123","name":"Test User","email":"test@example.com","role":"Admin","exp":9999999999} 
    // -> eyJzdWIiOiIxMjMiLCJuYW1lIjoiVGVzdCBVc2VyIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6IkFkbWluIiwiZXhwIjo5OTk5OTk5OTk5fQ
    // Signature: (ignored by decodeToken usually) -> signature

    const fakeToken = 'eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMiLCJuYW1lIjoiVGVzdCBVc2VyIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6IkFkbWluIiwiZXhwIjo5OTk5OTk5OTk5fQ.signature';

    it('should parse user details from token', () => {
        service.setToken(fakeToken);

        expect(service.getUserName()).toBe('Test User');
        expect(service.getUserEmail()).toBe('test@example.com');
        expect(service.getUserRole()).toBe('Admin');
        expect(service.getUserId()).toBeNull(); // Payload uses "sub", but code checks "user_id"
        expect(service.isLoggedIn()).toBeTrue();
    });

    it('should check roles correctly', () => {
        service.setToken(fakeToken);
        expect(service.hasRole(['Admin'])).toBeTrue();
        expect(service.hasRole(['User'])).toBeFalse();
        expect(service.hasRole(['Admin', 'User'])).toBeTrue();
    });
});
