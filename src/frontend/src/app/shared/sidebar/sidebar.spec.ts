import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { SidebarComponent } from './sidebar';
import { AuthService } from '../../services/auth.service';

describe('SidebarComponent', () => {
    let component: SidebarComponent;
    let fixture: ComponentFixture<SidebarComponent>;
    let authServiceSpy: jasmine.SpyObj<AuthService>;
    let router: Router;

    beforeEach(async () => {
        const spy = jasmine.createSpyObj('AuthService', ['getUserName', 'getUserEmail', 'hasRole', 'removeToken']);

        await TestBed.configureTestingModule({
            imports: [SidebarComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: AuthService, useValue: spy }
            ]
        })
            .compileComponents();

        authServiceSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        // Setup default returns
        authServiceSpy.getUserName.and.returnValue('Test User');
        authServiceSpy.getUserEmail.and.returnValue('test@example.com');
        authServiceSpy.hasRole.and.returnValue(false);

        router = TestBed.inject(Router);
        spyOn(router, 'navigate');

        fixture = TestBed.createComponent(SidebarComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
        expect(component.userName).toBe('Test User');
    });

    it('should generate initials', () => {
        expect(component.getInitials('John Doe')).toBe('JD');
        expect(component.getInitials('Single')).toBe('S');
    });

    it('should check roles', () => {
        component.hasRole(['Admin']);
        expect(authServiceSpy.hasRole).toHaveBeenCalledWith(['Admin']);
    });

    it('should logout', () => {
        component.logout();
        expect(authServiceSpy.removeToken).toHaveBeenCalled();
        expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
});
