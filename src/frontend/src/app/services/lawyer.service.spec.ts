import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { LawyerService } from './lawyer.service';

describe('LawyerService', () => {
  let service: LawyerService;
  let httpMock: HttpTestingController;
  const apiUrl = 'http://localhost:8080/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        LawyerService
      ]
    });

    service = TestBed.inject(LawyerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getLawyers', () => {
    it('should fetch lawyers list', () => {
      const mockResponse = { data: [{ id: '1', name: 'John Doe' }], meta: { totalCount: 1 } };

      service.getLawyers().subscribe(response => {
        expect(response.data.length).toBe(1);
        expect(response.data[0].name).toBe('John Doe');
      });

      const req = httpMock.expectOne(`${apiUrl}/lawyers`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should pass params when provided', () => {
      service.getLawyers({ page: 2, search: 'test' }).subscribe();

      const req = httpMock.expectOne(req => req.url.includes('/lawyers'));
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('search')).toBe('test');
      req.flush({ data: [], meta: {} });
    });
  });

  describe('getLawyer', () => {
    it('should fetch single lawyer by id', () => {
      const mockLawyer = { id: '123', name: 'John Doe' };

      service.getLawyer('123').subscribe(lawyer => {
        expect(lawyer.id).toBe('123');
        expect(lawyer.name).toBe('John Doe');
      });

      const req = httpMock.expectOne(`${apiUrl}/lawyers/123`);
      expect(req.request.method).toBe('GET');
      req.flush(mockLawyer);
    });
  });

  describe('deleteLawyer', () => {
    it('should delete lawyer by id', () => {
      service.deleteLawyer('123').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/lawyers/123`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });
  });

  describe('createLawyer', () => {
    it('should create new lawyer', () => {
      const newLawyer = { name: 'Jane Doe', email: 'jane@example.com', password: 'password123' };

      service.createLawyer(newLawyer).subscribe(response => {
        expect(response).toBeTruthy();
      });

      const req = httpMock.expectOne(`${apiUrl}/lawyers`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newLawyer);
      req.flush({ id: '456', ...newLawyer });
    });
  });

  describe('updateLawyer', () => {
    it('should update existing lawyer', () => {
      const updatePayload = { name: 'John Updated' };

      service.updateLawyer('123', updatePayload).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/lawyers/123`);
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual(updatePayload);
      req.flush({ id: '123', ...updatePayload });
    });
  });

  describe('getProcessesByLawyer', () => {
    it('should fetch processes for a lawyer', () => {
      const mockProcesses = { data: [{ id: '1', name: 'Process 1' }] };

      service.getProcessesByLawyer('123').subscribe(response => {
        expect(response.data.length).toBe(1);
      });

      const req = httpMock.expectOne(req => req.url.includes('/processes/lawyer/123'));
      expect(req.request.method).toBe('GET');
      req.flush(mockProcesses);
    });

    it('should pass pagination and search params', () => {
      service.getProcessesByLawyer('123', 2, 'search term').subscribe();

      const req = httpMock.expectOne(req => req.url.includes('/processes/lawyer/123'));
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('search')).toBe('search term');
      req.flush({ data: [] });
    });
  });

  describe('createProcess', () => {
    it('should create new process', () => {
      const processData = { name: 'New Process', clientId: '456' };

      service.createProcess(processData).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/processes`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(processData);
      req.flush({ id: '789', ...processData });
    });
  });
});
