import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-message-details-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './message-details-modal.html',
  styleUrls: ['./message-details-modal.css'],
})
export class MessageDetailsModalComponent {
  @Input() senderName: string = '';
  @Input() recipientName: string = '';
  @Input() processName: string = '';
  @Input() subject: string = '';
  @Input() body: string = '';
  @Input() date: string = '';
  @Input() readAt: string | null = null;
  @Input() isMine: boolean = false;
  @Input() canReply: boolean = true;

  @Output() close = new EventEmitter<void>();
  @Output() reply = new EventEmitter<void>();

  onClose() {
    this.close.emit();
  }

  onReply() {
    this.reply.emit();
  }

  onBackdropClick(event: MouseEvent) {
    if ((event.target as HTMLElement).classList.contains('msg-backdrop')) {
      this.onClose();
    }
  }

  getInitials(name: string): string {
    if (!name) return '?';
    return name
      .split(' ')
      .map((n) => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  getFormattedDate(dateStr: string | null): string {
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
}
