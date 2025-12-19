import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { EditClientComponent } from './edit-client';
import { ClientService } from '../../services/client.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { of } from 'rxjs';

describe('EditClientComponent', () => {
    let component: EditClientComponent;
    let fixture: ComponentFixture<EditClientComponent>;

    beforeEach(async () => {
        const clientSpy = jasmine.createSpyObj('ClientService', ['getClient', 'updateClient']);
        const notifSpy = jasmine.createSpyObj('NotificationService', ['showSuccess', 'showError']);
        const routeSpy = { snapshot: { paramMap: { get: () => '1' } } };

        clientSpy.getClient.and.returnValue(of({
            name: 'John Doe',
            email: 'john@example.com',
            nif: '123456789',
            phone: '123456789',
            address: 'Street; City; Country — 12345',
            status: 'ACTIVE'
        }));

        await TestBed.configureTestingModule({
            imports: [EditClientComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: ClientService, useValue: clientSpy },
                { provide: NotificationService, useValue: notifSpy },
                { provide: ActivatedRoute, useValue: routeSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(EditClientComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load client', () => {
        expect(component.model.name).toBe('John Doe');
    });

    it('should validate email', () => {
        expect(component.isEmailValid).toBeTrue();
    });

    it('should get initials', () => {
        expect(component.initials).toBe('JD');
    });

    it('should detect changes', () => {
        component.model.name = 'Jane Doe';
        expect(component.hasChanges()).toBeTrue();
    });

    it('should handle active toggle', () => {
        component.onActiveToggle(false);
        expect(component.model.isActive).toBeFalse();
    });

    it('should handle cancel', () => {
        component.onCancelClick();
        expect(component.showCancelModal).toBeTrue();
    });
});
