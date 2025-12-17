import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css'],
})
export class SidebarComponent {
  userName: string = 'User';
  userEmail: string = 'user@email.com';
  userInitials: string = 'U';

  constructor(private authService: AuthService, private router: Router) {
    this.userName = this.authService.getUserName();
    this.userEmail = this.authService.getUserEmail();
    this.userInitials = this.getInitials(this.userName);
  }

  hasRole(roles: string[]): boolean {
    return this.authService.hasRole(roles);
  }

  logout() {
    this.authService.removeToken();
    this.router.navigate(['/login']);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }
}
