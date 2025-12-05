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

  title = 'Process Details';
  private breadcrumbService = inject(BreadcrumbService);

  ngOnInit() {
    this.role = this.auth.getUserRole();
    const processId = this.route.snapshot.paramMap.get('id');
    if (processId) {
      this.loadProcess(processId);
    }
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
        };

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
        console.log(this.documents);
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

    files.forEach(file => {
      formData.append('files', file);
    });

    this.processService.uploadFiles(this.process.id, formData).subscribe({
      next: (res) => {
        console.log("Uploaded successfully:", res);
      },
      error: (err) => {
        console.error("Upload failed", err);
      }
    });
  }

  removeDocument(i: number) {
    const doc = this.documents[i];

    // Remove from UI
    this.documents.splice(i, 1);

    // Prepare backend request
    const formData = new FormData();
    formData.append("deleteDocuments", doc.raw.documentId);

    this.processService.updateProcessFiles(this.process.id, formData)
      .subscribe({
        next: () => console.log("Document deleted on backend"),
        error: err => console.error("Failed to delete document", err)
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
