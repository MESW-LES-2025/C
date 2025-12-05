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
    const pages: (number | 'dots')[] = [];

    const first = 1;
    const last = this.totalPages;

    if (this.totalPages <= 7) {
        return Array.from({ length: this.totalPages }, (_, i) => i + 1);
    }

    if (this.currentPage <= 3) {
        return [1, 2, 3, 4, 5, 'dots', last];
    }

  if (this.currentPage >= last - 2) {
    return [
      first,
      'dots',
      last - 4,
      last - 3,
      last - 2,
      last - 1,
      last
    ];
  }

  const before2 = this.currentPage - 2;
  const before1 = this.currentPage - 1;
  const after1 = this.currentPage + 1;

  pages.push(first);

  let leftHandled = false;

  if (before2 > 2) {
    pages.push('dots');
    leftHandled = true;
  } else if (before2 === 2) {
    pages.push(2);
    leftHandled = true;
  }

  const middleCandidates = [before2, before1, this.currentPage, after1];

  for (const p of middleCandidates) {
    if (p > first && p < last) {
      if (!pages.includes(p)) {
        pages.push(p);
      }
    }
  }

  if (after1 < last - 1) {
    pages.push('dots');
  } else if (after1 === last - 1) {
    pages.push(last - 1);
  }

  pages.push(last);

  return pages;
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
