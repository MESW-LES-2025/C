import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ButtonComponent } from '../../shared/button/button';
import { ProcessService } from '../../services/process.service';
import { ClientService } from '../../services/client.service';
import { LawyerService } from '../../services/lawyer.service';

@Component({
  selector: 'app-edit-process',
  standalone: true,
  templateUrl: './edit-process.html',
  styleUrls: ['./edit-process.css'],
  imports: [
    CommonModule,
    FormsModule,
    PageTitleComponent,
    ButtonComponent
  ]
})
export class EditProcessComponent {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private processService = inject(ProcessService);
  private clientService = inject(ClientService);
  private lawyerService = inject(LawyerService);
  private cdr = inject(ChangeDetectorRef);

  submitting = false;

  processId!: string;

  /** FULL PROCESS (used by sidebar + header) */
  process: any = null;

  /** EDIT MODEL */
  model: any = {
    name: '',
    description: '',
    processTypeId: '',
    processTypePhaseId: '',
    processStatusId: ''
  };

  /** LOOKUPS */
  processTypes: any[] = [];
  processTypePhases: any[] = [];
  filteredTypePhases: any[] = [];
  processStatuses: any[] = [];

  /** DOCUMENTS */
  documents: any[] = [];

  /** PHASE DESCRIPTION */
  processPhaseDescription = '';

  ngOnInit() {
    this.processId = this.route.snapshot.paramMap.get('id')!;
    this.loadLookups();
    this.loadProcess();
  }

  /* =========================
     LOAD DATA
     ========================= */

  loadProcess() {
    this.processService.getProcessWithDocuments(this.processId).subscribe(res => {

      this.process = {
        id: res.processId,
        name: res.name,
        clientId: res.clientId,
        lawyerId: res.lawyerId,
        client: null,
        lawyer: null
      };

      this.model = {
        name: res.name,
        description: res.description,
        processTypePhaseId: res.processTypePhaseId,
        processStatusId: res.processStatusId
      };

      this.documents = (res.documents || []).map((d: any) => ({
        name: d.fileName,
        size: (d.fileSize / 1024 / 1024).toFixed(2) + ' MB',
        uploaded: new Date(d.createdAt).toLocaleDateString(),
        raw: d
      }));

      this.fetchClient();
      this.resolveTypePhase(res.processTypePhaseId);

      this.cdr.detectChanges();
    });
  }

  loadLookups() {
    this.processService.getProcessTypes().subscribe(r => this.processTypes = r);
    this.processService.getProcessTypePhases().subscribe(r => this.processTypePhases = r);
    this.processService.getProcessStatuses().subscribe(r => this.processStatuses = r);
  }

  /* =========================
     UPDATE
     ========================= */

  updateProcess() {
    if (this.submitting) return;

    this.submitting = true;

    const payload = {
      name: this.model.name,
      description: this.model.description,
      processTypePhaseId: this.model.processTypePhaseId,
      processStatusId: this.model.processStatusId
    };

    this.processService.updateProcess(this.processId, payload).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/processes', this.processId]);
      },
      error: () => this.submitting = false
    });
  }

  isComplete(): boolean {
    return !!(
      this.model.name &&
      this.model.processTypePhaseId &&
      this.model.processStatusId
    );
  }

  /* =========================
     TYPE / PHASE
     ========================= */

  onProcessTypeChange() {
    this.filteredTypePhases = this.processTypePhases
      .filter(tp => tp.processTypeId === Number(this.model.processTypeId))
      .sort((a, b) => a.order - b.order);

    this.model.processTypePhaseId = '';
    this.processPhaseDescription = '';
  }

  resolveTypePhase(typePhaseId: number) {
    const match = this.processTypePhases.find(tp => tp.id === typePhaseId);
    if (!match) return;

    this.model.processTypeId = match.processTypeId;
    this.filteredTypePhases = this.processTypePhases.filter(
      tp => tp.processTypeId === match.processTypeId
    );

    this.processPhaseDescription = match.processPhaseDescription ?? '';
  }

  /* =========================
     DOCUMENTS
     ========================= */

  getDocumentIcon(name: string) {
    const ext = name.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return 'assets/doc-icons/pdf-logo.png';
    if (ext === 'docx') return 'assets/doc-icons/docs-logo.png';
    return 'assets/doc-icons/file-generic.png';
  }

  removeDocument(i: number) {
    const doc = this.documents[i];
    if (!doc?.raw?.documentId) return;

    this.processService.deleteDocument(doc.raw.documentId).subscribe(() => {
      this.documents.splice(i, 1);
      this.cdr.detectChanges();
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const formData = new FormData();
    Array.from(input.files).forEach(f => formData.append('files', f));

    this.processService.uploadFiles(this.processId, formData).subscribe(() => {
      this.loadProcess();
    });
  }

  /* =========================
     CLIENT
     ========================= */

  fetchClient() {
    if (!this.process?.clientId) return;

    this.clientService.getClient(this.process.clientId).subscribe(c => {
      this.process.client = {
        name: c.name,
        location: c.address ?? ''
      };
      this.cdr.detectChanges();
    });
  }

  getInitials(name?: string) {
    if (!name) return '?';
    return name.split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }

  onCancelClick() {
    this.router.navigate(['/processes', this.processId]);
  }
}
