import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ClientsComponent } from './clients';
import { ClientService } from '../../services/client.service';

import { of, throwError } from 'rxjs';

describe('ClientsComponent', () => {
    let component: ClientsComponent;
    let fixture: ComponentFixture<ClientsComponent>;
    let clientServiceSpy: jasmine.SpyObj<ClientService>;

    beforeEach(async () => {
        const spy = jasmine.createSpyObj('ClientService', ['getClients']);

        await TestBed.configureTestingModule({
            imports: [ClientsComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: ClientService, useValue: spy }
            ]
        })
            .compileComponents();

        clientServiceSpy = TestBed.inject(ClientService) as jasmine.SpyObj<ClientService>;

        // Default mock behavior
        clientServiceSpy.getClients.and.returnValue(of({
            data: [
                { id: 1, name: 'C1', status: 'Active', createdAt: '2023-01-01' },
                { id: 2, name: 'C2', status: 'Inactive', createdAt: '2023-02-01' }
            ],
            meta: { totalCount: 2 }
        }));

        fixture = TestBed.createComponent(ClientsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load clients on init', () => {
        expect(clientServiceSpy.getClients).toHaveBeenCalled();
        expect(component.clients.length).toBe(2);
        expect(component.totalCount).toBe(2);
        expect(component.loading).toBeFalse();
    });

    it('should filter clients by status', () => {
        component.applyFilter('active');
        expect(component.filteredClients.length).toBe(1);
        expect(component.filteredClients[0].name).toBe('C1');

        component.applyFilter('inactive');
        expect(component.filteredClients.length).toBe(1);
        expect(component.filteredClients[0].name).toBe('C2');

        component.applyFilter('all');
        expect(component.filteredClients.length).toBe(2);
    });

    it('should handle search', (done) => {
        component.onSearch('test');
        setTimeout(() => {
            expect(clientServiceSpy.getClients).toHaveBeenCalledWith(jasmine.objectContaining({ search: 'test' }));
            done();
        }, 600);
    });

    it('should handle pagination', () => {
        component.totalPages = 5;
        component.goToPage(2);
        expect(clientServiceSpy.getClients).toHaveBeenCalledWith(jasmine.objectContaining({ page: 2 }));
        expect(component.currentPage).toBe(2);
    });

    it('should sort clients', () => {
        component.toggleSort('name');
        expect(component.sortBy).toBe('name');
        expect(component.sortDir).toBe('asc');

        // Verify sorting logic locally
        expect(component.filteredClients[0].name).toBe('C1');
        expect(component.filteredClients[1].name).toBe('C2');

        component.toggleSort('name'); // Descending
        expect(component.sortDir).toBe('desc');
        expect(component.filteredClients[0].name).toBe('C2');
        expect(component.filteredClients[1].name).toBe('C1');
    });

    it('should handle error when loading clients', () => {
        clientServiceSpy.getClients.and.returnValue(throwError(() => new Error('Error')));
        component.loadClients();
        expect(component.loading).toBeFalse();
        expect(component.errorMessage).toBe('Failed to load clients.');
    });
});
