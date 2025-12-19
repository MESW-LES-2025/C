import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ButtonComponent } from './button';
import { provideRouter } from '@angular/router';

describe('ButtonComponent', () => {
    let component: ButtonComponent;
    let fixture: ComponentFixture<ButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ButtonComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([])
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ButtonComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should set variant class', () => {
        component.variant = 'primary';
        expect(component.variantClass).toBe('btn-primary');

        component.variant = 'secondary';
        expect(component.variantClass).toBe('');
    });

    it('should handle disabled state', () => {
        component.disabled = true;
        expect(component.pointerEvents).toBe('none');

        component.disabled = false;
        expect(component.pointerEvents).toBe('auto');
    });
});
