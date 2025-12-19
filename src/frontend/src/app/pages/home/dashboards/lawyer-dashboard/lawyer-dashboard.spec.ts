import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LawyerDashboardComponent } from './lawyer-dashboard';
import { AuthService } from '../../../../services/auth.service';
import { ProcessService } from '../../../../services/process.service';
import { MessageService } from '../../../../services/message.service';
import { of } from 'rxjs';

describe('LawyerDashboardComponent', () => {
    let component: LawyerDashboardComponent;
    let fixture: ComponentFixture<LawyerDashboardComponent>;

    beforeEach(async () => {
        const authSpy = jasmine.createSpyObj('AuthService', ['getUserName', 'getUserId']);
        const processSpy = jasmine.createSpyObj('ProcessService', ['getProcessesByLawyer', 'getProcessStatuses']);
        const messageSpy = jasmine.createSpyObj('MessageService', ['getUnreadCount']);

        authSpy.getUserId.and.returnValue('789');
        processSpy.getProcessesByLawyer.and.returnValue(of({ data: [] }));
        processSpy.getProcessStatuses.and.returnValue(of([]));
        messageSpy.getUnreadCount.and.returnValue(of({ total: 3, byProcess: [] }));

        await TestBed.configureTestingModule({
            imports: [LawyerDashboardComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: AuthService, useValue: authSpy },
                { provide: ProcessService, useValue: processSpy },
                { provide: MessageService, useValue: messageSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(LawyerDashboardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should get status class', () => {
        component.statuses = [{ id: 1, name: 'Open' }];
        expect(component.getStatusClass(1)).toBe('status-open');
    });

    it('should calculate stats', () => {
        expect(component.stats.total).toBe(0);
    });
});
