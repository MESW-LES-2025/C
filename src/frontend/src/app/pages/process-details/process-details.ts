import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ProcessService } from '../../services/process.service';
import { ClientService } from '../../services/client.service';
import { LawyerService } from '../../services/lawyer.service';
import { AuthService } from '../../services/auth.service';
import { BreadcrumbService } from '../../shared/breadcrumb/breadcrumb.service';

@Component({
  selector: 'app-process-details',
  standalone: true,
  templateUrl: './process-details.html',
  styleUrls: ['./process-details.css'],
  imports: [CommonModule, PageTitleComponent]
})
export class ProcessDetailsComponent {

  private route = inject(ActivatedRoute);
  private processService = inject(ProcessService);
  private clientService = inject(ClientService);
  private lawyerService = inject(LawyerService);
  private auth = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  process: any = null;
  documents: any[] = [];
  role: string | null = null;

  processStatusName: string = '';
  processTypeName: string = '';
  processPhaseName: string = '';


  title = 'Process Details';
  private breadcrumbService = inject(BreadcrumbService);

  ngOnInit() {
    this.role = this.auth.getUserRole();
    const processId = this.route.snapshot.paramMap.get('id');
    if (processId) {
      this.loadProcess(processId);
    }
    this.loadProcessStatus();
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
          lawyerId: res.lawyerId,
          priority: res.priority,
          processTypePhaseId: res.processTypePhaseId,
          processStatusId: res.processStatusId,
        };

        this.loadProcessStatus();
        this.loadProcessTypePhase();

        this.documents = (res.documents || []).map((d: { fileName: any; fileSize: number; createdAt: string | number | Date; fileMimeType: any; downloadUrl: any; }) => ({
          name: d.fileName,
          size: (d.fileSize / 1024 / 1024).toFixed(2) + ' MB',
          uploaded: new Date(d.createdAt).toLocaleDateString(),
          icon: d.fileMimeType,
          downloadUrl: d.downloadUrl,
          raw: d
        }));

        try {
          const url = `/processes/${id}`;
          if (res?.name) this.breadcrumbService.setLabelOverride(url, res.name);
        } catch (e) {}

        this.fetchClientAndLawyer();    
        this.cdr.detectChanges();
      }
    })
  }


  fetchClientAndLawyer() {
    if (this.process.clientId) {
      this.clientService.getClient(this.process.clientId).subscribe((client) => {
        this.process.client = {
          name: client.name,
          location: client.address ?? ""
        };
        this.cdr.detectChanges();
      });
    }

    if (this.process.lawyerId) {
      this.lawyerService.getLawyer(this.process.lawyerId).subscribe((lawyer) => {
        this.process.lawyer = {
          name: lawyer.name,
          location: lawyer.address ?? ""
        };
        this.cdr.detectChanges();
      });
    }
  }

  loadProcessStatus() {
    if (!this.process?.processStatusId) return;

    this.processService.getProcessStatuses().subscribe(statuses => {
      const match = statuses.find(s => s.id === this.process.processStatusId);
      this.processStatusName = match?.name ?? '—';
      this.cdr.detectChanges();
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

    this.processService.getProcessTypePhases().subscribe(types => {
      const match = types.find(t => t.id === this.process.processTypePhaseId);

      if (match) {
        this.processTypeName = match.processTypeName;
        this.processPhaseName = match.processPhaseName;
      } else {
        this.processTypeName = '—';
        this.processPhaseName = '—';
      }

      this.cdr.detectChanges();
    });
  }

  getInitials(name?: string | null): string {
    if (!name || typeof name !== 'string') return '?';
    
    const parts = name.split(' ').filter(Boolean);
    return parts.map(p => p[0].toUpperCase()).slice(0, 2).join('');
  }


  getDocumentIcon(name: string) {
    const ext = name.split('.').pop()?.toLowerCase();
    switch (ext) {
      case 'pdf': return 'assets/doc-icons/pdf-logo.png';
      case 'docx': return 'assets/doc-icons/docs-logo.png';
      default: return 'assets/doc-icons/file-generic.png';
    }
  }

  uploadFiles(files: File[]) {
    if (!this.process?.id) return;

    const formData = new FormData();
    files.forEach(file => formData.append('files', file));

    this.processService.uploadFiles(this.process.id, formData).subscribe({
      next: () => {
        this.loadProcess(this.process.id);
      },
      error: () => {}
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
      error: () => {}
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
        _file: file
      });
    }

    this.uploadFiles(files);
  }

  ngOnDestroy(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.breadcrumbService.clearLabelOverride(`/processes/${id}`);
    }
  }

}
