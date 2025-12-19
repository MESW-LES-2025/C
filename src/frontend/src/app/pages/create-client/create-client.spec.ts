import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { CreateClientComponent } from './create-client';
import { ClientService } from '../../services/client.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { of, throwError } from 'rxjs';

describe('CreateClientComponent', () => {
    let component: CreateClientComponent;
    let fixture: ComponentFixture<CreateClientComponent>;
    let clientServiceSpy: jasmine.SpyObj<ClientService>;
    let notificationServiceSpy: jasmine.SpyObj<NotificationService>;

    beforeEach(async () => {
        const clientSpy = jasmine.createSpyObj('ClientService', ['createClient']);
        const notifSpy = jasmine.createSpyObj('NotificationService', ['showSuccess', 'showError']);

        await TestBed.configureTestingModule({
            imports: [CreateClientComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: ClientService, useValue: clientSpy },
                { provide: NotificationService, useValue: notifSpy }
            ]
        }).compileComponents();

        clientServiceSpy = TestBed.inject(ClientService) as jasmine.SpyObj<ClientService>;
        notificationServiceSpy = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;

        fixture = TestBed.createComponent(CreateClientComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should validate email', () => {
        component.model.email = 'test@example.com';
        expect(component.isEmailValid).toBeTrue();
        component.model.email = 'invalid';
        expect(component.isEmailValid).toBeFalse();
    });

    it('should validate phone', () => {
        component.model.phone = '123456789';
        expect(component.isPhoneValid).toBeTrue();
        component.model.phone = '123';
        expect(component.isPhoneValid).toBeFalse();
    });

    it('should validate NIF', () => {
        component.model.nif = '123456789';
        expect(component.isNifValid).toBeTrue();
        component.model.nif = '123';
        expect(component.isNifValid).toBeFalse();
    });

    it('should validate password', () => {
        component.model.password = 'password123';
        component.model.confirmPassword = 'password123';
        expect(component.isPasswordValid()).toBeTrue();
    });

    it('should get initials', () => {
        component.model.name = 'John Doe';
        expect(component.initials).toBe('JD');
    });

    it('should validate completeness', () => {
        component.model = {
            name: 'John Doe',
            email: 'john@example.com',
            nif: '123456789',
            phone: '123456789',
            addressStreet: 'Street',
            addressCityState: 'City',
            addressCountry: 'Country',
            addressZip: '12345',
            password: 'password123',
            confirmPassword: 'password123',
            isActive: true
        };
        expect(component.isComplete()).toBeTrue();
    });

    it('should create client', () => {
        clientServiceSpy.createClient.and.returnValue(of({}));
        component.model = {
            name: 'John Doe',
            email: 'john@example.com',
            nif: '123456789',
            phone: '123456789',
            addressStreet: 'Street',
            addressCityState: 'City',
            addressCountry: 'Country',
            addressZip: '12345',
            password: 'password123',
            confirmPassword: 'password123'
        };
        component.createClient();
        expect(clientServiceSpy.createClient).toHaveBeenCalled();
    });

    it('should handle cancel', () => {
        component.onCancelClick();
        expect(component.showCancelModal).toBeTrue();
    });
});
