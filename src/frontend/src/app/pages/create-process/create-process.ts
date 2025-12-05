import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LawyerService } from '../../services/lawyer.service';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ButtonComponent } from '../../shared/button/button';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal';

@Component({
  selector: 'app-create-process',
  standalone: true,
  templateUrl: './create-process.html',
  styleUrls: ['./create-process.css'],
  imports: [
    CommonModule,
    FormsModule,
    PageTitleComponent,
    ButtonComponent,
    ConfirmModalComponent
  ]
})
export class CreateProcessComponent {
  private auth = inject(AuthService);
  private lawyerService = inject(LawyerService);
  private router = inject(Router);

  showCancelModal = false;
  submitting = false;

  model: any = {
    name: '',
    number: '',
    adversePartName: '',
    opposingCounselName: '',
    description: '',
    courtInfo: '',
    priority: 1,
    nextHearingDate: '',
    clientId: '',
  };

  ngOnInit() {
    // TEMP
    this.model.clientId = '7830bf4b-f7eb-4c6a-8765-0a6baf3c1070';
  }

  get initials() {
    const name = this.model.name || '';
    if (!name) return 'P';
    return name
      .split(' ')
      .map((n: string[]) => n[0].toUpperCase())
      .slice(0, 2)
      .join('');
  }

  isComplete(): boolean {
    return (
      this.model.name.trim().length > 0 &&
      this.model.number.trim().length > 0
    );
  }

  createProcess() {
    if (!this.isComplete() || this.submitting) return;

    this.submitting = true;

    const lawyerId = this.auth.getUserId();

    const payload = {
      name: this.model.name,
      number: this.model.number,
      clientId: this.model.clientId,
      lawyerId: this.auth.getUserId(),
      adversePartName: this.model.adversePartName,
      opposingCounselName: this.model.opposingCounselName,
      priority: Number(this.model.priority) || 1,
      courtInfo: this.model.courtInfo ?? "",
      processTypePhaseId: 1,
      processStatusId: 1,
      nextHearingDate: this.model.nextHearingDate
        ? new Date(this.model.nextHearingDate).toISOString()
        : null,
      description: this.model.description
    };


    this.lawyerService.createProcess(payload).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/processes']);
      },
      error: (err) => {
        console.error('Failed to create process', err);
        this.submitting = false;
      }
    });
  }

  onCancelClick() {
    this.showCancelModal = true;
  }

  onConfirmCancel() {
    this.showCancelModal = false;
    this.router.navigate(['/processes']);
  }

  onCloseModal() {
    this.showCancelModal = false;
  }
}
