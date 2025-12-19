import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ConfirmModalComponent } from './confirm-modal';

describe('ConfirmModalComponent', () => {
    let component: ConfirmModalComponent;
    let fixture: ComponentFixture<ConfirmModalComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ConfirmModalComponent],
            providers: [provideZonelessChangeDetection()]
        }).compileComponents();

        fixture = TestBed.createComponent(ConfirmModalComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have default values', () => {
        expect(component.title).toBe('Confirm');
        expect(component.confirmLabel).toBe('Confirm');
        expect(component.cancelLabel).toBe('Cancel');
        expect(component.danger).toBeFalse();
    });

    it('should emit confirm event', () => {
        spyOn(component.confirm, 'emit');
        component.onConfirm();
        expect(component.confirm.emit).toHaveBeenCalled();
    });

    it('should emit cancel event', () => {
        spyOn(component.cancel, 'emit');
        component.onCancel();
        expect(component.cancel.emit).toHaveBeenCalled();
    });
});
