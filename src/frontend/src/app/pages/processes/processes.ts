import { Component, inject } from '@angular/core';
import { ButtonComponent } from '../../shared/button/button';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { PaginationComponent } from '../../shared/pagination/pagination';
import { Router } from '@angular/router';

@Component({
  selector: 'app-processes',
  standalone: true,
  templateUrl: './processes.html',
  styleUrls: ['./processes.css'],
  imports: [PageTitleComponent, ButtonComponent, FormsModule, CommonModule, PaginationComponent]
})
export class ProcessesComponent {
  private router = inject(Router);
  
  processes = [
    {
      id: 1,
      title: 'Property Dispute',
      description:
        'This case concerns an ongoing dispute between two neighboring property owners regarding the exact boundary line separating their parcels of land in Lisbon. One party claims that a recently installed fence encroaches onto their property.',
      caseLocation: 'Lisbon, Portugal',
      clientName: 'Emily Collins',
      clientLocation: 'Porto, Portugal'
    },
    {
      id: 2,
      title: 'Commercial Lease Issue',
      description:
        'Disagreement between landlord and tenant over early termination of a commercial lease and responsibility for renovations.',
      caseLocation: 'Porto, Portugal',
      clientName: 'John Silva',
      clientLocation: 'Lisbon, Portugal'
    },
    {
      id: 3,
      title: 'Inheritance Dispute',
      description:
        'Siblings contest the distribution of assets after the death of a parent, questioning the validity of the will.',
      caseLocation: 'Coimbra, Portugal',
      clientName: 'Ana Pereira',
      clientLocation: 'Coimbra, Portugal'
    }
  ];

  currentPage = 1;
  totalPages = 8;
  goToPageNumber = 1;

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  getInitials(name: string): string {
    const parts = name.split(' ').filter(Boolean);
    return parts.map(p => p[0].toUpperCase()).slice(0, 2).join('');
  }

  openProcess(id: number) {
    this.router.navigate(['/processes', id]); // 👈 uses your /processes/:id route
  }
}
