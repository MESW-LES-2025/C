import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { AuthService } from '../../services/auth.service';
import { ProcessService } from '../../services/process.service';

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
  private auth = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  process: any = null;
  documents: any[] = [];

  role: 'Client' | 'Lawyer' | 'Admin' | null = null;

  title = 'Process Details';

  ngOnInit() {
    this.role = this.auth.getUserRole() as any;
    const processId = this.route.snapshot.paramMap.get('id');

    if (!processId) return;

    this.loadProcess(processId);
  }

  loadProcess(id: string) {
    this.processService.getProcessById(id).subscribe({
      next: (res: any) => {
        this.process = res;

        // Normalize structure for sidebar
        this.process.client = res.client || res.clientInfo || null;
        this.process.lawyer = res.lawyer || res.lawyerInfo || null;

        // Load documents if included
        this.documents = res.documents || [];

        this.cdr.detectChanges();
      }
    });
  }

  // Sidebar initials
  getInitials(name: string | undefined): string {
    if (!name) return '?';
    return name
      .split(' ')
      .filter(x => x.length > 0)
      .map(x => x[0].toUpperCase())
      .join('')
      .slice(0, 2);
  }

  // For attachments
  getDocumentIcon(fileName: string) {
    if (!fileName) return 'assets/file.png';
    const ext = fileName.split('.').pop()?.toLowerCase();

    switch (ext) {
      case 'pdf':
        return 'assets/icons/pdf.png';
      case 'doc':
      case 'docx':
        return 'assets/icons/doc.png';
      case 'png':
      case 'jpg':
      case 'jpeg':
        return 'assets/icons/img.png';
      default:
        return 'assets/icons/file.png';
    }
  }

  removeDocument(i: number) {
    this.documents.splice(i, 1);
    this.cdr.detectChanges();
  }
}
