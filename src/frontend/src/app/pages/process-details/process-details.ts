import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ProcessService } from '../../services/process.service';
import { ClientService } from '../../services/client.service';
import { LawyerService } from '../../services/lawyer.service';
import { AuthService } from '../../services/auth.service';
import { BreadcrumbService } from '../../shared/breadcrumb/breadcrumb.service';
import { MessageService } from '../../services/message.service';
import {
  CreateMessageModalComponent,
  MessagePayload,
} from '../../shared/create-message-modal/create-message-modal';
import { MessageDetailsModalComponent } from '../../shared/message-details-modal/message-details-modal';
import { NotificationService } from '../../shared/notification/notification.service';
import { ButtonComponent } from '../../shared/button/button';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-process-details',
  standalone: true,
  templateUrl: './process-details.html',
  styleUrls: ['./process-details.css'],
  imports: [
    CommonModule,
    PageTitleComponent, ButtonComponent,
    CreateMessageModalComponent,
    MessageDetailsModalComponent,
  ],
})
export class ProcessDetailsComponent {
  private route = inject(ActivatedRoute);
  private processService = inject(ProcessService);
  private clientService = inject(ClientService);
  private lawyerService = inject(LawyerService);
  private messageService = inject(MessageService);
  private notificationService = inject(NotificationService);
  private auth = inject(AuthService);
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

  process: any = null;
  documents: any[] = [];
  messages: any[] = [];
  role: string | null = null;
  currentUserId: string | null = null;

  processStatusName: string = '';
  processTypeName: string = '';
  processPhaseName: string = '';
  processPhases: any[] = [];
  processPhaseDescription: string = '';

  title = 'Process Details';
  private breadcrumbService = inject(BreadcrumbService);

  ngOnInit() {
    this.role = this.auth.getUserRole();
    const processId = this.route.snapshot.paramMap.get('id');
    if (processId) {
      this.loadProcess(processId);
    }
    this.loadProcessStatus();
    this.loadProcessPhases();
  }

  loadProcess(id: string) {
    this.processService.getProcessWithDocuments(id).subscribe({
      next: (res) => {
        this.process = {
          id: res.processId,
          name: res.name,
          description: res.description,
          courtInfo: res.courtInfo,
          nextHearingDate: res.nextHearingDate,
          clientId: res.clientId,
          clientName: res.clientName,
          lawyerId: res.lawyerId,
          lawyerName: res.lawyerName,
          priority: res.priority,
          processTypePhaseId: res.processTypePhaseId,
          processStatusId: res.processStatusId,
        };

        this.loadProcessStatus();
        this.loadProcessTypePhase();

        this.documents = (res.documents || []).map(
          (d: {
            fileName: any;
            fileSize: number;
            createdAt: string | number | Date;
            fileMimeType: any;
            downloadUrl: any;
          }) => ({
            name: d.fileName,
            size: (d.fileSize / 1024 / 1024).toFixed(2) + ' MB',
            uploaded: new Date(d.createdAt).toLocaleDateString(),
            icon: d.fileMimeType,
            downloadUrl: d.downloadUrl,
            raw: d,
          })
        );

        try {
          const url = `/processes/${id}`;
          if (res?.name) this.breadcrumbService.setLabelOverride(url, res.name);
        } catch (e) { }

        if (res.clientId) {
          this.process.client = {
            name: res.clientName || 'Unknown Client',
            location: '',
          };
        }
        if (res.lawyerId) {
          this.process.lawyer = {
            name: res.lawyerName || 'Unknown Lawyer',
            location: '',
          };
        }

        this.cdr.detectChanges();
        this.loadMessages();
      },
    });
  }

  loadProcessStatus() {
    if (!this.process?.processStatusId) return;

    this.processService.getProcessStatuses().subscribe((statuses) => {
      const match = statuses.find((s) => s.id === this.process.processStatusId);
      this.processStatusName = match?.name ?? '—';
      this.cdr.detectChanges();
    });
  }

  loadProcessPhases() {
    this.processService.getProcessPhases().subscribe(phases => {
      this.processPhases = phases.filter(p => p.isActive);
      this.resolvePhaseDescription();
    });
  }

  getStatusClass(status: string): string {
    if (!status) return '';

    const s = status.toLowerCase();

    if (s.includes('open')) return 'status-open';
    if (s.includes('suspend')) return 'status-suspended';
    if (s.includes('closed')) return 'status-closed';

    return 'status-default';
  }

