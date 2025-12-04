import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable, of } from 'rxjs';
import { delay, catchError } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class ProcessService {

  private useMock = false; // set to true if you want mock data

  constructor(private http: HttpClient) {}

  /** Get a single process by ID */
  getProcessById(id: string): Observable<any> {
    if (this.useMock) {
      return of({
        id,
        name: 'Mock Process',
        description: 'Example mock description.',
        client: { id: '1', name: 'Mock Client' },
        lawyer: { id: '2', name: 'Mock Lawyer' },
        documents: []
      }).pipe(delay(200));
    }

    return this.http.get<any>(`${environment.apiBaseUrl}/processes/${id}`);
  }



}
