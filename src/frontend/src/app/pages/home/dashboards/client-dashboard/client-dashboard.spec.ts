import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ClientDashboardComponent } from './client-dashboard';
import { AuthService } from '../../../../services/auth.service';
import { ProcessService } from '../../../../services/process.service';
import { MessageService } from '../../../../services/message.service';
import { of } from 'rxjs';

describe('ClientDashboardComponent', () => {
    let component: ClientDashboardComponent;
    let fixture: ComponentFixture<ClientDashboardComponent>;

    beforeEach(async () => {
        const authSpy = jasmine.createSpyObj('AuthService', ['getUserName', 'getUserId']);
        const processSpy = jasmine.createSpyObj('ProcessService', ['getProcessesByClient', 'getProcessStatuses']);
        const messageSpy = jasmine.createSpyObj('MessageService', ['getUnreadCount']);

        authSpy.getUserId.and.returnValue('123');
        processSpy.getProcessesByClient.and.returnValue(of({ data: [] }));
        processSpy.getProcessStatuses.and.returnValue(of([]));
        messageSpy.getUnreadCount.and.returnValue(of({ total: 5, byProcess: [] }));

        await TestBed.configureTestingModule({
            imports: [ClientDashboardComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: AuthService, useValue: authSpy },
                { provide: ProcessService, useValue: processSpy },
                { provide: MessageService, useValue: messageSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ClientDashboardComponent);
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
});
