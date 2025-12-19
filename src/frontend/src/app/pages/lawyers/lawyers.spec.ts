import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LawyersComponent } from './lawyers';
import { LawyerService } from '../../services/lawyer.service';
import { of, throwError } from 'rxjs';

describe('LawyersComponent', () => {
    let component: LawyersComponent;
    let fixture: ComponentFixture<LawyersComponent>;
    let lawyerServiceSpy: jasmine.SpyObj<LawyerService>;

    beforeEach(async () => {
        const spy = jasmine.createSpyObj('LawyerService', ['getLawyers']);

        await TestBed.configureTestingModule({
            imports: [LawyersComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                { provide: LawyerService, useValue: spy }
            ]
        }).compileComponents();

        lawyerServiceSpy = TestBed.inject(LawyerService) as jasmine.SpyObj<LawyerService>;
        lawyerServiceSpy.getLawyers.and.returnValue(of({
            data: [
                { id: 1, name: 'L1', status: 'Active', createdAt: '2023-01-01' },
                { id: 2, name: 'L2', status: 'Inactive', createdAt: '2023-02-01' }
            ],
            meta: { totalCount: 2 }
        }));

        fixture = TestBed.createComponent(LawyersComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load lawyers on init', () => {
        expect(lawyerServiceSpy.getLawyers).toHaveBeenCalled();
        expect(component.lawyers.length).toBe(2);
    });

    it('should apply filter', () => {
        component.applyFilter('active');
        expect(component.filteredLawyers.length).toBe(1);
    });

    it('should toggle sort', () => {
        component.toggleSort('name');
        expect(component.sortBy).toBe('name');
    });
});
