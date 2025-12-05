import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ButtonComponent } from '../../shared/button/button';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LawyerService } from '../../services/lawyer.service';
import { ClientService } from '../../services/client.service';

@Component({
  selector: 'app-processes',
  standalone: true,
  templateUrl: './processes.html',
  styleUrls: ['./processes.css'],
  imports: [PageTitleComponent, ButtonComponent, FormsModule, CommonModule]
})
export class ProcessesComponent {
  private router = inject(Router);
  private lawyerService = inject(LawyerService);
  private clientService = inject(ClientService);
  private auth = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  
  processes: any[] = [];

  currentPage = 1;
  totalPages = 1;
  goToPageNumber = 1;
  totalProcesses = 0;

  role: string | null = null;
  loading = false;

  searchTerm: string = '';
  searchTimeout: any = null;

  ngOnInit() {
    this.role = this.auth.getUserRole();
    const userId = this.auth.getUserId();

    this.loadProcesses(userId!, this.currentPage);
  }

  isLawyer() {
    return this.role === 'Lawyer';
  }

  isClient() {
    return this.role === 'Client';
  }

  loadProcesses(id: string, page: number, search: string = '') {
    if (!id) return;

    this.loading = true;

    if (this.role === 'Lawyer') {
      this.lawyerService.getProcessesByLawyer(id, page, search).subscribe({
        next: (res) => {
          this.processes = [...(res.data ?? [])];

          this.processes.forEach((p) => {
            if (p.clientId) {
              this.clientService.getClient(p.clientId).subscribe(clientRes => {
                p.clientName = clientRes?.name ?? 'Unknown Client';
                this.cdr.detectChanges();
              });
            }
          });

          const totalCount = res.meta?.totalCount ?? 0;
          const limit = res.meta?.limit ?? 20;

          this.totalPages = Math.max(1, Math.ceil(totalCount / limit));
          this.totalProcesses = totalCount;
          this.currentPage = page;

          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.loading = false;
        }
      });

      return;
    }

    if (this.role === 'Client') {
      this.clientService.getProcessesByClient(id, page, search).subscribe({
        next: (res) => {
          this.processes = res.data ?? [];

          this.processes.forEach((p) => {
            if (p.lawyerId) {
                this.lawyerService.getLawyer(p.lawyerId).subscribe(lawyerRes => {
                    p.lawyerName = lawyerRes?.name ?? 'Unknown Lawyer';
                    this.cdr.detectChanges();
                });
            }
          });

          const totalCount = res.meta?.totalCount ?? 0;
          const limit = res.meta?.limit ?? 20;

          this.totalPages = Math.max(1, Math.ceil(totalCount / limit));
          this.totalProcesses = totalCount;
          this.currentPage = page;

          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.loading = false;
        }
      });
      return;
    }

    this.lawyerService.getProcessesByLawyer(id, page, search).subscribe({
      next: (res) => {
        this.processes = [...(res.data ?? [])];
        const totalCount = res.meta?.totalCount ?? 0;
        const limit = res.meta?.limit ?? 20;
        this.totalPages = Math.max(1, Math.ceil(totalCount / limit));
        this.totalProcesses = totalCount;
        this.currentPage = page;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  onSearch() {
    clearTimeout(this.searchTimeout);

    this.searchTimeout = setTimeout(() => {
        const userId = this.auth.getUserId();
        if (userId) {
        this.loadProcesses(userId, 1, this.searchTerm);
        }
    }, 300);
  }


  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      const lawyerId = this.auth.getUserId();
      this.loadProcesses(lawyerId!, this.currentPage);
    }
  }

  getInitials(name?: string): string {
    if (!name || typeof name !== 'string') return '?';

    const parts = name.split(' ').filter(Boolean);
    return parts.map(p => p[0].toUpperCase()).slice(0, 2).join('');
  }

  openProcess(id: string) {
    this.router.navigate(['/processes', id]);
  }

  startProcess() {
    this.router.navigate(['/processes/create']);
  }
}
