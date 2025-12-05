import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay, catchError } from 'rxjs/operators';
import { Lawyer } from '../models/lawyer';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LawyerService {
  // Toggle to use mock data locally. Default false to use real API.
  private useMock = false;

  constructor(private http: HttpClient) {}

  getLawyers(params?: any): Observable<any> {
    if (this.useMock) {
      // lightweight mock (empty)
      return of({ data: [], meta: { totalCount: 0 } }).pipe(delay(250));
    }
    return this.http.get<any>(`${environment.apiBaseUrl}/lawyers`, { params });
  }

  getLawyer(id: string): Observable<any> {
    if (this.useMock) {
      return of(null).pipe(delay(200));
    }
    return this.http.get<any>(`${environment.apiBaseUrl}/lawyers/${id}`);
  }

  deleteLawyer(id: string): Observable<any> {
    if (this.useMock) return of({}).pipe(delay(200));
    return this.http.delete<any>(`${environment.apiBaseUrl}/lawyers/${id}`);
  }

  createLawyer(payload: Partial<Lawyer & { password?: string }>): Observable<any> {
    if (this.useMock) return of({}).pipe(delay(300));
    return this.http.post<any>(`${environment.apiBaseUrl}/lawyers`, payload).pipe(
      catchError(err => { throw err; })
    );
  }

  updateLawyer(id: string, payload: Partial<Lawyer & { password?: string }>): Observable<any> {
    if (this.useMock) return of({}).pipe(delay(200));
    return this.http.patch<any>(`${environment.apiBaseUrl}/lawyers/${id}`, payload).pipe(
      catchError(err => { throw err; })
    );
  }

  getProcessesByLawyer(lawyerId: string, page = 1, search: string = '') {
    return this.http.get<any>(
      `${environment.apiBaseUrl}/processes/lawyer/${lawyerId}`,
      { params: { page, search } }
    );
  }

  createProcess(data: any) {
    return this.http.post(`${environment.apiBaseUrl}/processes`, data);
  }

}
