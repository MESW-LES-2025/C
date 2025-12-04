import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ClientService } from '../../services/client.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { PaginationComponent } from '../../shared/pagination/pagination';

@Component({
  selector: 'app-processes-client',
  standalone: true,
  templateUrl: './processes-client.html',
  styleUrls: ['./processes-client.css'],
  imports: [PageTitleComponent, FormsModule, CommonModule, PaginationComponent]
})
export class ProcessesClientComponent {

  private router = inject(Router);
  private clientService = inject(ClientService);
  private auth = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  processes: any[] = [];

  currentPage = 1;
  totalPages = 1;
  totalProcesses = 0;

  loading = false;

  ngOnInit() {
    const clientId = this.auth.getUserId();
    this.loadProcesses(clientId!, this.currentPage);
  }

  loadProcesses(clientId: string, page: number) {
    this.loading = true;

    this.clientService.getProcessesByClient(clientId, page).subscribe({
      next: (res) => {
        this.processes = res.data ?? [];

        const total = res.meta?.totalCount ?? 0;
        const limit = res.meta?.limit ?? 20;

        this.totalProcesses = total;
        this.totalPages = Math.max(1, Math.ceil(total / limit));
        this.currentPage = page;

        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) return;

    const clientId = this.auth.getUserId();
    this.loadProcesses(clientId!, page);
  }

  openProcess(id: string) {
    this.router.navigate(['/processes/client', id]);
  }

}
