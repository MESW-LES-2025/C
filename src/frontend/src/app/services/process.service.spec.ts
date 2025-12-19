import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ProcessService } from './process.service';
import { environment } from '../../environments/environment';

describe('ProcessService', () => {
    let service: ProcessService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                provideZonelessChangeDetection(),
                ProcessService
            ]
        });
        service = TestBed.inject(ProcessService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should get process by id', () => {
        const id = '123';
        const mockProcess = { id, name: 'Test Process' };

        service.getProcessById(id).subscribe(process => {
            expect(process).toEqual(mockProcess);
        });

        const req = httpMock.expectOne(`${environment.apiBaseUrl}/processes/${id}`);
        expect(req.request.method).toBe('GET');
        req.flush(mockProcess);
    });

    it('should get processes', () => {
        const mockProcesses = [{ id: '1' }];

        service.getProcesses().subscribe(res => {
            expect(res).toEqual(mockProcesses);
        });

        const req = httpMock.expectOne(req => req.url.startsWith(`${environment.apiBaseUrl}/processes`));
        expect(req.request.method).toBe('GET');
        req.flush(mockProcesses);
    });

    it('should get processes by client', () => {
        const clientId = 'c1';

        service.getProcessesByClient(clientId).subscribe();

        const req = httpMock.expectOne(`${environment.apiBaseUrl}/processes/client/${clientId}`);
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });

    it('should get processes by lawyer', () => {
        const lawyerId = 'l1';

        service.getProcessesByLawyer(lawyerId).subscribe();

        const req = httpMock.expectOne(`${environment.apiBaseUrl}/processes/lawyer/${lawyerId}`);
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });

    it('should get process with documents', () => {
        const id = 'p1';
        service.getProcessWithDocuments(id).subscribe();
        const req = httpMock.expectOne(`${environment.apiBaseUrl}/processes/${id}/with-documents`);
        expect(req.request.method).toBe('GET');
        req.flush({});
    });

    it('should upload files', () => {
        const id = 'p1';
        const formData = new FormData();
        service.uploadFiles(id, formData).subscribe();
        const req = httpMock.expectOne(`${environment.apiBaseUrl}/processes/${id}/with-documents`);
        expect(req.request.method).toBe('PATCH');
        req.flush({});
    });

    it('should get statuses and phases', () => {
        service.getProcessStatuses().subscribe();
        httpMock.expectOne(`${environment.apiBaseUrl}/lookups/process-statuses`).flush([]);

        service.getProcessTypePhases().subscribe();
        httpMock.expectOne(`${environment.apiBaseUrl}/lookups/process-type-phases`).flush([]);
    });

    it('should delete document', () => {
        const docId = 'd1';
        service.deleteDocument(docId).subscribe();
        const req = httpMock.expectOne(`${environment.apiBaseUrl}/documents/${docId}`);
        expect(req.request.method).toBe('DELETE');
        req.flush({});
    });
});
