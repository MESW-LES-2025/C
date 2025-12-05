import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProcessDetails } from './process-details';

describe('ProcessDetails', () => {
  let component: ProcessDetails;
  let fixture: ComponentFixture<ProcessDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProcessDetails]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProcessDetails);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
