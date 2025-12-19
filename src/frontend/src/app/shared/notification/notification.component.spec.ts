import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { NotificationComponent } from './notification.component';
import { NotificationService } from './notification.service';

describe('NotificationComponent', () => {
    let component: NotificationComponent;
    let fixture: ComponentFixture<NotificationComponent>;
    let service: NotificationService;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [NotificationComponent],
            providers: [provideZonelessChangeDetection(), NotificationService]
        }).compileComponents();

        service = TestBed.inject(NotificationService);
        fixture = TestBed.createComponent(NotificationComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should push notification', () => {
        const notification = { id: '1', type: 'success' as const, message: 'Test' };
        component.push(notification);
        expect(component.notifications.length).toBe(1);
        expect(component.notifications[0].message).toBe('Test');
    });

    it('should start close animation', () => {
        component.notifications = [{ id: '1', type: 'info', message: 'Test', closing: false }];
        component.startClose('1');
        expect(component.notifications[0].closing).toBeTrue();
    });

    it('should remove notification', () => {
        component.notifications = [{ id: '1', type: 'info', message: 'Test' }];
        component.finalRemove('1');
        expect(component.notifications.length).toBe(0);
    });
});
