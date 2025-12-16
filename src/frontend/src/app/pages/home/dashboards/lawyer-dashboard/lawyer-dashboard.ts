import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../services/auth.service';
import { ProcessService } from '../../../../services/process.service';
import { MessageService } from '../../../../services/message.service';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-lawyer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './lawyer-dashboard.html',
  styleUrls: ['./lawyer-dashboard.css'],
})
export class LawyerDashboardComponent implements OnInit {
  auth = inject(AuthService);
  router = inject(Router);
  processService = inject(ProcessService);
  messageService = inject(MessageService);
  cdr = inject(ChangeDetectorRef);

  userName = this.auth.getUserName();
  processes: any[] = [];
  statuses: any[] = [];
  unreadCount = 0;
  unreadProcesses: any[] = [];
  loading = true;

  ngOnInit() {
    this.loadProcesses();
    this.loadStatuses();
    this.loadUnreadCount();
  }

  loadUnreadCount() {
    const userId = this.auth.getUserId();
    if (!userId) return;

    this.messageService.getUnreadCount(userId).subscribe({
      next: (res) => {
        this.unreadCount = res.total || 0;
        this.unreadProcesses = res.byProcess || [];
        this.cdr.detectChanges();
      },
      error: () => {},
    });
  }

  loadStatuses() {
    this.processService.getProcessStatuses().subscribe((res) => {
      this.statuses = res || [];
    });
  }

  getStatusName(id: number): string {
    const s = this.statuses.find((x) => x.id === id);
    return s ? s.name : String(id);
  }

  getStatusClass(id: number): string {
    const name = this.getStatusName(id).toLowerCase();
    if (name.includes('open') || name.includes('inprogress') || name.includes('active'))
      return 'status-open';
    if (name.includes('suspend') || name.includes('hold')) return 'status-suspended';
    if (name.includes('closed') || name.includes('archived') || name.includes('done'))
      return 'status-closed';
    return 'status-default';
  }

  loadProcesses() {
    console.log('LawyerDashboard: Loading processes...');
    const userId = this.auth.getUserId();
    console.log('LawyerDashboard: UserId derived from token:', userId);

    if (!userId) {
      console.error('LawyerDashboard: No user ID found in token.');
      this.loading = false;
      return;
    }

    this.processService.getProcessesByLawyer(userId).subscribe({
      next: (res) => {
        console.log('LawyerDashboard: API Response:', res);
        this.processes = res.data || [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading processes', err);
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  get stats() {
    return {
      active: this.processes.filter((p) => !p.closedAt).length,
      pending: 0,
      total: this.processes.length,
    };
  }

  createProcess() {
    this.router.navigate(['/processes/create']);
  }

  createClient() {
    this.router.navigate(['/create-client']);
  }
}
