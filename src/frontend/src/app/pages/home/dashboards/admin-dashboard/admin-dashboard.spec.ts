import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AdminDashboardComponent } from './admin-dashboard';
import { AuthService } from '../../../../services/auth.service';
import { UserService } from '../../../../services/user.service';
import { of } from 'rxjs';

describe('AdminDashboardComponent', () => {
    let component: AdminDashboardComponent;
    let fixture: ComponentFixture<AdminDashboardComponent>;
    let userServiceSpy: jasmine.SpyObj<UserService>;

    beforeEach(async () => {
        const authSpy = jasmine.createSpyObj('AuthService', ['getUserName']);
        const userSpy = jasmine.createSpyObj('UserService', ['getAllUsers']);

        await TestBed.configureTestingModule({
            imports: [AdminDashboardComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: AuthService, useValue: authSpy },
                { provide: UserService, useValue: userSpy }
            ]
        }).compileComponents();

        userServiceSpy = TestBed.inject(UserService) as jasmine.SpyObj<UserService>;
        userServiceSpy.getAllUsers.and.returnValue(of([{ status: 'ACTIVE' }, { status: 'INACTIVE' }]));

        fixture = TestBed.createComponent(AdminDashboardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load users', () => {
        expect(userServiceSpy.getAllUsers).toHaveBeenCalled();
        expect(component.users.length).toBe(2);
    });

    it('should calculate stats', () => {
        expect(component.stats.total).toBe(2);
        expect(component.stats.active).toBe(1);
    });
});
