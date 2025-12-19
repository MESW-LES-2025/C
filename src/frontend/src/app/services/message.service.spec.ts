import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { MessageService } from './message.service';
import { environment } from '../../environments/environment';

describe('MessageService', () => {
    let service: MessageService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                provideZonelessChangeDetection(),
                MessageService
            ]
        });
        service = TestBed.inject(MessageService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should create a message', () => {
        const mockRequest = {
            senderId: '1',
            recipientId: '2',
            processId: 'p1',
            subject: 'Test',
            body: 'Hello'
        };
        const mockResponse = { ...mockRequest, id: 123, createdAt: '2023-01-01', senderName: 'S', recipientName: 'R', processName: 'P' };

        service.createMessage(mockRequest).subscribe(msg => {
            expect(msg).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(`${environment.apiBaseUrl}/messages`);
        expect(req.request.method).toBe('POST');
        req.flush(mockResponse);
    });

    it('should get messages by process', () => {
        const processId = 'p1';
        const mockResponse = { data: [], meta: {} };

        service.getMessagesByProcess(processId).subscribe(res => {
            expect(res).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/messages/process/${processId}`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe('20');
        req.flush(mockResponse);
    });

    it('should mark messages as read', () => {
        const processId = 'p1';
        const recipientId = 'r1';

        service.markMessagesAsRead(processId, recipientId).subscribe(res => {
            expect(res).toBeTruthy();
        });

        const req = httpMock.expectOne(`${environment.apiBaseUrl}/messages/process/${processId}/read`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ recipientId });
        req.flush({});
    });

    it('should get unread count', () => {
        const userId = 'u1';
        const mockResponse = { total: 5, byProcess: [] };

        service.getUnreadCount(userId).subscribe(res => {
            expect(res).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(`${environment.apiBaseUrl}/messages/unread-count/${userId}`);
        expect(req.request.method).toBe('GET');
        req.flush(mockResponse);
    });
});
