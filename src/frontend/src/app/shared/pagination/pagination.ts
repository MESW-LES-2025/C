import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pagination.html',
  styleUrls: ['./pagination.css']
})
export class PaginationComponent {
  @Input() currentPage = 1;
  @Input() totalPages = 1;

  @Output() pageChange = new EventEmitter<number>();

  get pages(): (number | 'dots')[] {
    if (this.totalPages <= 5) {
      // If few pages, show all
      return Array.from({ length: this.totalPages }, (_, i) => i + 1);
    }

    // Always show: 1 2 3 4 … last
    return [1, 2, 3, 4, 'dots', this.totalPages];
  }

  goToPage(page: number | 'dots') {
    if (typeof page === 'number') {
      this.pageChange.emit(page);
    }
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.pageChange.emit(this.currentPage + 1);
    }
  }

  previousPage() {
    if (this.currentPage > 1) {
      this.pageChange.emit(this.currentPage - 1);
    }
  }
}
