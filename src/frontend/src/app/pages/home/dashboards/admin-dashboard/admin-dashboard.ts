import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../services/auth.service';
import { UserService } from '../../../../services/user.service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css'],
})
export class AdminDashboardComponent implements OnInit {
  auth = inject(AuthService);
  userService = inject(UserService);
  cdr = inject(ChangeDetectorRef);

  userName = this.auth.getUserName();
  users: any[] = [];
  loading = true;

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    console.log('AdminDashboard: Loading users...');
    this.userService.getAllUsers().subscribe({
      next: (res) => {
        console.log('AdminDashboard: API Response:', res);
        this.users = res || [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading users', err);
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  get stats() {
    const active = this.users.filter((u) => u.status === 'ACTIVE').length;
    return {
      total: this.users.length,
      active: active,
      systemHealth: 'Stable', // Mock
    };
  }
}
