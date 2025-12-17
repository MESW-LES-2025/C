import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateMessageRequest {
  senderId: string;
  recipientId: string;
  processId: string;
  subject: string;
  body: string;
}

export interface MessageResponse {
  id: number;
  senderId: string;
  senderName: string;
  recipientId: string;
  recipientName: string;
  processId: string;
  processName: string;
  subject: string;
  body: string;
  createdAt: string;
  readAt?: string;
}

@Injectable({
  providedIn: 'root',
})
export class MessageService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/messages`;

  constructor() {}

  createMessage(request: CreateMessageRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(this.apiUrl, request);
  }

  getMessagesByProcess(
    processId: string,
    page: number = 1,
    limit: number = 20
  ): Observable<{ data: MessageResponse[]; meta: any }> {
    let params = new HttpParams().set('page', page).set('limit', limit);

    return this.http.get<{ data: MessageResponse[]; meta: any }>(
      `${this.apiUrl}/process/${processId}`,
      { params }
    );
  }

  markMessagesAsRead(processId: string, recipientId: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/process/${processId}/read`, {
      recipientId,
    });
  }

  getUnreadCount(userId: string): Observable<{ total: number; byProcess: any[] }> {
    return this.http.get<{ total: number; byProcess: any[] }>(
      `${this.apiUrl}/unread-count/${userId}`
    );
  }
}
