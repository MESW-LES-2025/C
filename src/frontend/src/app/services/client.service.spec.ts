import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { ClientService } from './client.service';

describe('ClientService', () => {
  let service: ClientService;
  let httpMock: HttpTestingController;
  const apiUrl = 'http://localhost:8080/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        ClientService
      ]
    });

    service = TestBed.inject(ClientService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getClients', () => {
    it('should fetch clients list', () => {
      const mockResponse = { data: [{ id: '1', name: 'Client One' }], meta: { totalCount: 1 } };

      service.getClients().subscribe(response => {
        expect(response.data.length).toBe(1);
        expect(response.data[0].name).toBe('Client One');
      });

      const req = httpMock.expectOne(`${apiUrl}/clients`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should pass params when provided', () => {
      service.getClients({ page: 3, search: 'test' }).subscribe();

      const req = httpMock.expectOne(req => req.url.includes('/clients'));
      expect(req.request.params.get('page')).toBe('3');
      expect(req.request.params.get('search')).toBe('test');
      req.flush({ data: [], meta: {} });
    });
  });

  describe('getClient', () => {
    it('should fetch single client by id', () => {
      const mockClient = { id: '123', name: 'Client One' };

      service.getClient('123').subscribe(client => {
        expect(client.id).toBe('123');
        expect(client.name).toBe('Client One');
      });

      const req = httpMock.expectOne(`${apiUrl}/clients/123`);
      expect(req.request.method).toBe('GET');
      req.flush(mockClient);
    });
  });

  describe('getAllClients', () => {
    it('should fetch all clients', () => {
      const mockClients = [{ id: '1' }, { id: '2' }];

      service.getAllClients().subscribe(clients => {
        expect(clients.length).toBe(2);
      });

      const req = httpMock.expectOne(`${apiUrl}/clients`);
      expect(req.request.method).toBe('GET');
      req.flush(mockClients);
    });
  });

  describe('deleteClient', () => {
    it('should delete client by id', () => {
      service.deleteClient('123').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/clients/123`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });
  });

  describe('createClient', () => {
    it('should create new client', () => {
      const newClient = { name: 'New Client', email: 'client@example.com' };

      service.createClient(newClient).subscribe(response => {
        expect(response).toBeTruthy();
      });

      const req = httpMock.expectOne(`${apiUrl}/clients`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newClient);
      req.flush({ id: '456', ...newClient });
    });
  });

  describe('updateClient', () => {
    it('should update existing client', () => {
      const updatePayload = { name: 'Client Updated' };

      service.updateClient('123', updatePayload).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/clients/123`);
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual(updatePayload);
      req.flush({ id: '123', ...updatePayload });
    });
  });

  describe('getProcessesByClient', () => {
    it('should fetch processes for a client', () => {
      const mockProcesses = { data: [{ id: '1', name: 'Process 1' }] };

      service.getProcessesByClient('123').subscribe(response => {
        expect(response.data.length).toBe(1);
      });

      const req = httpMock.expectOne(req => req.url.includes('/processes/client/123'));
      expect(req.request.method).toBe('GET');
      req.flush(mockProcesses);
    });

    it('should pass pagination and search params', () => {
      service.getProcessesByClient('123', 2, 'search term').subscribe();

      const req = httpMock.expectOne(req => req.url.includes('/processes/client/123'));
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('search')).toBe('search term');
      req.flush({ data: [] });
    });
  });
});
