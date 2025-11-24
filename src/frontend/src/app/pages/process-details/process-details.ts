import { Component, inject, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { BreadcrumbService } from '../../shared/breadcrumb/breadcrumb.service';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-process-details',
  templateUrl: './process-details.html',
  styleUrls: ['./process-details.css'],
  imports: [CommonModule, PageTitleComponent]
})
export class ProcessDetailsComponent implements OnDestroy {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private breadcrumbService = inject(BreadcrumbService);

  title = 'Process Details';
  private sub!: Subscription;

  constructor() {
    this.sub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) return;

      const url = `/processes/${id}`;

      this.breadcrumbService.clearLabelOverride(url);

      this.breadcrumbService.setLabelOverride(url, 'Process Details');
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }
}
