import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { EditLawyerComponent } from './edit-lawyer';
import { LawyerService } from '../../services/lawyer.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { of } from 'rxjs';

describe('EditLawyerComponent', () => {
    let component: EditLawyerComponent;
    let fixture: ComponentFixture<EditLawyerComponent>;

    beforeEach(async () => {
        const lawyerSpy = jasmine.createSpyObj('LawyerService', ['getLawyer', 'updateLawyer']);
        const notifSpy = jasmine.createSpyObj('NotificationService', ['showSuccess', 'showError']);
        const routeSpy = { snapshot: { paramMap: { get: () => '1' } } };

        lawyerSpy.getLawyer.and.returnValue(of({
            name: 'Jane Doe',
            email: 'jane@example.com',
            professionalRegister: 'ABC123',
            nif: '123456789',
            phone: '123456789',
            status: 'ACTIVE'
        }));

        await TestBed.configureTestingModule({
            imports: [EditLawyerComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: LawyerService, useValue: lawyerSpy },
                { provide: NotificationService, useValue: notifSpy },
                { provide: ActivatedRoute, useValue: routeSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(EditLawyerComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load lawyer', () => {
        expect(component.model.name).toBe('Jane Doe');
    });

    it('should validate email', () => {
        expect(component.isEmailValid).toBeTrue();
    });

    it('should validate register number', () => {
        expect(component.isRegisterNumberValid).toBeTrue();
    });

    it('should get initials', () => {
        expect(component.initials).toBe('JD');
    });

    it('should detect changes', () => {
        component.model.name = 'John Doe';
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
