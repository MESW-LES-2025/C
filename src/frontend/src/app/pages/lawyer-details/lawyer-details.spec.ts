import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { LawyerDetailsComponent } from './lawyer-details';
import { LawyerService } from '../../services/lawyer.service';
import { BreadcrumbService } from '../../shared/breadcrumb/breadcrumb.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { of } from 'rxjs';

describe('LawyerDetailsComponent', () => {
    let component: LawyerDetailsComponent;
    let fixture: ComponentFixture<LawyerDetailsComponent>;

    beforeEach(async () => {
        const lawyerSpy = jasmine.createSpyObj('LawyerService', ['getLawyer', 'deleteLawyer']);
        const breadcrumbSpy = jasmine.createSpyObj('BreadcrumbService', ['setLabelOverride', 'clearLabelOverride']);
        const notifSpy = jasmine.createSpyObj('NotificationService', ['showSuccess', 'showError']);
        const routeSpy = { snapshot: { paramMap: { get: () => '456' } } };

        lawyerSpy.getLawyer.and.returnValue(of({ id: '456', name: 'Jane Doe', status: 'ACTIVE' }));

        await TestBed.configureTestingModule({
            imports: [LawyerDetailsComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: LawyerService, useValue: lawyerSpy },
                { provide: BreadcrumbService, useValue: breadcrumbSpy },
                { provide: NotificationService, useValue: notifSpy },
                { provide: ActivatedRoute, useValue: routeSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(LawyerDetailsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load lawyer', () => {
        expect(component.lawyer).toBeTruthy();
    });

    it('should get initials', () => {
        expect(component.initials).toBe('JD');
    });

    it('should show delete modal', () => {
        component.onDelete();
        expect(component.showDeleteModal).toBeTrue();
    });
});
