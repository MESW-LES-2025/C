import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, Component } from '@angular/core';
import { HomeComponent } from './home';
import { AuthService } from '../../services/auth.service';
import { ClientDashboardComponent } from './dashboards/client-dashboard/client-dashboard';
import { LawyerDashboardComponent } from './dashboards/lawyer-dashboard/lawyer-dashboard';
import { AdminDashboardComponent } from './dashboards/admin-dashboard/admin-dashboard';

@Component({ selector: 'app-client-dashboard', standalone: true, template: '' })
class MockClientDashboard { }

@Component({ selector: 'app-lawyer-dashboard', standalone: true, template: '' })
class MockLawyerDashboard { }

@Component({ selector: 'app-admin-dashboard', standalone: true, template: '' })
class MockAdminDashboard { }

describe('HomeComponent', () => {
    let component: HomeComponent;
    let fixture: ComponentFixture<HomeComponent>;
    let authServiceSpy: jasmine.SpyObj<AuthService>;

    beforeEach(async () => {
        const spy = jasmine.createSpyObj('AuthService', ['getUserRole']);

        await TestBed.configureTestingModule({
            imports: [HomeComponent],
            providers: [
                provideZonelessChangeDetection(),
                { provide: AuthService, useValue: spy }
            ]
        })
            .overrideComponent(HomeComponent, {
                remove: { imports: [ClientDashboardComponent, LawyerDashboardComponent, AdminDashboardComponent] },
                add: { imports: [MockClientDashboard, MockLawyerDashboard, MockAdminDashboard] }
            })
            .compileComponents();

        authServiceSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        authServiceSpy.getUserRole.and.returnValue('Client');

        fixture = TestBed.createComponent(HomeComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should expose user role', () => {
        authServiceSpy.getUserRole.and.returnValue('Lawyer');
        expect(component.role).toBe('Lawyer');
    });
});
