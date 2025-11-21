import { Component } from '@angular/core';
import { ButtonComponent } from '../../shared/button/button';
import { PageTitleComponent } from '../../shared/page-title/page-title';

@Component({
  selector: 'app-cases',
  standalone: true,
  templateUrl: './cases.html',
  styleUrls: ['./cases.css'],
  imports: [PageTitleComponent, ButtonComponent]
})
export class CasesComponent {}