  loadProcessTypePhase() {
    if (!this.process?.processTypePhaseId) return;

    this.processService.getProcessTypePhases().subscribe((types) => {
      const match = types.find((t) => t.id === this.process.processTypePhaseId);

      if (match) {
        this.processTypeName = match.processTypeName;
        this.processPhaseName = match.processPhaseName;

        this.resolvePhaseDescription(match.processPhaseId);
      } else {
        this.processTypeName = '—';
        this.processPhaseName = '—';
        this.processPhaseDescription = '';
      }

      this.cdr.detectChanges();
    });
  }

  resolvePhaseDescription(phaseId?: number) {
    if (!phaseId || !this.processPhases.length) {
      this.processPhaseDescription = '';
      return;
    }

    const phase = this.processPhases.find(p => p.id === phaseId);
    this.processPhaseDescription = phase?.description ?? '';
  }

  getInitials(name?: string | null): string {
    if (!name || typeof name !== 'string') return '?';

    const parts = name.split(' ').filter(Boolean);
    return parts
      .map((p) => p[0].toUpperCase())
      .slice(0, 2)
      .join('');
  }

  getDocumentIcon(name: string) {
    const ext = name.split('.').pop()?.toLowerCase();
    switch (ext) {
      case 'pdf':
        return 'assets/doc-icons/pdf-logo.png';
      case 'docx':
        return 'assets/doc-icons/docs-logo.png';
      default:
        return 'assets/doc-icons/file-generic.png';
    }
  }

