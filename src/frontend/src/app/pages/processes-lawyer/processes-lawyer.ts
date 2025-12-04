import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ButtonComponent } from '../../shared/button/button';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LawyerService } from '../../services/lawyer.service';
import { PaginationComponent } from '../../shared/pagination/pagination';

@Component({
  selector: 'app-processes',
  standalone: true,
  templateUrl: './processes-lawyer.html',
  styleUrls: ['./processes-lawyer.css'],
  imports: [PageTitleComponent, ButtonComponent, FormsModule, CommonModule, PaginationComponent]
})
export class ProcessesLawyerComponent {
  private router = inject(Router);
  private lawyerService = inject(LawyerService);
  private auth = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  
  processes: any[] = [];

  currentPage = 1;
  totalPages = 1;
  goToPageNumber = 1;
  totalProcesses = 0;

  ngOnInit() {
    const lawyerId = this.auth.getUserId();
    this.loadProcesses(lawyerId!, this.currentPage);
  }

  loadProcesses(id: string, page: number) {
    this.lawyerService.getProcessesByLawyer(id, page).subscribe({
      next: (res) => {
        this.processes = [...(res.data ?? [])];

        const totalCount = res.meta?.totalCount ?? 0;
        const limit = res.meta?.limit ?? 20;

        this.totalPages = Math.max(1, Math.ceil(totalCount / limit));
        this.totalProcesses = totalCount;

        this.currentPage = page;

        this.cdr.detectChanges();

        console.log("Processes loaded:", this.processes);
      }
    });
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      const lawyerId = this.auth.getUserId();
      this.loadProcesses(lawyerId!, this.currentPage);
    }
  }

  getInitials(name: string): string {
    const parts = name.split(' ').filter(Boolean);
    return parts.map(p => p[0].toUpperCase()).slice(0, 2).join('');
  }

  openProcess(id: string) {
    this.router.navigate(['/processes/lawyer', id]);
  }

  startProcess() {
    this.router.navigate(['/processes/lawyer/create']);
  }
}
