import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { PaginationComponent } from './pagination';

describe('PaginationComponent', () => {
    let component: PaginationComponent;
    let fixture: ComponentFixture<PaginationComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [PaginationComponent],
            providers: [provideZonelessChangeDetection()]
        }).compileComponents();

        fixture = TestBed.createComponent(PaginationComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should generate pages for small total', () => {
        component.totalPages = 5;
        expect(component.pages).toEqual([1, 2, 3, 4, 5]);
    });

    it('should generate pages with dots', () => {
        component.totalPages = 10;
        component.currentPage = 5;
        const pages = component.pages;
        expect(pages.includes('dots')).toBeTrue();
    });

    it('should emit page change', () => {
        spyOn(component.pageChange, 'emit');
        component.goToPage(3);
        expect(component.pageChange.emit).toHaveBeenCalledWith(3);
    });

    it('should not emit for dots', () => {
        spyOn(component.pageChange, 'emit');
        component.goToPage('dots');
        expect(component.pageChange.emit).not.toHaveBeenCalled();
    });

    it('should go to next page', () => {
        spyOn(component.pageChange, 'emit');
        component.currentPage = 2;
        component.totalPages = 5;
        component.nextPage();
        expect(component.pageChange.emit).toHaveBeenCalledWith(3);
    });

    it('should go to previous page', () => {
        spyOn(component.pageChange, 'emit');
        component.currentPage = 3;
        component.previousPage();
        expect(component.pageChange.emit).toHaveBeenCalledWith(2);
    });

    it('should not go beyond last page', () => {
        spyOn(component.pageChange, 'emit');
        component.currentPage = 5;
        component.totalPages = 5;
        component.nextPage();
        expect(component.pageChange.emit).not.toHaveBeenCalled();
    });

    it('should not go before first page', () => {
        spyOn(component.pageChange, 'emit');
        component.currentPage = 1;
        component.previousPage();
        expect(component.pageChange.emit).not.toHaveBeenCalled();
    });
});
