import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { ClientDetailsComponent } from './client-details';
import { ClientService } from '../../services/client.service';
import { BreadcrumbService } from '../../shared/breadcrumb/breadcrumb.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { of } from 'rxjs';

describe('ClientDetailsComponent', () => {
    let component: ClientDetailsComponent;
    let fixture: ComponentFixture<ClientDetailsComponent>;
    let clientServiceSpy: jasmine.SpyObj<ClientService>;

    beforeEach(async () => {
        const clientSpy = jasmine.createSpyObj('ClientService', ['getClient', 'deleteClient']);
        const breadcrumbSpy = jasmine.createSpyObj('BreadcrumbService', ['setLabelOverride', 'clearLabelOverride']);
        const notifSpy = jasmine.createSpyObj('NotificationService', ['showSuccess', 'showError']);
        const routeSpy = { snapshot: { paramMap: { get: () => '123' } } };

        await TestBed.configureTestingModule({
            imports: [ClientDetailsComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: ClientService, useValue: clientSpy },
                { provide: BreadcrumbService, useValue: breadcrumbSpy },
                { provide: NotificationService, useValue: notifSpy },
                { provide: ActivatedRoute, useValue: routeSpy }
            ]
        }).compileComponents();

        clientServiceSpy = TestBed.inject(ClientService) as jasmine.SpyObj<ClientService>;
        clientServiceSpy.getClient.and.returnValue(of({ id: '123', name: 'John Doe', status: 'ACTIVE', address: 'Street; City; Country — 12345' }));

        fixture = TestBed.createComponent(ClientDetailsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load client', () => {
        expect(clientServiceSpy.getClient).toHaveBeenCalledWith('123');
        expect(component.client).toBeTruthy();
    });

    it('should get initials', () => {
        expect(component.initials).toBe('JD');
    });

    it('should show delete modal', () => {
        component.onDelete();
        expect(component.showDeleteModal).toBeTrue();
    });
});
