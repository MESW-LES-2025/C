import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MessageDetailsModalComponent } from './message-details-modal';

describe('MessageDetailsModalComponent', () => {
    let component: MessageDetailsModalComponent;
    let fixture: ComponentFixture<MessageDetailsModalComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [MessageDetailsModalComponent],
            providers: [provideZonelessChangeDetection()]
        }).compileComponents();

        fixture = TestBed.createComponent(MessageDetailsModalComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should emit close event when onClose is called', () => {
        spyOn(component.close, 'emit');
        component.onClose();
        expect(component.close.emit).toHaveBeenCalled();
    });

    it('should emit reply event when onReply is called', () => {
        spyOn(component.reply, 'emit');
        component.onReply();
        expect(component.reply.emit).toHaveBeenCalled();
    });

    it('should call onClose when backdrop is clicked', () => {
        const mockEvent = {
            target: { classList: { contains: (className: string) => className === 'msg-backdrop' } }
        } as any;

        spyOn(component, 'onClose');
        component.onBackdropClick(mockEvent);
        expect(component.onClose).toHaveBeenCalled();
    });

    it('should not call onClose when non-backdrop element is clicked', () => {
        const mockEvent = {
            target: { classList: { contains: (className: string) => false } }
        } as any;

        spyOn(component, 'onClose');
        component.onBackdropClick(mockEvent);
        expect(component.onClose).not.toHaveBeenCalled();
    });

    describe('getInitials', () => {
        it('should return ? for empty name', () => {
            expect(component.getInitials('')).toBe('?');
        });

        it('should return initials for single name', () => {
            expect(component.getInitials('John')).toBe('J');
        });

        it('should return initials for full name', () => {
            expect(component.getInitials('John Doe')).toBe('JD');
        });

        it('should return only first two initials for three names', () => {
            expect(component.getInitials('John Patrick Doe')).toBe('JP');
        });

        it('should return uppercase initials', () => {
            expect(component.getInitials('john doe')).toBe('JD');
        });
    });

    describe('getFormattedDate', () => {
        it('should return empty string for null date', () => {
            expect(component.getFormattedDate(null)).toBe('');
        });

        it('should return empty string for empty date', () => {
            expect(component.getFormattedDate('')).toBe('');
        });

        it('should format valid date string', () => {
            const date = '2024-01-15T10:30:00Z';
            const formatted = component.getFormattedDate(date);
            expect(formatted).toBeTruthy();
            expect(formatted).toContain('2024');
        });
    });
});
