import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { ClientDashboardComponent } from './dashboards/client-dashboard/client-dashboard';
import { LawyerDashboardComponent } from './dashboards/lawyer-dashboard/lawyer-dashboard';
import { AdminDashboardComponent } from './dashboards/admin-dashboard/admin-dashboard';

@Component({
  selector: 'app-home',
  standalone: true,
  templateUrl: './home.html',
  styleUrls: ['./home.css'],
  imports: [
    CommonModule,
    ClientDashboardComponent,
    LawyerDashboardComponent,
    AdminDashboardComponent,
  ],
})
export class HomeComponent {
  auth = inject(AuthService);

  get role() {
    return this.auth.getUserRole();
  }
}
