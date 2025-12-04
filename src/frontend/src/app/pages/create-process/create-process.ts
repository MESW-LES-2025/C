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
    this.model.clientId = '60ecdebc-3d74-4e5f-b619-044c194a5bb1';
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
      clientId: this.model.clientId,                    // MUST be a GUID, not empty
      lawyerId: this.auth.getUserId(),                  // MUST be a GUID
      adversePartName: this.model.adversePartName,
      opposingCounselName: this.model.opposingCounselName,
      priority: Number(this.model.priority) || 1,       // MUST be a number
      courtInfo: this.model.courtInfo ?? "",            // MUST NOT be null
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
        this.router.navigate(['/processes/lawyer']);
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
    this.router.navigate(['/processes/lawyer']);
  }

  onCloseModal() {
    this.showCancelModal = false;
  }
}
