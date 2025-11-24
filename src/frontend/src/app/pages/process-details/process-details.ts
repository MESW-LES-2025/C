import { Component, inject, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { BreadcrumbService } from '../../shared/breadcrumb/breadcrumb.service';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-process-details',
  templateUrl: './process-details.html',
  styleUrls: ['./process-details.css'],
  imports: [CommonModule, PageTitleComponent]
})
export class ProcessDetailsComponent implements OnDestroy {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private breadcrumbService = inject(BreadcrumbService);
  title = 'Process Details';
  private sub!: Subscription;

  documents = [
    { name: 'Contract_Draft_v3.docx', size: '643MB', uploaded: '2 days ago' },
    { name: 'Lease_Agreement_Final.pdf', size: '3.2MB', uploaded: '1 week ago' },
    { name: 'Evidence_Photos.docx', size: '120MB', uploaded: '3 weeks ago' },
    { name: 'Additional_Notes.docx', size: '1.1MB', uploaded: '1 month ago' }
  ];

  process: any = null;

  constructor() {
    this.sub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) return;

      const url = `/processes/${id}`;

      this.breadcrumbService.clearLabelOverride(url);

      this.breadcrumbService.setLabelOverride(url, 'Process Details');

      this.sub = this.route.paramMap.subscribe(params => {
        const id = params.get('id');
        if (!id) return;

        this.loadProcess(id);
      });
      
    });
  }

  loadProcess(id: string) {
    // later for API service, call the backend

    this.process = {
      clientName: 'Emily Collins',
      clientLocation: 'Porto, Portugal'
    };
  }

  getInitials(name: string): string {
    const parts = name.split(' ').filter(Boolean);
    return parts.map(p => p[0].toUpperCase()).slice(0, 2).join('');
  }

  removeDocument(index: number) {
    this.documents.splice(index, 1);
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
