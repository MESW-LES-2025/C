import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LawyerService } from '../../services/lawyer.service';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ButtonComponent } from '../../shared/button/button';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal';
import { ClientService } from '../../services/client.service';
import { ProcessService } from '../../services/process.service';

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
  private clientService = inject(ClientService);
  private processService = inject(ProcessService);

  showCancelModal = false;
  submitting = false;

  clients: any[] = [];
  processTypes: any[] = [];
  processTypePhases: any[] = [];
  filteredTypePhases: any[] = [];
  processStatuses: any[] = [];

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
    processTypeId: '',
    processTypePhaseId: '',
    processStatusId: ''
  };

  ngOnInit() {
    this.loadClients();
    this.loadLookups();
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
      this.model.number.trim().length > 0 &&
      this.model.clientId &&
      this.model.processTypeId &&
      this.model.processTypePhaseId
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
      processTypePhaseId: this.model.processTypePhaseId,
      processStatusId: this.model.processStatusId,
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

  loadClients() {
    this.clientService.getAllClients().subscribe({
      next: (res) => {
        this.clients = res.data ?? [];
      },
      error: (err: any) => console.error("Failed to load clients", err)
    });
  }

  loadLookups() {
    this.processService.getProcessTypes().subscribe(types => {
      this.processTypes = types.filter(t => t.isActive);
    });

    this.processService.getProcessTypePhases().subscribe(tps => {
      this.processTypePhases = tps.filter(tp => tp.isActive);
    });

    this.processService.getProcessStatuses().subscribe(statuses => {
      this.processStatuses = statuses.filter(s => s.isActive);
      const def = this.processStatuses.find(s => s.isDefault);
      if (def) this.model.processStatusId = def.id;
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

  onProcessTypeChange() {
    this.filteredTypePhases = this.processTypePhases
      .filter(tp => tp.processTypeId === Number(this.model.processTypeId))
      .sort((a, b) => a.order - b.order);

    this.model.processTypePhaseId = '';
  }

}
