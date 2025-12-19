import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { CreateLawyerComponent } from './create-lawyer';
import { LawyerService } from '../../services/lawyer.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { of } from 'rxjs';

describe('CreateLawyerComponent', () => {
    let component: CreateLawyerComponent;
    let fixture: ComponentFixture<CreateLawyerComponent>;
    let lawyerServiceSpy: jasmine.SpyObj<LawyerService>;

    beforeEach(async () => {
        const lawyerSpy = jasmine.createSpyObj('LawyerService', ['createLawyer']);
        const notifSpy = jasmine.createSpyObj('NotificationService', ['showSuccess', 'showError']);

        await TestBed.configureTestingModule({
            imports: [CreateLawyerComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: LawyerService, useValue: lawyerSpy },
                { provide: NotificationService, useValue: notifSpy }
            ]
        }).compileComponents();

        lawyerServiceSpy = TestBed.inject(LawyerService) as jasmine.SpyObj<LawyerService>;
        fixture = TestBed.createComponent(CreateLawyerComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should validate email', () => {
        component.model.email = 'test@example.com';
        expect(component.isEmailValid).toBeTrue();
    });

    it('should validate register number', () => {
        component.model.professionalRegister = 'ABC123';
        expect(component.isRegisterNumberValid).toBeTrue();
    });

    it('should validate password', () => {
        component.model.password = 'password123';
        component.model.confirmPassword = 'password123';
        expect(component.isPasswordValid()).toBeTrue();
    });

    it('should get initials', () => {
        component.model.name = 'Jane Doe';
        expect(component.initials).toBe('JD');
    });

    it('should handle cancel', () => {
        component.onCancelClick();
        expect(component.showCancelModal).toBeTrue();
    });

    it('should create lawyer', () => {
        lawyerServiceSpy.createLawyer.and.returnValue(of({}));
        component.model = {
            name: 'Jane Doe',
            email: 'jane@example.com',
            professionalRegister: 'ABC123',
            nif: '123456789',
            phone: '123456789',
            password: 'password123',
            confirmPassword: 'password123'
        };
        component.createLawyer();
        expect(lawyerServiceSpy.createLawyer).toHaveBeenCalled();
    });
});