  uploadFiles(files: File[]) {
    if (!this.process?.id) return;

    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));

    this.processService.uploadFiles(this.process.id, formData).subscribe({
      next: () => {
        this.loadProcess(this.process.id);
      },
      error: () => { },
    });
  }

  removeDocument(i: number) {
    const doc = this.documents[i];

    // Prevent crash if the document has no backend ID yet
    if (!doc.raw || !doc.raw.documentId) {
      return;
    }

    const documentId = doc.raw.documentId;

    this.processService.deleteDocument(documentId).subscribe({
      next: () => {
        this.documents.splice(i, 1);
        this.cdr.detectChanges();
      },
      error: () => { },
    });
  }

  downloadDocument(doc: any) {
    if (!doc.raw || !doc.raw.downloadUrl) {
      this.notificationService.showError('Document download URL not available');
      return;
    }

    // Create a full URL using the environment API base URL
    // environment.apiBaseUrl is 'http://localhost:8080/api' (or production URL)
    // downloadUrl from backend is '/api/documents/123/download'
    // So we remove '/api' from base and append the full path
    const baseUrl = environment.apiBaseUrl.replace('/api', '');
    const downloadUrl = `${baseUrl}${doc.raw.downloadUrl}`;

    // Use HttpClient to download with auth headers (via auth interceptor)
    this.http.get(downloadUrl, { responseType: 'blob' }).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = doc.name || 'document';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Download failed', err);
        this.notificationService.showError('Failed to download document');
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const files = Array.from(input.files);

    for (const file of files) {
      this.documents.push({
        name: file.name,
        size: (file.size / 1024 / 1024).toFixed(2) + ' MB',
        uploaded: 'Just now',
        _file: file,
      });
    }

    this.uploadFiles(files);
  }

  loadMessages() {
    if (!this.process?.id) return;
    this.currentUserId = this.auth.getUserId();

    this.messageService.getMessagesByProcess(this.process.id).subscribe({
      next: (res) => {
        this.messages = (res.data || []).sort(
          (a: any, b: any) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
        );
        this.cdr.detectChanges();
        setTimeout(() => this.scrollToBottom(), 100);

        // Don't mark messages as read if user is Admin (admins only view, don't participate)
        if (this.currentUserId && this.role !== 'Admin') {
          const userId = this.currentUserId;
          this.messageService.markMessagesAsRead(this.process.id, userId).subscribe({
            next: () => { },
            error: () => { },
          });
        }
      },
      error: (err) => console.error('Failed to load messages', err),
    });
  }

  scrollToBottom() {
    const chatContainer = document.querySelector('.chat-messages');
    if (chatContainer) {
      chatContainer.scrollTop = chatContainer.scrollHeight;
    }
  }

  isMyMessage(senderId: string): boolean {
    return senderId === this.currentUserId;
  }

  isNewDay(index: number): boolean {
    if (index === 0) return true;

    const currentMsg = this.messages[index];
    const prevMsg = this.messages[index - 1];

    const currentDate = new Date(currentMsg.createdAt).setHours(0, 0, 0, 0);
    const prevDate = new Date(prevMsg.createdAt).setHours(0, 0, 0, 0);

    return currentDate !== prevDate;
  }

  // --- Message Creation Logic ---

  showCreateMessageModal = false;
  senderNameForModal = '';
  recipientNameForModal = '';

  canSendMessage(): boolean {
    // Admins cannot send messages - they can only view
    if (this.role === 'Admin') return false;
    // Lawyers can only reply to messages, not create new ones
    if (this.role === 'Lawyer') return false;
    // Clients can create new messages
    if (this.role === 'Client') return true;
    return false;
  }

  openCreateMessageModal(prefillSubject: string = '', prefillBody: string = '') {
    if (!this.process || !this.role) return;

    if (this.role === 'Lawyer') {
      this.senderNameForModal = this.process.lawyerName || 'Me';
      this.recipientNameForModal = this.process.clientName || 'Client';
    } else if (this.role === 'Client') {
      this.senderNameForModal = this.process.clientName || 'Me';
      this.recipientNameForModal = this.process.lawyerName || 'Lawyer';
    } else {
      this.senderNameForModal = 'Admin';
      this.recipientNameForModal = 'Recipient';
    }

    this.showCreateMessageModal = true;
    this.currentPrefillSubject = prefillSubject;
    this.currentPrefillBody = prefillBody;
    this.cdr.detectChanges();
  }

  currentPrefillSubject = '';
  currentPrefillBody = '';

  onReply(msg: any) {
    const subject = msg.subject.startsWith('Re:') ? msg.subject : `Re: ${msg.subject}`;
    const dateStr = new Date(msg.createdAt).toLocaleDateString();

    const replyBody = `

------------ original message ------------
From: ${msg.senderName || 'Unknown'} (${dateStr})
Subject: ${msg.subject}

${msg.body.substring(0, 40)}${msg.body.length > 40 ? '...' : ''}`;

    this.closeMessageDetails();
    this.openCreateMessageModal(subject, replyBody);
  }

  closeCreateMessageModal() {
    this.showCreateMessageModal = false;
    this.cdr.detectChanges();
  }

  // --- Message Details Logic ---
  showDetailsModal = false;
  selectedMessage: any = null;

  openMessageDetails(msg: any) {
    console.log('Opening message details:', msg);
    this.selectedMessage = msg;
    this.showDetailsModal = true;
    this.cdr.detectChanges();
  }

  closeMessageDetails() {
    this.showDetailsModal = false;
    this.selectedMessage = null;
    this.cdr.detectChanges();
  }

  getMessageDate(msg: any): string {
    if (!msg || !msg.createdAt) return '';
    return this.formatDate(msg.createdAt);
  }

  formatDate(dateStr: string | Date): string {
    if (!dateStr) return '';
    try {
      return new Date(dateStr).toLocaleString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch (e) {
      return '';
    }
  }

  handleMessageCreate(payload: MessagePayload) {
    if (!this.process) return;

    const senderId = this.auth.getUserId();
    if (!senderId) return;

    let recipientId = '';

    if (senderId === this.process.lawyerId) {
      recipientId = this.process.clientId;
    } else if (senderId === this.process.clientId) {
      recipientId = this.process.lawyerId;
    } else {
      return;
    }

    const request = {
      senderId: senderId,
      recipientId: recipientId,
      processId: this.process.id,
      subject: payload.subject,
      body: payload.body,
    };

    this.messageService.createMessage(request).subscribe({
      next: (res) => {
        this.closeCreateMessageModal();
        this.notificationService.showSuccess('Message sent successfully!');
        this.loadMessages();
      },
      error: (err) => {
        console.error('Error sending message:', err);
        this.notificationService.showError(
          'Failed to send message: ' + (err.error?.message || 'Unknown error')
        );
      },
    });
  }

  ngOnDestroy(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.breadcrumbService.clearLabelOverride(`/processes/${id}`);
    }
  }
}
