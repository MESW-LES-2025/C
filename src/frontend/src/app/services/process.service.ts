import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable, of } from 'rxjs';
import { delay, catchError } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class ProcessService {
  private useMock = false;

  constructor(private http: HttpClient) {}

  getProcessById(id: string): Observable<any> {
    if (this.useMock) {
      return of({
        id,
        name: 'Mock Process',
        description: 'Example mock description.',
        client: { id: '1', name: 'Mock Client' },
        lawyer: { id: '2', name: 'Mock Lawyer' },
        documents: [],
      }).pipe(delay(200));
    }

    return this.http.get<any>(`${environment.apiBaseUrl}/processes/${id}`);
  }

  getProcesses(params: any = {}): Observable<any> {
    return this.http.get<any>(`${environment.apiBaseUrl}/processes`, { params });
  }

  getProcessesByClient(clientId: string): Observable<any> {
    return this.http.get<any>(`${environment.apiBaseUrl}/processes/client/${clientId}`);
  }

  getProcessesByLawyer(lawyerId: string): Observable<any> {
    return this.http.get<any>(`${environment.apiBaseUrl}/processes/lawyer/${lawyerId}`);
  }

  getProcessWithDocuments(id: string) {
    return this.http.get<any>(`${environment.apiBaseUrl}/processes/${id}/with-documents`);
  }

  uploadFiles(processId: string, formData: FormData) {
    return this.http.patch(
      `${environment.apiBaseUrl}/processes/${processId}/with-documents`,
      formData
    );
  }

  getProcessStatuses() {
    return this.http.get<any[]>(`${environment.apiBaseUrl}/lookups/process-statuses`);
  }

  getProcessTypePhases() {
    return this.http.get<any[]>(`${environment.apiBaseUrl}/lookups/process-type-phases`);
  }

  deleteDocument(documentId: string) {
    return this.http.delete(`${environment.apiBaseUrl}/documents/${documentId}`);
  }

  getProcessTypes() {
    return this.http.get<any[]>(
      `${environment.apiBaseUrl}/lookups/process-types`
    );
  }

  getProcessPhases() {
    return this.http.get<any[]>(
      `${environment.apiBaseUrl}/lookups/process-phases`
    );
  }

  updateProcess(id: string, payload: any) {
    return this.http.patch(
      `${environment.apiBaseUrl}/processes/${id}`,
      payload
    );
  }

}
