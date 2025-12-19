import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { provideZonelessChangeDetection } from '@angular/core';
import { CreateMessageModalComponent } from './create-message-modal';

describe('CreateMessageModalComponent', () => {
    let component: CreateMessageModalComponent;
    let fixture: ComponentFixture<CreateMessageModalComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [CreateMessageModalComponent, FormsModule],
            providers: [provideZonelessChangeDetection()]
        }).compileComponents();

        fixture = TestBed.createComponent(CreateMessageModalComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with empty subject and body', () => {
        component.ngOnInit();
        expect(component.subject).toBe('');
        expect(component.body).toBe('');
    });

    it('should initialize with provided initialSubject', () => {
        component.initialSubject = 'Test Subject';
        component.ngOnInit();
        expect(component.subject).toBe('Test Subject');
    });

    it('should initialize with provided initialBody', () => {
        component.initialBody = 'Test Body';
        component.ngOnInit();
        expect(component.body).toBe('Test Body');
    });

    it('should emit cancel event when onCancel is called', () => {
        spyOn(component.cancel, 'emit');
        component.onCancel();
        expect(component.cancel.emit).toHaveBeenCalled();
    });

    it('should emit send event with payload when form is valid', () => {
        component.subject = 'Test Subject';
        component.body = 'Test Body';
        spyOn(component.send, 'emit');

        component.onSend();

        expect(component.send.emit).toHaveBeenCalledWith({
            subject: 'Test Subject',
            body: 'Test Body'
        });
    });

    it('should not emit send event when form is invalid', () => {
        component.subject = '';
        component.body = '';
        spyOn(component.send, 'emit');

        component.onSend();

        expect(component.send.emit).not.toHaveBeenCalled();
    });

    it('should trim subject and body before sending', () => {
        component.subject = '  Test Subject  ';
        component.body = '  Test Body  ';
        spyOn(component.send, 'emit');

        component.onSend();

        expect(component.send.emit).toHaveBeenCalledWith({
            subject: 'Test Subject',
            body: 'Test Body'
        });
    });

    describe('isValid', () => {
        it('should return false when subject is empty', () => {
            component.subject = '';
            component.body = 'Test Body';
            expect(component.isValid()).toBe(false);
        });

        it('should return false when body is empty', () => {
            component.subject = 'Test Subject';
            component.body = '';
            expect(component.isValid()).toBe(false);
        });

        it('should return false when subject is only whitespace', () => {
            component.subject = '   ';
            component.body = 'Test Body';
            expect(component.isValid()).toBe(false);
        });

        it('should return false when body exceeds 240 characters', () => {
            component.subject = 'Test';
            component.body = 'a'.repeat(241);
            expect(component.isValid()).toBe(false);
        });

        it('should return true when subject and body are valid', () => {
            component.subject = 'Test Subject';
            component.body = 'Test Body';
            expect(component.isValid()).toBe(true);
        });

        it('should return true when body is exactly 240 characters', () => {
            component.subject = 'Test';
            component.body = 'a'.repeat(240);
            expect(component.isValid()).toBe(true);
        });
    });

    describe('bodyLength', () => {
        it('should return 0 for empty body', () => {
            component.body = '';
            expect(component.bodyLength).toBe(0);
        });

        it('should return correct length for body with text', () => {
            component.body = 'Test Body';
            expect(component.bodyLength).toBe(9);
        });
    });

    it('should call onCancel when backdrop is clicked', () => {
        const mockEvent = {
            target: { classList: { contains: (className: string) => className === 'msg-backdrop' } }
        } as any;

        spyOn(component, 'onCancel');
        component.onBackdropClick(mockEvent);
        expect(component.onCancel).toHaveBeenCalled();
    });

    it('should not call onCancel when non-backdrop element is clicked', () => {
        const mockEvent = {
            target: { classList: { contains: (className: string) => false } }
        } as any;

        spyOn(component, 'onCancel');
        component.onBackdropClick(mockEvent);
        expect(component.onCancel).not.toHaveBeenCalled();
    });
});
