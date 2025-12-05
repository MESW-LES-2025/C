import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay, catchError } from 'rxjs/operators';
import { Client } from '../models/client';
import { MOCK_CLIENTS } from '../pages/clients/mock-clients';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ClientService {
  // Toggle aqui para usar mocks ou a API real.
  // Quando a API estiver pronta, mudar para false e assegurar que o HttpClientModule está importado.
  private useMock = false;

  constructor(private http: HttpClient) {}

  getClients(params?: any): Observable<any> {
    if (this.useMock) {
      return of({ data: MOCK_CLIENTS, meta: { totalCount: MOCK_CLIENTS.length } }).pipe(delay(250));
    }
    return this.http.get<any>(`${environment.apiBaseUrl}/clients`, { params });
  }

  getClient(id: string): Observable<any> {
    if (this.useMock) {
      const found = MOCK_CLIENTS.find(c => c.id === id);
      return of(found).pipe(delay(200));
    }
    return this.http.get<any>(`${environment.apiBaseUrl}/clients/${id}`);
  }

  getAllClients():Observable<any>{
    return this.http.get<any[]>(`${environment.apiBaseUrl}/clients`);
  }

  deleteClient(id: string): Observable<any> {
    if (this.useMock) {
      return of({}).pipe(delay(200));
    }
    return this.http.delete<any>(`${environment.apiBaseUrl}/clients/${id}`);
  }

  createClient(payload: Partial<Client>): Observable<any> {
    if (this.useMock) {
      const created: any = { ...payload, id: Math.random().toString(36).slice(2), createdAt: new Date().toISOString() };
      try {
        (MOCK_CLIENTS as any[]).unshift(created);
      } catch {}
      return of(created).pipe(delay(300));
    }
    return this.http.post<any>(`${environment.apiBaseUrl}/clients`, payload).pipe(
      catchError(err => { throw err; })
    );
  }

  updateClient(id: string, payload: Partial<Client>): Observable<any> {
    if (this.useMock) {
      const idx = (MOCK_CLIENTS as any[]).findIndex(c => c.id === id);
      if (idx >= 0) {
        (MOCK_CLIENTS as any[])[idx] = { ...MOCK_CLIENTS[idx], ...payload } as any;
        return of((MOCK_CLIENTS as any[])[idx]).pipe(delay(200));
      }
      return of(null).pipe(delay(200));
    }
    return this.http.patch<any>(`${environment.apiBaseUrl}/clients/${id}`, payload).pipe(
      catchError(err => { throw err; })
    );
  }

  getProcessesByClient(clientId: string, page = 1, search: string = '') {
    return this.http.get<any>(
      `${environment.apiBaseUrl}/processes/client/${clientId}`,
      { params: { page, search } }
    );
  }

}
