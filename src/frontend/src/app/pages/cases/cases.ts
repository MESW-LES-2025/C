import { Component } from '@angular/core';
import { ButtonComponent } from '../../shared/button/button';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { FormsModule } from '@angular/forms'; 

@Component({
  selector: 'app-cases',
  standalone: true,
  templateUrl: './cases.html',
  styleUrls: ['./cases.css'],
  imports: [PageTitleComponent, ButtonComponent, FormsModule]
})
export class CasesComponent {
  // Mock data for processes
  processes = [
    { id: 1, name: 'Process #1', details: 'Details of the process will go here...' },
    { id: 2, name: 'Process #2', details: 'Details of the process will go here...' },
    { id: 3, name: 'Process #3', details: 'Details of the process will go here...' },
    { id: 4, name: 'Process #4', details: 'Details of the process will go here...' }
  ];

  currentPage = 1;
  totalPages = 1;
  goToPageNumber = 1;
  client = { name: 'Emily Collins' };

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  getInitials(name: string): string {
    const nameParts = name.split(' ');
    const initials = nameParts.map(part => part.charAt(0).toUpperCase()).slice(0, 2).join('');
    return initials;
  }
}
