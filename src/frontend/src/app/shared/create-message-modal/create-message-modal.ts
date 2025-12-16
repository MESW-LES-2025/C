import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface MessagePayload {
  subject: string;
  body: string;
}

@Component({
  selector: 'app-create-message-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-message-modal.html',
  styleUrls: ['./create-message-modal.css'],
})
export class CreateMessageModalComponent {
  // Inputs for auto-filled data
  @Input() senderName: string = '';
  @Input() recipientName: string = '';
  @Input() processName: string = '';
  @Input() initialSubject: string = '';
  @Input() initialBody: string = '';

  // Outputs for actions
  @Output() cancel = new EventEmitter<void>();
  @Output() send = new EventEmitter<MessagePayload>();

  // Form Fields
  subject: string = '';
  body: string = '';

  constructor() {}

  ngOnInit() {
    if (this.initialSubject) {
      this.subject = this.initialSubject;
    }
    if (this.initialBody) {
      this.body = this.initialBody;
    }
  }

  onCancel() {
    this.cancel.emit();
  }

  onSend() {
    if (this.isValid()) {
      this.send.emit({
        subject: this.subject.trim(),
        body: this.body.trim(),
      });
    }
  }

  isValid(): boolean {
    return this.subject.trim().length > 0 && this.body.trim().length > 0 && this.body.length <= 240;
  }

  get bodyLength(): number {
    return this.body.length;
  }

  onBackdropClick(event: MouseEvent) {
    if ((event.target as HTMLElement).classList.contains('msg-backdrop')) {
      this.onCancel();
    }
  }
}
