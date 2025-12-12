import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WebsocketService, Message } from '../../services/websocket.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styles: []
})
export class ChatComponent implements OnInit, OnDestroy {
  users: any[] = [];
  messages: Message[] = [];

  // Form fields
  selectedRecipientId: string = '';
  selectedProcessId: string = '';
  newMessageSubject: string = '';
  newMessageBody: string = '';

  processes: any[] = [];
  currentUserId: string = '';

  constructor(
    private websocketService: WebsocketService,
    private http: HttpClient,
    private authService: AuthService
  ) { }

  ngOnInit() {
    this.currentUserId = this.authService.getUserId() || '';

    // Load users
    this.http.get<any[]>(`${environment.apiBaseUrl}/users`).subscribe(users => {
      this.users = users.filter(u => u.id !== this.currentUserId);
    });

    // Load processes
    this.http.get<any[]>(`${environment.apiBaseUrl}/processes`).subscribe(processes => {
      this.processes = processes;
      if (this.processes.length > 0) {
        this.selectedProcessId = this.processes[0].processId;
      }
    });

    // Load messages
    this.http.get<Message[]>(`${environment.apiBaseUrl}/messages`).subscribe(msgs => {
      this.messages = msgs;
    });

    // Subscribe to real-time messages
    this.websocketService.messageReceived$.subscribe(msg => {
      if (msg) {
        this.messages.push(msg);
      }
    });
  }

  ngOnDestroy() {
    // Cleanup if needed
  }

  sendMessage() {
    if (!this.selectedRecipientId || !this.newMessageBody || !this.selectedProcessId) return;

    this.websocketService.sendMessage(
      this.selectedRecipientId,
      this.selectedProcessId,
      this.newMessageSubject || 'No Subject',
      this.newMessageBody
    ).then(() => {
      this.newMessageBody = '';
      this.newMessageSubject = '';
      // Optional: clear recipient? No, keep it for convenience.
    });
  }

  getProcessName(processId: string): string {
    const process = this.processes.find(p => p.processId === processId);
    return process ? `${process.name} (${process.number})` : 'Unknown Process';
  }
}
